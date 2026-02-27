using UnityEngine;

public class TreasurePickup : MonoBehaviour
{
    [SerializeField] private int value = 10;
    [Header("Model Override")]
    [SerializeField] private GameObject modelPrefab;

    private bool collected;
    private float spawnGameTime;
    private bool spawnGameTimeSet;

    public int Value => value;
    public bool IsCollected => collected;

    public void Initialize(int treasureValue)
    {
        float dailyLoot = DailyEventManager.Instance != null ? DailyEventManager.Instance.LootValueMultiplier : 1f;
        float commanderLoot = CommanderManager.GetLootValueMultiplier();
        float relicLoot = RelicManager.Instance != null ? RelicManager.Instance.GetLootValueMultiplier() : 1f;
        float metaLoot = MetaProgressionManager.GetLootValueMultiplier();
        value = Mathf.RoundToInt(treasureValue * dailyLoot * commanderLoot * relicLoot * metaLoot);
        if (GameManager.Instance != null)
        {
            spawnGameTime = GameManager.Instance.GameTime;
            spawnGameTimeSet = true;
            Debug.Log($"[TreasurePickup] Spawned with value={value} at {transform.position}, gameTime={spawnGameTime:F1}");
        }
    }

    private void Awake()
    {
        if (modelPrefab != null)
        {
            // Destroy any child objects
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            // Remove the root mesh (cube primitive has MeshRenderer/MeshFilter on root)
            var rootRenderer = GetComponent<MeshRenderer>();
            if (rootRenderer != null) Destroy(rootRenderer);
            var rootFilter = GetComponent<MeshFilter>();
            if (rootFilter != null) Destroy(rootFilter);

            var newModel = Instantiate(modelPrefab, transform);
            newModel.name = "Model";
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localScale = Vector3.one;

            Debug.Log("[TreasurePickup] Custom model loaded");
        }
    }

    private void Start()
    {
        // Fallback for pre-placed loot or if Initialize() wasn't called
        if (!spawnGameTimeSet && GameManager.Instance != null)
        {
            spawnGameTime = GameManager.Instance.GameTime;
            spawnGameTimeSet = true;
        }
    }

    private void Update()
    {
        // Rotate the loot for visual effect
        transform.Rotate(Vector3.up, 90f * Time.deltaTime);

        // Despawn after 24 game hours (one full day+night cycle)
        if (!collected && spawnGameTimeSet && GameManager.Instance != null && DayNightCycle.Instance != null)
        {
            float elapsed = GameManager.Instance.GameTime - spawnGameTime;
            if (elapsed >= DayNightCycle.Instance.FullCycleDuration)
            {
                Debug.Log($"[TreasurePickup] Loot worth {value} despawned at {transform.position} after {elapsed:F1}s (gameTime={GameManager.Instance.GameTime:F1}, spawnTime={spawnGameTime:F1})");
                collected = true;
                Destroy(gameObject);
            }
        }
    }

    /// <summary>Restore value directly from save (bypasses multiplier calculations).</summary>
    public void RestoreValue(int savedValue)
    {
        value = savedValue;
        if (GameManager.Instance != null)
        {
            spawnGameTime = GameManager.Instance.GameTime;
            spawnGameTimeSet = true;
        }
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;
        Debug.Log($"[TreasurePickup] Collected! value={value} at {transform.position}");
        Destroy(gameObject);
    }
}
