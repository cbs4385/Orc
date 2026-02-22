using System.Collections.Generic;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    public static WallManager Instance { get; private set; }

    [SerializeField] private GameObject wallPrefab;

    private List<Wall> allWalls = new List<Wall>();

    public IReadOnlyList<Wall> AllWalls => allWalls;
    public GameObject WallPrefab => wallPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[WallManager] Instance registered in Awake.");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[WallManager] Instance re-registered in OnEnable after domain reload.");
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        // Register any walls already in the scene
        foreach (var wall in FindObjectsByType<Wall>(FindObjectsSortMode.None))
        {
            RegisterWall(wall);
        }
    }

    public void RegisterWall(Wall wall)
    {
        if (!allWalls.Contains(wall))
        {
            allWalls.Add(wall);
            wall.OnWallDestroyed += HandleWallDestroyed;
            Debug.Log($"[WallManager] Registered wall {wall.name} at {wall.transform.position}. Total walls: {allWalls.Count}");
        }
    }

    private void HandleWallDestroyed(Wall wall)
    {
        // Check if breach allows enemies to reach tower
        // For now, any wall destroyed triggers a breach check
        CheckForBreach();
    }

    private void CheckForBreach()
    {
        // Count active walls on each side
        int destroyedCount = 0;
        int totalCount = allWalls.Count;
        foreach (var wall in allWalls)
        {
            if (wall.IsDestroyed) destroyedCount++;
        }

        if (destroyedCount > 0)
        {
            Debug.LogWarning($"[WallManager] BREACH DETECTED! {destroyedCount}/{totalCount} wall(s) destroyed. Enemies can path through gaps.");
            foreach (var wall in allWalls)
            {
                if (wall.IsDestroyed)
                    Debug.Log($"[WallManager] Breached wall: {wall.name} at {wall.transform.position}");
            }
        }
    }

    public Wall GetNearestWall(Vector3 position)
    {
        Wall nearest = null;
        float nearestDist = float.MaxValue;
        foreach (var wall in allWalls)
        {
            if (wall.IsDestroyed) continue;
            float dist = Vector3.Distance(position, wall.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = wall;
            }
        }
        return nearest;
    }

    public Wall GetNearestDamagedWall(Vector3 position)
    {
        return GetMostDamagedWall();
    }

    /// <summary>
    /// Returns the wall with the lowest HP percentage. Destroyed walls (breaches) always come first.
    /// </summary>
    public Wall GetMostDamagedWall()
    {
        Wall best = null;
        float bestHpPct = float.MaxValue;
        foreach (var wall in allWalls)
        {
            if (wall.CurrentHP >= wall.MaxHP) continue;
            // Destroyed walls get -1 so they always win
            float hpPct = wall.IsDestroyed ? -1f : (float)wall.CurrentHP / wall.MaxHP;
            if (hpPct < bestHpPct)
            {
                bestHpPct = hpPct;
                best = wall;
            }
        }
        return best;
    }

    public Vector3 GetNearestWallPosition(Vector3 fromPosition)
    {
        var wall = GetNearestWall(fromPosition);
        return wall != null ? wall.transform.position : GameManager.FortressCenter;
    }

    public bool HasBreach()
    {
        foreach (var wall in allWalls)
        {
            if (wall.IsDestroyed) return true;
        }
        return false;
    }

    public GameObject PlaceWall(Vector3 position, Quaternion rotation = default)
    {
        if (wallPrefab == null) return null;
        if (rotation == default) rotation = Quaternion.identity;
        var go = Instantiate(wallPrefab, position, rotation, transform);
        var wall = go.GetComponent<Wall>();
        if (wall != null)
        {
            RegisterWall(wall);
            wall.SetUnderConstruction();
        }
        Debug.Log($"[WallManager] Wall placed (under construction) at {position}, rotation={rotation.eulerAngles}.");
        return go;
    }
}
