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

    public WallCorners Corners { get; private set; }

    private Renderer[] renderers;
    private Color[][] originalColors; // Per-renderer, per-material original colors
    private static readonly Color damagedColor = new Color(0.6f, 0.2f, 0.2f);
    private static readonly Color constructionColor = new Color(0.5f, 0.4f, 0.3f);

    private void Awake()
    {
        CurrentHP = maxHP;
        Corners = GetComponent<WallCorners>();

        // Ensure the root BoxCollider covers the wall body
        var boxCol = GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            boxCol.center = new Vector3(0, 1f, 0);
            boxCol.size = new Vector3(1f, 2f, 0.5f);
        }

        // Add capsule colliders for the octagonal towers at each end
        AddTowerColliders();

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

        // Solid (non-trigger) colliders so Physics.OverlapSphere can detect them
        // for engineer stand position validation.
        var leftTower = new GameObject("TowerCollider_L");
        leftTower.transform.SetParent(transform, false);
        leftTower.transform.localPosition = new Vector3(-towerOffset, 1f, 0);
        var leftCap = leftTower.AddComponent<CapsuleCollider>();
        leftCap.radius = towerRadius;
        leftCap.height = 2f;
        leftCap.direction = 1; // Y-axis

        var rightTower = new GameObject("TowerCollider_R");
        rightTower.transform.SetParent(transform, false);
        rightTower.transform.localPosition = new Vector3(towerOffset, 1f, 0);
        var rightCap = rightTower.AddComponent<CapsuleCollider>();
        rightCap.radius = towerRadius;
        rightCap.height = 2f;
        rightCap.direction = 1; // Y-axis
    }

    public void TakeDamage(int damage)
    {
        if (IsDestroyed || IsUnderConstruction) return;
        CurrentHP = Mathf.Max(0, CurrentHP - damage);
        FloatingDamageNumber.Spawn(transform.position, damage, false);
        OnWallDamaged?.Invoke(this);
        UpdateVisual();

        if (CurrentHP <= 0)
        {
            if (SoundManager.Instance != null) SoundManager.Instance.PlayWallCollapse(transform.position);
            OnWallDestroyed?.Invoke(this);
            gameObject.SetActive(false);
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
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);

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
