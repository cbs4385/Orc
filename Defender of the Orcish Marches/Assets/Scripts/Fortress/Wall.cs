using System;
using UnityEngine;

public class Wall : MonoBehaviour
{
    [SerializeField] private int maxHP = 100;
    public int CurrentHP { get; private set; }
    public int MaxHP => maxHP;
    public bool IsDestroyed => CurrentHP <= 0;

    public event Action<Wall> OnWallDestroyed;
    public event Action<Wall> OnWallDamaged;

    public WallCorners Corners { get; private set; }

    private Renderer[] renderers;
    private Color[][] originalColors; // Per-renderer, per-material original colors
    private static readonly Color damagedColor = new Color(0.6f, 0.2f, 0.2f);

    private void Awake()
    {
        CurrentHP = maxHP;
        Corners = GetComponent<WallCorners>();

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

    public void TakeDamage(int damage)
    {
        if (IsDestroyed) return;
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

    public void Repair(int amount)
    {
        bool wasDestroyed = IsDestroyed;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);

        if (wasDestroyed && CurrentHP > 0)
        {
            gameObject.SetActive(true);
            Debug.Log($"[Wall] Rebuilt at {transform.position}, HP={CurrentHP}/{maxHP}");
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
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
