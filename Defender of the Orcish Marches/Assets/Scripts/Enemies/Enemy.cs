using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data;

    public EnemyData Data => data;
    public int CurrentHP { get; private set; }
    public int ScaledDamage { get; private set; }
    public bool IsDead { get; private set; }

    public static event Action<Enemy> OnEnemyDied;
    public static event Action<Enemy> OnEnemySpawned;

    // Static registry of living enemies — avoids FindObjectsByType every frame
    private static readonly HashSet<Enemy> activeEnemies = new HashSet<Enemy>();
    public static IReadOnlyCollection<Enemy> ActiveEnemies => activeEnemies;

    private Renderer[] bodyRenderers;
    private Color[] originalColors;
    private float damageFlashTimer;
    private Animator animator;
    private UnityEngine.AI.NavMeshAgent agent;
    private float actionAnimTimer;
    private const float IDLE_WALK_FRAME = 0.25f; // frame 6 of 24

    private void OnEnable()
    {
        activeEnemies.Add(this);
    }

    private void OnDisable()
    {
        activeEnemies.Remove(this);
    }

    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        float dailyHP = DailyEventManager.Instance != null ? DailyEventManager.Instance.EnemyHPMultiplier : 1f;
        float dailyDmg = DailyEventManager.Instance != null ? DailyEventManager.Instance.EnemyDamageMultiplier : 1f;
        CurrentHP = Mathf.RoundToInt(data.maxHP * GameSettings.GetEnemyHPMultiplier() * dailyHP);
        ScaledDamage = Mathf.RoundToInt(data.damage * dailyDmg);

        // Swap model if the EnemyData specifies a custom model
        if (data.modelPrefab != null)
        {
            var existingModel = transform.Find("Model");
            if (existingModel != null)
            {
                Debug.Log($"[Enemy] Destroying default Model for {data.enemyName}, replacing with custom model");
                Destroy(existingModel.gameObject);
            }

            var newModel = Instantiate(data.modelPrefab, transform);
            newModel.name = "Model";
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localScale = Vector3.one;

            animator = newModel.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = newModel.AddComponent<Animator>();
            if (data.animatorController != null)
                animator.runtimeAnimatorController = data.animatorController;
            animator.applyRootMotion = false;

            Debug.Log($"[Enemy] Custom model loaded for {data.enemyName}");
        }
        else
        {
            // Cache animator from existing model
            animator = GetComponentInChildren<Animator>();
        }

        // Cache all renderers and their original colors
        bodyRenderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[bodyRenderers.Length];
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (animator != null)
            {
                // Model has its own materials — store them as-is
                originalColors[i] = bodyRenderers[i].material.color;
            }
            else
            {
                // Cube placeholder — apply bodyColor from data
                bodyRenderers[i].material.color = data.bodyColor;
                originalColors[i] = data.bodyColor;
            }
        }

        transform.localScale = data.bodyScale;
        FitColliderToModel();
        Debug.Log($"[Enemy] Initialized: {data.enemyName}, HP={data.maxHP}, type={data.enemyType}");
        OnEnemySpawned?.Invoke(this);
    }

    private void FitColliderToModel()
    {
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule == null || bodyRenderers == null || bodyRenderers.Length == 0) return;

        // Compute combined world-space bounds of all renderers
        Bounds worldBounds = default;
        bool hasBounds = false;
        foreach (var r in bodyRenderers)
        {
            if (r == null) continue;
            if (!hasBounds)
            {
                worldBounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                worldBounds.Encapsulate(r.bounds);
            }
        }
        if (!hasBounds) return;

        // Convert world height to local space (collider is in local coords)
        float scaleY = Mathf.Max(transform.lossyScale.y, 0.001f);
        float localHeight = worldBounds.size.y / scaleY;
        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
        // Keep XZ center at zero so collider stays centered on the pivot
        localCenter.x = 0f;
        localCenter.z = 0f;

        // Place capsule from ground up (y=0 to y=localHeight)
        capsule.height = localHeight;
        capsule.center = new Vector3(0f, localHeight / 2f, 0f);

        // Create visible hitbox indicator using a capsule primitive
        var hitboxObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hitboxObj.name = "HitboxGizmo";
        hitboxObj.transform.SetParent(transform);
        // Unity capsule primitive: height=2, radius=0.5 at scale (1,1,1)
        float capsuleScaleY = localHeight / 2f;
        float capsuleScaleXZ = capsule.radius / 0.5f;
        hitboxObj.transform.localScale = new Vector3(capsuleScaleXZ, capsuleScaleY, capsuleScaleXZ);
        hitboxObj.transform.localPosition = new Vector3(0f, localHeight / 2f, 0f);
        hitboxObj.transform.localRotation = Quaternion.identity;

        // Remove auto-created collider so it doesn't interfere with gameplay
        var primCollider = hitboxObj.GetComponent<Collider>();
        if (primCollider != null) Destroy(primCollider);

        // Apply transparent red material (Sprites/Default supports alpha in all pipelines)
        var hitboxRenderer = hitboxObj.GetComponent<Renderer>();
        if (hitboxRenderer != null)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0.15f, 0.15f, 0.3f);
            mat.renderQueue = 3100; // Render on top of opaque geometry
            hitboxRenderer.material = mat;
            hitboxRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hitboxRenderer.receiveShadows = false;
        }

        Debug.Log($"[Enemy] Collider fitted to model: height={localHeight:F2}, center.y={localHeight / 2f:F2}, worldHeight={worldBounds.size.y:F2}");
    }

    public void ApplyDayScaling(float hpMultiplier, float damageMultiplier)
    {
        float dailyHP = DailyEventManager.Instance != null ? DailyEventManager.Instance.EnemyHPMultiplier : 1f;
        float dailyDmg = DailyEventManager.Instance != null ? DailyEventManager.Instance.EnemyDamageMultiplier : 1f;
        CurrentHP = Mathf.RoundToInt(data.maxHP * hpMultiplier * dailyHP);
        ScaledDamage = Mathf.RoundToInt(data.damage * damageMultiplier * dailyDmg);
        Debug.Log($"[Enemy] {data.enemyName} day-scaled: HP={CurrentHP} (base {data.maxHP}), Damage={ScaledDamage} (base {data.damage})");
    }

    private void Update()
    {
        if (damageFlashTimer > 0)
        {
            damageFlashTimer -= Time.deltaTime;
            if (damageFlashTimer <= 0)
            {
                RestoreColors();
            }
        }

        if (!IsDead) UpdateIdleAnimation();
    }

    private void UpdateIdleAnimation()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (actionAnimTimer > 0) { actionAnimTimer -= Time.deltaTime; return; }

        bool isMoving = agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.1f;
        if (isMoving)
        {
            if (animator.speed < 0.01f) animator.speed = 1f;
        }
        else
        {
            animator.Play("Walk", 0, IDLE_WALK_FRAME);
            animator.speed = 0f;
        }
    }

    private void RestoreColors()
    {
        if (bodyRenderers == null) return;
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (bodyRenderers[i] != null)
                bodyRenderers[i].material.color = originalColors[i];
        }
    }

    private void FlashWhite()
    {
        if (bodyRenderers == null) return;
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (bodyRenderers[i] != null)
                bodyRenderers[i].material.color = Color.white;
        }
        damageFlashTimer = 0.1f;
    }

    public void TriggerAttackAnimation()
    {
        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetTrigger("Attack");
            actionAnimTimer = 1.0f;
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        CurrentHP -= damage;
        Debug.Log($"[Enemy] {data?.enemyName} took {damage} damage at {transform.position}. HP={CurrentHP}");

        FloatingDamageNumber.Spawn(transform.position, damage, true);
        FlashWhite();

        if (CurrentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;

        // Play death sound based on enemy type
        if (SoundManager.Instance != null && data != null)
        {
            if (data.enemyType == EnemyType.WallBreaker)
                SoundManager.Instance.PlayTrollHit(transform.position);
            else
                SoundManager.Instance.PlayOrcHit(transform.position);
        }

        // Spawn loot
        if (data.maxLootDrops > 0)
        {
            SpawnLoot();
        }
        else
        {
            Debug.Log($"[Enemy] {data.enemyName} died at {transform.position}, no loot configured.");
        }

        OnEnemyDied?.Invoke(this);

        // Play die animation if available, then destroy after delay
        if (animator != null)
        {
            animator.SetTrigger("Die");
            // Disable movement and attack while dying
            var movement = GetComponent<EnemyMovement>();
            if (movement != null) movement.enabled = false;
            var attack = GetComponent<EnemyAttack>();
            if (attack != null) attack.enabled = false;
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            StartCoroutine(DestroyAfterAnimation());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator DestroyAfterAnimation()
    {
        // Die animation is 30 frames at 24fps ≈ 1.25 seconds
        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        var spawner = EnemySpawnManager.Instance;
        if (spawner == null)
        {
            Debug.LogError("[Enemy] SpawnLoot failed: EnemySpawnManager.Instance is null!");
            return;
        }
        if (spawner.TreasurePrefab == null)
        {
            Debug.LogError("[Enemy] SpawnLoot failed: TreasurePrefab is null!");
            return;
        }

        int dropCount = UnityEngine.Random.Range(data.minLootDrops, data.maxLootDrops + 1);
        int totalValue = 0;

        for (int i = 0; i < dropCount; i++)
        {
            // Scatter loot around the death position
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, 0, offset.y);

            var loot = Instantiate(spawner.TreasurePrefab, spawnPos, Quaternion.identity);
            var pickup = loot.GetComponent<TreasurePickup>();
            if (pickup != null)
            {
                int lootValue = UnityEngine.Random.Range(data.minLootValue, data.maxLootValue + 1);
                if (MutatorManager.IsActive("blood_tide")) lootValue = Mathf.RoundToInt(lootValue * 1.3f);
                if (MutatorManager.IsActive("golden_horde")) lootValue *= 3;
                pickup.Initialize(lootValue);
                totalValue += lootValue;
            }
            else
            {
                Debug.LogError("[Enemy] SpawnLoot: TreasurePickup component missing from prefab!");
            }
        }

        Debug.Log($"[Enemy] {data.enemyName} died at {transform.position}. Dropped {dropCount} loot (total value={totalValue})");
    }

}
