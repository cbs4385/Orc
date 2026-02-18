using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenialManager : MonoBehaviour
{
    public static MenialManager Instance { get; private set; }

    [SerializeField] private GameObject menialPrefab;
    [SerializeField] private Transform menialSpawnPoint;

    private List<Menial> allMenials = new List<Menial>();
    private UnityEngine.Camera mainCam;

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

        // Clean up dead menials from list
        allMenials.RemoveAll(m => m == null || m.IsDead);
    }

    private void TrySendMenialToLoot()
    {
        // Check if we're in wall placement mode
        var wallPlacement = FindAnyObjectByType<WallPlacement>();
        if (wallPlacement != null && wallPlacement.IsPlacing) return;

        if (mainCam == null) { mainCam = UnityEngine.Camera.main; return; }

        // Raycast to find treasure
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0));
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (!groundPlane.Raycast(ray, out float distance)) return;

        Vector3 clickPos = ray.GetPoint(distance);

        var allPickups = FindObjectsByType<TreasurePickup>(FindObjectsSortMode.None);

        // Find nearest treasure pickup to click
        TreasurePickup nearestLoot = null;
        float nearestDist = 5f;
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

        if (nearestLoot == null)
        {
            Debug.Log("No loot found near click position");
            return;
        }

        Debug.Log($"Selected loot at {nearestLoot.transform.position}, value={nearestLoot.Value}");

        // Find nearest idle menial
        Menial bestMenial = null;
        float bestDist = float.MaxValue;
        int idleCount = 0;
        foreach (var menial in allMenials)
        {
            if (menial == null || menial.IsDead) continue;
            if (!menial.IsIdle) continue;
            idleCount++;
            float dist = Vector3.Distance(menial.transform.position, nearestLoot.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestMenial = menial;
            }
        }

        if (bestMenial != null)
        {
            Debug.Log($"[MenialManager] Assigned menial to loot at {nearestLoot.transform.position} (value={nearestLoot.Value}, {idleCount} idle)");
            bestMenial.AssignLoot(nearestLoot);
        }
        else
        {
            Debug.Log($"[MenialManager] No idle menials available ({allMenials.Count} total)");
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
            // Spawn outside the tower (tower radius ~1.5) but inside walls (wall ring at ~4)
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(2f, 3f);
            spawnPos = new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
        }

        // Find a valid NavMesh position near the spawn point
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPos = hit.position;
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
