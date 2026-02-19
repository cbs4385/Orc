using UnityEngine;

public class TreasurePickup : MonoBehaviour
{
    [SerializeField] private int value = 10;
    private bool collected;
    private float spawnGameTime;

    public int Value => value;
    public bool IsCollected => collected;

    public void Initialize(int treasureValue)
    {
        value = treasureValue;
    }

    private void Start()
    {
        spawnGameTime = GameManager.Instance != null ? GameManager.Instance.GameTime : 0f;
    }

    private void Update()
    {
        // Rotate the loot for visual effect
        transform.Rotate(Vector3.up, 90f * Time.deltaTime);

        // Despawn after 24 game hours (one full day+night cycle)
        if (!collected && GameManager.Instance != null && DayNightCycle.Instance != null)
        {
            float elapsed = GameManager.Instance.GameTime - spawnGameTime;
            if (elapsed >= DayNightCycle.Instance.FullCycleDuration)
            {
                Debug.Log($"[TreasurePickup] Loot worth {value} despawned at {transform.position} after {elapsed:F1}s");
                collected = true;
                Destroy(gameObject);
            }
        }
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;
        Destroy(gameObject);
    }
}
