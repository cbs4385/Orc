using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenialManager : MonoBehaviour
{
    public static MenialManager Instance { get; private set; }

    [SerializeField] private GameObject menialPrefab;
    [SerializeField] private Transform menialSpawnPoint;

    [SerializeField] private float lootSearchRadius = 5f;

    private List<Menial> allMenials = new List<Menial>();
    private UnityEngine.Camera mainCam;
    private List<GameObject> activeBanners = new List<GameObject>();

    // Menial count audit
    private float auditTimer;
    private const float AUDIT_INTERVAL = 5f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[MenialManager] Instance registered in Awake.");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[MenialManager] Instance re-registered in OnEnable after domain reload.");
        }

        // Rebuild allMenials list after domain reload (static fields reset but scene objects survive)
        if (allMenials.Count == 0)
        {
            var sceneMenials = FindObjectsByType<Menial>(FindObjectsSortMode.None);
            foreach (var m in sceneMenials)
            {
                if (m != null && !m.IsDead)
                    allMenials.Add(m);
            }
            if (sceneMenials.Length > 0)
                Debug.Log($"[MenialManager] Rebuilt menial list: {allMenials.Count} menials found.");
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        mainCam = UnityEngine.Camera.main;

        // Spawn starting menials
        if (GameManager.Instance != null)
        {
            for (int i = 0; i < GameManager.Instance.MenialCount; i++)
            {
                SpawnMenial();
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Right-click to send menial to collect loot
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            TrySendMenialToLoot();
        }

        // Clean up dead menials and expired banners
        allMenials.RemoveAll(m => m == null || m.IsDead);
        activeBanners.RemoveAll(b => b == null);

        // Periodic audit: compare tracked count vs actual scene objects
        auditTimer -= Time.deltaTime;
        if (auditTimer <= 0f)
        {
            auditTimer = AUDIT_INTERVAL;
            AuditMenialCount();
        }
    }

    private void AuditMenialCount()
    {
        if (GameManager.Instance == null) return;

        var sceneMenials = FindObjectsByType<Menial>(FindObjectsSortMode.None);
        int aliveInScene = 0;
        int idleInScene = 0;
        int collectingInScene = 0;
        int returningInScene = 0;
        int fleeingInScene = 0;
        int enteringTowerInScene = 0;
        int otherInScene = 0;

        foreach (var m in sceneMenials)
        {
            if (m == null || m.IsDead) continue;
            aliveInScene++;
            switch (m.CurrentState)
            {
                case MenialState.Idle: idleInScene++; break;
                case MenialState.Collecting: collectingInScene++; break;
                case MenialState.Returning: returningInScene++; break;
                case MenialState.Fleeing: fleeingInScene++; break;
                case MenialState.EnteringTower: enteringTowerInScene++; break;
                default: otherInScene++; break;
            }
        }

        int trackedCount = GameManager.Instance.MenialCount;
        int trackedIdle = GameManager.Instance.IdleMenialCount;
        int listCount = allMenials.Count;

        bool mismatch = aliveInScene != trackedCount || idleInScene != trackedIdle;

        if (mismatch)
        {
            Debug.LogWarning($"[MenialManager] AUDIT MISMATCH! " +
                $"Tracked={trackedCount} (idle={trackedIdle}), " +
                $"Scene={aliveInScene} (idle={idleInScene}), " +
                $"List={listCount}. " +
                $"States: collect={collectingInScene} return={returningInScene} " +
                $"flee={fleeingInScene} tower={enteringTowerInScene} other={otherInScene}");
        }
    }

    private void TrySendMenialToLoot()
    {
        // Check if we're in wall placement mode
        var wallPlacement = FindAnyObjectByType<WallPlacement>();
        if (wallPlacement != null && wallPlacement.IsPlacing) return;

        if (mainCam == null) mainCam = UnityEngine.Camera.main;
        if (mainCam == null) return;

        // Raycast to find treasure
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (!groundPlane.Raycast(ray, out float distance)) return;

        Vector3 clickPos = ray.GetPoint(distance);

        var allPickups = FindObjectsByType<TreasurePickup>(FindObjectsSortMode.None);

        // Find nearest treasure pickup to click
        TreasurePickup nearestLoot = null;
        float nearestDist = lootSearchRadius;
        foreach (var pickup in allPickups)
        {
            if (pickup.IsCollected) continue;
            float dist = Vector3.Distance(clickPos, pickup.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestLoot = pickup;
            }
        }

        // Always show banner at click position so player sees the search radius
        SpawnBanner(clickPos);

        if (nearestLoot == null)
        {
            // No loot — try to find vegetation to clear instead
            TrySendMenialToVegetation(clickPos);
            return;
        }

        // Collect all uncollected loot within the search radius
        var lootInRadius = new List<TreasurePickup>();
        foreach (var pickup in allPickups)
        {
            if (pickup.IsCollected) continue;
            if (Vector3.Distance(clickPos, pickup.transform.position) <= lootSearchRadius)
                lootInRadius.Add(pickup);
        }

        Debug.Log($"[MenialManager] Found {lootInRadius.Count} loot in radius at {clickPos}");

        // Find nearest available menial to the click position
        Menial bestMenial = null;
        float bestMenialDist = float.MaxValue;
        foreach (var menial in allMenials)
        {
            if (menial == null || menial.IsDead) continue;
            if (!menial.IsAvailable) continue;
            float dist = Vector3.Distance(clickPos, menial.transform.position);
            if (dist < bestMenialDist)
            {
                bestMenialDist = dist;
                bestMenial = menial;
            }
        }

        if (bestMenial == null)
        {
            Debug.Log($"[MenialManager] No available menials ({allMenials.Count} total)");
            return;
        }

        // Assign the menial to the nearest loot piece in the radius
        TreasurePickup bestLoot = null;
        float bestLootDist = float.MaxValue;
        foreach (var loot in lootInRadius)
        {
            if (loot == null || loot.IsCollected) continue;
            float dist = Vector3.Distance(bestMenial.transform.position, loot.transform.position);
            if (dist < bestLootDist)
            {
                bestLootDist = dist;
                bestLoot = loot;
            }
        }

        if (bestLoot != null)
        {
            bestMenial.AssignLoot(bestLoot);
            Debug.Log($"[MenialManager] Assigned 1 menial to loot at {bestLoot.transform.position} ({lootInRadius.Count} loot pieces in radius)");
        }
    }

    private void TrySendMenialToVegetation(Vector3 clickPos)
    {
        // Check if any alive vegetation exists within the search radius
        var allVeg = FindObjectsByType<Vegetation>(FindObjectsSortMode.None);
        int vegCount = 0;

        foreach (var veg in allVeg)
        {
            if (veg == null || veg.IsDead) continue;
            if (Vector3.Distance(clickPos, veg.transform.position) <= lootSearchRadius)
                vegCount++;
        }

        if (vegCount == 0)
        {
            Debug.Log("[MenialManager] No loot or vegetation found near click position.");
            return;
        }

        Debug.Log($"[MenialManager] Found {vegCount} vegetation pieces in radius at {clickPos}");

        // Find nearest available menial to the click position
        Menial bestMenial = null;
        float bestDist = float.MaxValue;
        foreach (var menial in allMenials)
        {
            if (menial == null || menial.IsDead) continue;
            if (!menial.IsAvailable) continue;
            float dist = Vector3.Distance(clickPos, menial.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestMenial = menial;
            }
        }

        if (bestMenial != null)
        {
            bestMenial.AssignVegetationArea(clickPos, lootSearchRadius);
            Debug.Log($"[MenialManager] Assigned 1 menial to clear {vegCount} vegetation at {clickPos}");
        }
        else
        {
            Debug.Log($"[MenialManager] No available menials ({allMenials.Count} total)");
        }
    }

    public Menial SpawnMenial()
    {
        if (menialPrefab == null) return null;

        Vector3 spawnPos;
        if (menialSpawnPoint != null)
        {
            spawnPos = menialSpawnPoint.position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        }
        else
        {
            // Spawn outside the tower (radius ~1.5) but inside walls (wall ring at ~4.5)
            Vector3 fc = GameManager.FortressCenter;
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(3f, 4f);
            spawnPos = fc + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
        }

        // Find a valid NavMesh position near the spawn point (small radius to avoid snapping to tower center)
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 1.5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            // Reject positions too close to the tower center
            Vector3 fc2 = GameManager.FortressCenter;
            float hitDist = new Vector2(hit.position.x - fc2.x, hit.position.z - fc2.z).magnitude;
            if (hitDist >= 2f)
                spawnPos = hit.position;
            else
                Debug.LogWarning($"[MenialManager] NavMesh sample too close to tower ({hitDist:F1}m), using original spawn pos.");
        }

        var go = Instantiate(menialPrefab, spawnPos, Quaternion.identity, transform);
        var menial = go.GetComponent<Menial>();
        if (menial != null)
        {
            allMenials.Add(menial);
        }
        return menial;
    }

    public int GetIdleMenialCount()
    {
        int count = 0;
        foreach (var m in allMenials)
        {
            if (m != null && m.IsIdle && !m.IsDead) count++;
        }
        return count;
    }

    private void SpawnBanner(Vector3 position)
    {
        var root = new GameObject("LootBanner");
        root.transform.position = new Vector3(position.x, 0f, position.z);

        // Pole
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(pole.GetComponent<BoxCollider>());
        pole.transform.SetParent(root.transform, false);
        pole.transform.localPosition = new Vector3(0, 0.75f, 0);
        pole.transform.localScale = new Vector3(0.06f, 1.5f, 0.06f);
        pole.GetComponent<Renderer>().material.color = new Color(0.6f, 0.2f, 0.1f);

        // Flag
        var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(flag.GetComponent<BoxCollider>());
        flag.transform.SetParent(root.transform, false);
        flag.transform.localPosition = new Vector3(0.15f, 1.3f, 0);
        flag.transform.localScale = new Vector3(0.25f, 0.2f, 0.04f);
        flag.GetComponent<Renderer>().material.color = new Color(0.9f, 0.8f, 0.1f);

        // Radius circle using LineRenderer
        var circleObj = new GameObject("RadiusCircle");
        circleObj.transform.SetParent(root.transform, false);
        circleObj.transform.localPosition = new Vector3(0, 0.05f, 0);
        var lr = circleObj.AddComponent<LineRenderer>();
        int segments = 48;
        lr.positionCount = segments + 1;
        lr.loop = false;
        lr.useWorldSpace = false;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.9f, 0.2f, 0.7f);
        lr.endColor = new Color(1f, 0.9f, 0.2f, 0.7f);
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * lootSearchRadius, 0, Mathf.Sin(angle) * lootSearchRadius));
        }

        // Auto-destroy after 4 seconds
        var banner = root.AddComponent<LootBanner>();
        banner.lifetime = 4f;

        activeBanners.Add(root);
        Debug.Log($"[MenialManager] Banner placed at {position}, search radius={lootSearchRadius} ({activeBanners.Count} active)");
    }

    /// <summary>
    /// Send N idle menials to the tower. Calls onAllEntered once all have arrived.
    /// </summary>
    public bool ConsumeMenials(int count, System.Action onAllEntered)
    {
        var idle = new List<Menial>();
        foreach (var m in allMenials)
        {
            if (m != null && m.IsIdle && !m.IsDead)
                idle.Add(m);
            if (idle.Count >= count) break;
        }

        if (idle.Count < count)
        {
            Debug.LogWarning($"[MenialManager] Not enough idle menials! Need {count}, have {idle.Count}");
            return false;
        }

        int arrived = 0;
        foreach (var menial in idle)
        {
            menial.SendToTower(() =>
            {
                arrived++;
                Debug.Log($"[MenialManager] Menial entered tower ({arrived}/{count})");
                if (arrived >= count)
                {
                    onAllEntered?.Invoke();
                }
            });
        }
        return true;
    }
}
