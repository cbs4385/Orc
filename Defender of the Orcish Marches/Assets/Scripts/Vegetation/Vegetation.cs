using UnityEngine;

public enum VegetationType
{
    Bush,
    Tree
}

public class Vegetation : MonoBehaviour
{
    [SerializeField] private VegetationType vegetationType = VegetationType.Bush;
    [SerializeField] private int maxHP = 25;

    private int currentHP;
    private float growthTimer;
    private Renderer[] renderers;
    private Color[] originalColors;
    private float flashTimer;
    private const float FLASH_DURATION = 0.1f;

    public VegetationType Type => vegetationType;
    public bool IsDead { get; private set; }

    public static event System.Action OnVegetationCleared;

    public float ClearTimeRequired => vegetationType == VegetationType.Bush ? 0.5f : 1.0f;

    public void Setup(VegetationType type)
    {
        vegetationType = type;
        maxHP = type == VegetationType.Bush ? 25 : 100;
        currentHP = maxHP;
        growthTimer = Random.Range(15f, 30f);
    }

    private void Awake()
    {
        currentHP = maxHP;
        growthTimer = Random.Range(15f, 30f);

        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }
    }

    private void Update()
    {
        if (IsDead) return;

        // White flash recovery
        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0)
            {
                RestoreColors();
            }
        }

        // Growth timer
        growthTimer -= Time.deltaTime;
        if (growthTimer <= 0)
        {
            growthTimer = Random.Range(15f, 30f);
            if (VegetationManager.Instance != null)
            {
                VegetationManager.Instance.TryGrowNear(this);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHP -= damage;
        FloatingDamageNumber.Spawn(transform.position + Vector3.up * 0.5f, damage, false);
        FlashWhite();
        Debug.Log($"[Vegetation] {vegetationType} took {damage} damage at {transform.position}, HP={currentHP}/{maxHP}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Clear()
    {
        if (IsDead) return;
        Debug.Log($"[Vegetation] {vegetationType} cleared by menial at {transform.position}");
        OnVegetationCleared?.Invoke();
        Die();
    }

    private void Die()
    {
        IsDead = true;
        if (VegetationManager.Instance != null)
        {
            VegetationManager.Instance.OnVegetationDestroyed(this);
        }
        Debug.Log($"[Vegetation] {vegetationType} destroyed at {transform.position}");
        Destroy(gameObject);
    }

    private void FlashWhite()
    {
        flashTimer = FLASH_DURATION;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = Color.white;
        }
    }

    private void RestoreColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = originalColors[i];
        }
    }
}
