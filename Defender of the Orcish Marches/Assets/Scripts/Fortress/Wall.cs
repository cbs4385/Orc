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

    private Renderer rend;
    private Color originalColor;
    private static readonly Color damagedColor = new Color(0.6f, 0.2f, 0.2f);

    private void Awake()
    {
        CurrentHP = maxHP;
        Corners = GetComponent<WallCorners>();
        rend = GetComponentInChildren<Renderer>();
        if (rend != null) originalColor = rend.material.color;
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
            // Rebuild the wall
            gameObject.SetActive(true);
            Debug.Log($"[Wall] Rebuilt at {transform.position}, HP={CurrentHP}/{maxHP}");
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (rend == null) return;
        float hpRatio = (float)CurrentHP / maxHP;
        rend.material.color = Color.Lerp(damagedColor, originalColor, hpRatio);
    }
}
