using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class VegetationManager : MonoBehaviour
{
    public static VegetationManager Instance { get; private set; }

    [SerializeField] private GameObject bushPrefab;
    [SerializeField] private GameObject treePrefab;

    private List<Vegetation> allVegetation = new List<Vegetation>();
    private bool isNight;

    private const int INITIAL_BUSHES = 30;
    private const int INITIAL_TREES = 10;
    private const int GLOBAL_CAP = 120;
    private const float MAP_RADIUS = 40f;
    private const float FORTRESS_EXCLUSION = 5f;
    private const float EDGE_BUFFER = 2f;
    private const float DENSITY_RADIUS = 6f;
    private const int DENSITY_CAP = 5;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Instance = this;
        var dnc = FindAnyObjectByType<DayNightCycle>();
        if (dnc != null)
        {
            dnc.OnNightStarted += OnNightStarted;
            dnc.OnDayStarted += OnDayStarted;
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        var dnc = FindAnyObjectByType<DayNightCycle>();
        if (dnc != null)
        {
            dnc.OnNightStarted -= OnNightStarted;
            dnc.OnDayStarted -= OnDayStarted;
        }
    }

    private void OnNightStarted()
    {
        isNight = true;
        Debug.Log("[VegetationManager] Night started — vegetation growth enabled.");
    }

    private void OnDayStarted()
    {
        isNight = false;
        Debug.Log("[VegetationManager] Day started — vegetation growth paused.");
    }

    private void Start()
    {
        SpawnInitialVegetation();
    }

    private void SpawnInitialVegetation()
    {
        int bushCount = 0;
        int treeCount = 0;

        // Spawn bushes
        for (int i = 0; i < INITIAL_BUSHES; i++)
        {
            Vector3 pos = GetRandomWesternPosition();
            if (pos != Vector3.zero)
            {
                SpawnVegetation(VegetationType.Bush, pos);
                bushCount++;
            }
        }

        // Spawn trees
        for (int i = 0; i < INITIAL_TREES; i++)
        {
            Vector3 pos = GetRandomWesternPosition();
            if (pos != Vector3.zero)
            {
                SpawnVegetation(VegetationType.Tree, pos);
                treeCount++;
            }
        }

        Debug.Log($"[VegetationManager] Initial spawn: {bushCount} bushes, {treeCount} trees");
    }

    private Vector3 GetRandomWesternPosition()
    {
        Vector3 fc = GameManager.FortressCenter;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            // Random position within western third of the play area
            float westernThirdX = -MAP_RADIUS + (2f * MAP_RADIUS) / 3f; // ~-13.3
            float x = Random.Range(-MAP_RADIUS + EDGE_BUFFER, westernThirdX);
            float z = Random.Range(-MAP_RADIUS + EDGE_BUFFER, MAP_RADIUS - EDGE_BUFFER);

            Vector3 candidate = new Vector3(x, 0, z);

            // Check within map radius from origin
            if (candidate.magnitude > MAP_RADIUS - EDGE_BUFFER) continue;

            // Exclude area around fortress
            float distToFortress = Vector3.Distance(candidate, fc);
            if (distToFortress < FORTRESS_EXCLUSION) continue;

            // Validate on NavMesh
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                return new Vector3(hit.position.x, 0, hit.position.z);
            }
        }

        return Vector3.zero;
    }

    private Vegetation SpawnVegetation(VegetationType type, Vector3 position)
    {
        GameObject prefab = type == VegetationType.Bush ? bushPrefab : treePrefab;
        if (prefab == null)
        {
            Debug.LogError($"[VegetationManager] {type} prefab is null!");
            return null;
        }

        var go = Instantiate(prefab, position, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
        var veg = go.GetComponent<Vegetation>();
        if (veg == null) veg = go.AddComponent<Vegetation>();
        veg.Setup(type);
        allVegetation.Add(veg);

        Debug.Log($"[VegetationManager] Spawned {type} at {position}");
        return veg;
    }

    public void TryGrowNear(Vegetation parent)
    {
        if (!isNight) return; // Vegetation only grows at night
        if (parent == null || parent.IsDead) return;
        if (allVegetation.Count >= GLOBAL_CAP) return;

        // 50% chance per tick
        if (Random.value > 0.5f) return;

        Vector3 parentPos = parent.transform.position;

        // Check density around parent
        if (CountVegetationInRadius(parentPos, DENSITY_RADIUS) >= DENSITY_CAP) return;

        // Find a valid position 2-5 units from parent
        for (int attempt = 0; attempt < 10; attempt++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(2f, 5f);
            Vector3 candidate = parentPos + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);

            // Must be within map bounds
            if (candidate.magnitude > MAP_RADIUS - EDGE_BUFFER) continue;

            // Exclude fortress area
            float distToFortress = Vector3.Distance(candidate, GameManager.FortressCenter);
            if (distToFortress < FORTRESS_EXCLUSION) continue;

            // Check density at spawn point too
            if (CountVegetationInRadius(candidate, DENSITY_RADIUS) >= DENSITY_CAP) continue;

            // Validate on NavMesh
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                Vector3 spawnPos = new Vector3(hit.position.x, 0, hit.position.z);

                // 80/20 type ratio: same type as parent 80%, opposite 20%
                VegetationType newType = Random.value < 0.8f ? parent.Type :
                    (parent.Type == VegetationType.Bush ? VegetationType.Tree : VegetationType.Bush);

                SpawnVegetation(newType, spawnPos);
                Debug.Log($"[VegetationManager] Growth near {parent.Type}: spawned {newType} at {spawnPos}");
                return;
            }
        }
    }

    private int CountVegetationInRadius(Vector3 center, float radius)
    {
        int count = 0;
        float radiusSqr = radius * radius;
        for (int i = allVegetation.Count - 1; i >= 0; i--)
        {
            if (allVegetation[i] == null || allVegetation[i].IsDead)
            {
                allVegetation.RemoveAt(i);
                continue;
            }
            if ((allVegetation[i].transform.position - center).sqrMagnitude <= radiusSqr)
            {
                count++;
            }
        }
        return count;
    }

    public Vegetation FindNearestVegetation(Vector3 pos, float range)
    {
        Vegetation nearest = null;
        float bestDist = range;

        for (int i = allVegetation.Count - 1; i >= 0; i--)
        {
            if (allVegetation[i] == null || allVegetation[i].IsDead)
            {
                allVegetation.RemoveAt(i);
                continue;
            }
            float dist = Vector3.Distance(pos, allVegetation[i].transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                nearest = allVegetation[i];
            }
        }
        return nearest;
    }

    public void OnVegetationDestroyed(Vegetation veg)
    {
        allVegetation.Remove(veg);
        Debug.Log($"[VegetationManager] Vegetation removed. Total remaining: {allVegetation.Count}");
    }
}
