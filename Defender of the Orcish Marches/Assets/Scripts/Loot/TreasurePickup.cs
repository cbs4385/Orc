using UnityEngine;

public class TreasurePickup : MonoBehaviour
{
    [SerializeField] private int value = 10;
    private bool collected;
    private int spawnDay;

    public int Value => value;
    public bool IsCollected => collected;

    public void Initialize(int treasureValue)
    {
        value = treasureValue;
    }

    private void Start()
    {
        spawnDay = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;
    }

    private void Update()
    {
        // Rotate the loot for visual effect
        transform.Rotate(Vector3.up, 90f * Time.deltaTime);

        // Despawn after one full day cycle
        if (!collected && DayNightCycle.Instance != null && DayNightCycle.Instance.DayNumber > spawnDay)
        {
            Debug.Log($"[TreasurePickup] Loot worth {value} despawned at {transform.position} (spawned day {spawnDay}, now day {DayNightCycle.Instance.DayNumber})");
            collected = true;
            Destroy(gameObject);
        }
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;
        Destroy(gameObject);
    }
}
