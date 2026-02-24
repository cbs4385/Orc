using System;
using UnityEngine;

public class Wall : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;
    public int CurrentHP { get; private set; }
    public int MaxHP => maxHP;
    public bool IsDestroyed => CurrentHP <= 0 && !IsUnderConstruction;
    public bool IsUnderConstruction { get; private set; }

    public event Action<Wall> OnWallDestroyed;
    public event Action<Wall> OnWallDamaged;
    public static event Action<int> OnWallRepaired;

    public WallCorners Corners { get; private set; }

    private Renderer[] renderers;
    private Color[][] originalColors; // Per-renderer, per-material original colors
    private static readonly Color damagedColor = new Color(0.6f, 0.2f, 0.2f);
    private static readonly Color constructionColor = new Color(0.5f, 0.4f, 0.3f);
    private bool torchesAdded;

    private void Awake()
    {
        CurrentHP = maxHP;
        Corners = GetComponent<WallCorners>();

        // Ensure the root BoxCollider covers the wall body
        var boxCol = GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            boxCol.center = new Vector3(0, 1.5f, 0);
            boxCol.size = new Vector3(1f, 3f, 0.5f);
        }

        // Add capsule colliders for the octagonal towers at each end
        AddTowerColliders();

        // Add invisible pathing cube for PathingRayManager ray detection
        AddPathingCube();

        // Gather ALL renderers (FBX model has one renderer with 4 material sub-meshes)
        renderers = GetComponentsInChildren<Renderer>();

        // Cache original colors for each material on each renderer
        originalColors = new Color[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                Material[] mats = renderers[i].materials;
                originalColors[i] = new Color[mats.Length];
                for (int j = 0; j < mats.Length; j++)
                    originalColors[i][j] = mats[j].color;
            }
        }

        Debug.Log($"[Wall] Initialized at {transform.position}, HP={maxHP}, renderers={renderers.Length}");
    }

    private void AddTowerColliders()
    {
        // Skip tower colliders on ghost walls (placement preview)
        var corners = GetComponent<WallCorners>();
        if (corners != null && corners.isGhost) return;

        float towerOffset = WallCorners.TOWER_OFFSET;
        float towerRadius = WallCorners.OCT_APOTHEM;

        // "EnemyBlock" layer: included in enemy NavMesh bake but excluded from humanoid.
        // Tower colliders on this layer bake as static obstacles into the enemy NavMesh,
        // blocking enemies at tower endpoints without sealing humanoid passage gaps.
        int enemyBlockLayer = LayerMask.NameToLayer("EnemyBlock");

        // Solid (non-trigger) colliders so Physics.OverlapSphere can detect them
        // for engineer stand position validation.
        var leftTower = new GameObject("TowerCollider_L");
        leftTower.transform.SetParent(transform, false);
        leftTower.transform.localPosition = new Vector3(-towerOffset, 1.5f, 0);
        if (enemyBlockLayer >= 0) leftTower.layer = enemyBlockLayer;
        var leftCap = leftTower.AddComponent<CapsuleCollider>();
        leftCap.radius = towerRadius;
        leftCap.height = 3f;
        leftCap.direction = 1; // Y-axis

        var rightTower = new GameObject("TowerCollider_R");
        rightTower.transform.SetParent(transform, false);
        rightTower.transform.localPosition = new Vector3(towerOffset, 1.5f, 0);
        if (enemyBlockLayer >= 0) rightTower.layer = enemyBlockLayer;
        var rightCap = rightTower.AddComponent<CapsuleCollider>();
        rightCap.radius = towerRadius;
        rightCap.height = 3f;
        rightCap.direction = 1; // Y-axis
    }

    /// <summary>
    /// Creates an invisible trigger plane on the PathingRay layer running along the
    /// wall's center axis. The plane extends from tower CENTER to tower CENTER (not
    /// edge-to-edge), so adjacent wall segments' planes meet at the shared tower
    /// center without overlapping. This prevents double-counting when PathingRayManager
    /// SphereCasts count wall crossings per direction.
    /// </summary>
    private void AddPathingCube()
    {
        var corners = GetComponent<WallCorners>();
        if (corners != null && corners.isGhost) return;

        var cubeGO = new GameObject("PathingCube");
        cubeGO.transform.SetParent(transform, false);
        cubeGO.layer = LayerMask.NameToLayer("PathingRay");

        var box = cubeGO.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, 1f, 0f);
        // Width (local X): tower-center to tower-center (no overlap with neighbors).
        // Height (local Y): wall height.
        // Depth (local Z): thin plane (0.1) — just thick enough for SphereCast detection.
        box.size = new Vector3(
            2f * WallCorners.TOWER_OFFSET,
            2f,
            0.1f
        );

        int layerIdx = cubeGO.layer;
        Vector3 worldCenter = cubeGO.transform.TransformPoint(box.center);
        // Compute world-space size accounting for wall rotation
        Vector3 halfExtents = box.size * 0.5f;
        Vector3 worldRight = cubeGO.transform.right * halfExtents.x;
        Vector3 worldUp = cubeGO.transform.up * halfExtents.y;
        Vector3 worldFwd = cubeGO.transform.forward * halfExtents.z;
        Debug.Log($"[Wall] PathingCube for {name}: layer={layerIdx} ({LayerMask.LayerToName(layerIdx)}), " +
            $"localSize={box.size}, worldCenter={worldCenter}, " +
            $"wallRot={transform.eulerAngles.y:F0}°, " +
            $"worldRight={worldRight} (width axis), worldFwd={worldFwd} (depth axis)");
    }

    public void TakeDamage(int damage)
    {
        if (IsDestroyed || IsUnderConstruction) return;
        int prevHP = CurrentHP;
        CurrentHP = Mathf.Max(0, CurrentHP - damage);
        Debug.Log($"[Wall] {name} took {damage} damage at {transform.position}. HP: {prevHP} -> {CurrentHP}/{maxHP}");
        FloatingDamageNumber.Spawn(transform.position, damage, false);
        OnWallDamaged?.Invoke(this);
        UpdateVisual();

        if (CurrentHP <= 0)
        {
            Debug.Log($"[Wall] {name} DESTROYED at {transform.position}! Breach created.");
            if (SoundManager.Instance != null) SoundManager.Instance.PlayWallCollapse(transform.position);
            OnWallDestroyed?.Invoke(this);
            gameObject.SetActive(false);
            // Rebake enemy NavMesh so tower geometry is updated, then retarget
            if (WallManager.Instance != null) WallManager.Instance.RebakeEnemyNavMesh();
            EnemyMovement.ForceAllRetarget();
        }
    }

    public void SetUnderConstruction()
    {
        IsUnderConstruction = true;
        CurrentHP = 0;

        // Disable colliders so enemies/raycasts pass through
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Disable NavMeshObstacle so it doesn't block pathfinding
        var obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (obstacle != null) obstacle.enabled = false;

        UpdateConstructionVisual();
        Debug.Log($"[Wall] {name} set under construction at {transform.position}");
    }

    public void Repair(int amount)
    {
        bool wasDestroyed = CurrentHP <= 0 && !IsUnderConstruction;
        bool wasConstruction = IsUnderConstruction;
        int prevHP = CurrentHP;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        int actualRepaired = CurrentHP - prevHP;
        if (actualRepaired > 0)
            OnWallRepaired?.Invoke(actualRepaired);

        if (wasDestroyed && CurrentHP > 0)
        {
            gameObject.SetActive(true);
            Debug.Log($"[Wall] Rebuilt at {transform.position}, HP={CurrentHP}/{maxHP}");
        }

        if (wasConstruction && CurrentHP > 0)
        {
            IsUnderConstruction = false;

            // Re-enable colliders
            foreach (var col in GetComponentsInChildren<Collider>())
                col.enabled = true;

            // Re-enable NavMeshObstacle
            var obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obstacle != null) obstacle.enabled = true;

            Debug.Log($"[Wall] {name} construction complete at {transform.position}, HP={CurrentHP}/{maxHP}");

            // Add torches to completed wall
            if (!torchesAdded)
            {
                torchesAdded = true;
                if (TorchManager.Instance != null)
                    TorchManager.Instance.AddWallTorches(this);
            }

            // Rebake enemy NavMesh to include new tower geometry, then retarget
            if (WallManager.Instance != null) WallManager.Instance.RebakeEnemyNavMesh();
            EnemyMovement.ForceAllRetarget();
        }

        UpdateVisual();
    }

    private void UpdateConstructionVisual()
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material[] mats = renderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
                mats[j].color = constructionColor;
            renderers[i].materials = mats;
        }
    }

    private void UpdateVisual()
    {
        if (IsUnderConstruction) { UpdateConstructionVisual(); return; }
        if (renderers == null || originalColors == null) return;
        float hpRatio = (float)CurrentHP / maxHP;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || originalColors[i] == null) continue;
            Material[] mats = renderers[i].materials;
            for (int j = 0; j < mats.Length && j < originalColors[i].Length; j++)
            {
                mats[j].color = Color.Lerp(damagedColor, originalColors[i][j], hpRatio);
            }
            renderers[i].materials = mats;
        }
    }
}
