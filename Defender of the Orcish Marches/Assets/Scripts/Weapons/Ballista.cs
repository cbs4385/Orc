using UnityEngine;
using UnityEngine.InputSystem;

public class Ballista : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float fireRate = 1.5f;
    [SerializeField] private int damage = 20;
    [SerializeField] private float projectileSpeed = 25f;
    [SerializeField] private float maxRange = 30f;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;

    private float fireCooldown;
    private UnityEngine.Camera mainCam;

    // Power-up state
    private bool hasDoubleShot;
    private bool hasBurstDamage;
    private float doubleShotSpreadAngle = 8f;
    private float burstDamageRadius = 3f;

    // Aim lines
    private LineRenderer aimLine;
    private LineRenderer aimLineSpread;

    // Nightmare FPS mode
    private bool isNightmareMode;
    private float yaw;

    // Hard mode aim wobble
    private bool hasAimWobble;
    private const float AIM_WOBBLE_DEGREES = 0.5f;

    public int Damage => damage;
    public float FireRate => fireRate;
    public bool HasDoubleShot => hasDoubleShot;
    public bool HasBurstDamage => hasBurstDamage;
    public static event System.Action OnBallistaShotFired;

    public bool IsNightmareMode => isNightmareMode;
    public Transform FirePoint => firePoint;
    public float ProjectileSpeed => projectileSpeed;
    public float MaxRange => maxRange;
    public float BurstDamageRadius => hasBurstDamage ? burstDamageRadius : 0f;

    private void Start()
    {
        mainCam = UnityEngine.Camera.main;
        if (firePoint == null) firePoint = transform;
        isNightmareMode = NightmareCamera.IsNightmareMode;

        if (MutatorManager.IsActive("lone_ballista"))
        {
            damage *= 2;
            fireRate *= 1.5f;
            Debug.Log($"[Ballista] Lone Ballista mutator applied: damage={damage}, fireRate={fireRate}");
        }

        // Commander and meta-progression ballista bonuses
        damage += MetaProgressionManager.GetBonusBallistaDamage();
        damage = Mathf.RoundToInt(damage * CommanderManager.GetBallistaDamageMultiplier());
        fireRate *= MetaProgressionManager.GetBallistaFireRateMultiplier();
        fireRate *= CommanderManager.GetBallistaFireRateMultiplier();

        // Relic ballista multipliers
        if (RelicManager.Instance != null)
        {
            damage = Mathf.RoundToInt(damage * RelicManager.Instance.GetBallistaDamageMultiplier());
            fireRate *= RelicManager.Instance.GetBallistaFireRateMultiplier();
        }

        Debug.Log($"[Ballista] Initialized. damage={damage}, fireRate={fireRate}, range={maxRange}, nightmare={isNightmareMode}");

        // Aim lines only visible on Easy difficulty
        bool showAimLines = GameSettings.CurrentDifficulty == Difficulty.Easy && !isNightmareMode;
        aimLine = CreateAimLine("AimLine");
        aimLineSpread = CreateAimLine("AimLineSpread");
        aimLine.enabled = showAimLines;
        aimLineSpread.enabled = false;

        // Hard mode aim wobble
        hasAimWobble = GameSettings.CurrentDifficulty == Difficulty.Hard;

        if (isNightmareMode)
        {
            // Face west (-X direction) initially
            yaw = 270f;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Debug.Log($"[Ballista] Nightmare FPS mode enabled. Initial yaw={yaw}");
        }

        Debug.Log($"[Ballista] aimLines={showAimLines}, aimWobble={hasAimWobble}, difficulty={GameSettings.CurrentDifficulty}");
    }

    private LineRenderer CreateAimLine(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0f, 0f, 0.6f);
        lr.endColor = new Color(1f, 0f, 0f, 0.15f);
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        return lr;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // In nightmare + build mode, skip all input (don't rotate/fire while placing walls)
        if (isNightmareMode && BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildMode) return;

        if (!isNightmareMode && mainCam == null) { mainCam = UnityEngine.Camera.main; return; }

        fireCooldown -= Time.deltaTime;

        RotateTowardsMouse();
        if (!isNightmareMode) UpdateAimLines();

        if (Mouse.current != null && Mouse.current.leftButton.isPressed && fireCooldown <= 0f)
        {
            Fire();
        }
    }

    private void UpdateAimLines()
    {
        if (aimLine == null || !aimLine.enabled) return;

        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector3 direction = mouseWorld - firePoint.position;
        direction.y = 0;
        if (direction.sqrMagnitude < 0.01f) return;
        direction.Normalize();

        Vector3 origin = new Vector3(firePoint.position.x, 0.5f, firePoint.position.z);

        aimLine.SetPosition(0, origin);
        aimLine.SetPosition(1, origin + direction * maxRange);

        if (aimLineSpread != null)
        {
            aimLineSpread.enabled = hasDoubleShot;
            if (hasDoubleShot)
            {
                Vector3 spreadDir = Quaternion.Euler(0, doubleShotSpreadAngle, 0) * direction;
                aimLineSpread.SetPosition(0, origin);
                aimLineSpread.SetPosition(1, origin + spreadDir * maxRange);
            }
        }
    }

    private void RotateTowardsMouse()
    {
        if (isNightmareMode)
        {
            RotateFPS();
            return;
        }

        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector3 direction = mouseWorld - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
        }
    }

    private void RotateFPS()
    {
        if (Mouse.current == null) return;
        Vector2 delta = Mouse.current.delta.ReadValue();
        yaw += delta.x * 0.2f;
        // Read pitch from NightmareCamera so the ballista model visually tilts
        float pitch = NightmareCamera.Instance != null ? NightmareCamera.Instance.Pitch : 0f;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void Fire()
    {
        fireCooldown = 1f / fireRate;
        OnBallistaShotFired?.Invoke();

        if (projectilePrefab == null)
        {
            Debug.LogError("[Ballista] projectilePrefab is null! Cannot fire.");
            return;
        }

        Vector3 direction;
        Vector3 spawnPos;

        if (isNightmareMode)
        {
            // Use FPS camera forward for 3D direction (includes pitch)
            var cam = UnityEngine.Camera.main;
            direction = cam != null ? cam.transform.forward : transform.forward;
            spawnPos = firePoint.position;
        }
        else
        {
            Vector3 mouseWorld = GetMouseWorldPosition();
            direction = (mouseWorld - firePoint.position);
            direction.y = 0;
            direction.Normalize();
            spawnPos = new Vector3(firePoint.position.x, 0.5f, firePoint.position.z);
        }

        // Hard mode: apply random aim wobble
        if (hasAimWobble)
        {
            float wobbleYaw = Random.Range(-AIM_WOBBLE_DEGREES, AIM_WOBBLE_DEGREES);
            float wobblePitch = Random.Range(-AIM_WOBBLE_DEGREES, AIM_WOBBLE_DEGREES);
            direction = Quaternion.Euler(wobblePitch, wobbleYaw, 0f) * direction;
        }

        Debug.Log($"[Ballista] Firing. dir={direction}, doubleShot={hasDoubleShot}, burstDamage={hasBurstDamage}, wobble={hasAimWobble}");

        // Fire main projectile
        if (SoundManager.Instance != null) SoundManager.Instance.PlayScorpioFire(spawnPos);
        SpawnProjectile(spawnPos, direction);

        // Double shot: fire a second projectile at a slight angle
        if (hasDoubleShot)
        {
            Vector3 spreadDir = Quaternion.Euler(0, doubleShotSpreadAngle, 0) * direction;
            SpawnProjectile(spawnPos, spreadDir);
        }
    }

    private void SpawnProjectile(Vector3 position, Vector3 direction)
    {
        GameObject proj = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));
        var projectile = proj.GetComponent<BallistaProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(direction, projectileSpeed, damage, maxRange, hasBurstDamage, burstDamageRadius, isNightmareMode);
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = mainCam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return transform.position + transform.forward * 10f;
    }

    public void UpgradeDamage(int amount)
    {
        damage += amount;
        Debug.Log($"[Ballista] Damage upgraded by {amount}. New damage={damage}");
    }

    public void UpgradeFireRate(float amount)
    {
        fireRate += amount;
        Debug.Log($"[Ballista] Fire rate upgraded by {amount}. New fireRate={fireRate}");
    }

    public void EnableDoubleShot()
    {
        hasDoubleShot = true;
        Debug.Log("[Ballista] Double shot enabled.");
    }

    public void EnableBurstDamage()
    {
        hasBurstDamage = true;
        Debug.Log("[Ballista] Burst damage enabled.");
    }
}
