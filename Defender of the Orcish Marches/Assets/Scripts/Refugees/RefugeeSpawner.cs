using UnityEngine;

public class RefugeeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject refugeePrefab;
    [SerializeField] private float minSpawnInterval = 30f;
    [SerializeField] private float maxSpawnInterval = 60f;
    [SerializeField] private float mapRadius = 40f;
    [SerializeField] [Range(0f, 1f)] private float powerUpChance = 0.3f;

    private float spawnTimer;

    private void Start()
    {
        spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval) * GameSettings.GetRefugeeSpawnMultiplier();
        Debug.Log($"[RefugeeSpawner] Initialized. interval=[{minSpawnInterval},{maxSpawnInterval}], powerUpChance={powerUpChance}, firstSpawn in {spawnTimer:F1}s");
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Don't spawn refugees at night
        if (DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnRefugee();
            spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval) * GameSettings.GetRefugeeSpawnMultiplier();
        }
    }

    private void SpawnRefugee()
    {
        if (refugeePrefab == null)
        {
            Debug.LogError("[RefugeeSpawner] refugeePrefab is null! Cannot spawn refugee.");
            return;
        }

        int side = Random.Range(0, 4);
        float offset = Random.Range(-mapRadius * 0.8f, mapRadius * 0.8f);
        Vector3 pos;
        switch (side)
        {
            case 0: pos = new Vector3(-mapRadius, 0, offset); break;
            case 1: pos = new Vector3(mapRadius, 0, offset); break;
            case 2: pos = new Vector3(offset, 0, -mapRadius); break;
            default: pos = new Vector3(offset, 0, mapRadius); break;
        }

        Debug.Log($"[RefugeeSpawner] Spawning refugee at {pos} (side={side})");
        var go = Instantiate(refugeePrefab, pos, Quaternion.identity);
        var refugee = go.GetComponent<Refugee>();

        // Randomly assign a power-up
        if (refugee != null && Random.value < powerUpChance)
        {
            var powerUps = new[] { RefugeePowerUp.DoubleShot, RefugeePowerUp.BurstDamage };
            var chosenPowerUp = powerUps[Random.Range(0, powerUps.Length)];
            refugee.SetPowerUp(chosenPowerUp);
            Debug.Log($"[RefugeeSpawner] Refugee assigned power-up: {chosenPowerUp}");
        }
    }
}
