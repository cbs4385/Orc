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
        spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0)
        {
            SpawnRefugee();
            spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }

    private void SpawnRefugee()
    {
        if (refugeePrefab == null) return;

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

        var go = Instantiate(refugeePrefab, pos, Quaternion.identity);
        var refugee = go.GetComponent<Refugee>();

        // Randomly assign a power-up
        if (refugee != null && Random.value < powerUpChance)
        {
            var powerUps = new[] { RefugeePowerUp.DoubleShot, RefugeePowerUp.BurstDamage };
            refugee.SetPowerUp(powerUps[Random.Range(0, powerUps.Length)]);
        }
    }
}
