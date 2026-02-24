using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerPositionManager : MonoBehaviour
{
    public static TowerPositionManager Instance { get; private set; }

    public class TowerPosition
    {
        public Vector3 WorldPos; // Y = 2.0 (top of tower)
        public List<Wall> Walls = new List<Wall>();
        public Defender Occupant;
    }

    private List<TowerPosition> towers = new List<TowerPosition>();

    private const float DEDUP_THRESHOLD = 0.5f; // XZ proximity for same tower
    private const float TOWER_TOP_Y = 1.85f;
    private const float MIN_TOWER_SPACING = 1.0f; // Minimum XZ distance between occupied towers

    public IReadOnlyList<TowerPosition> Towers => towers;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[TowerPositionManager] Instance registered.");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[TowerPositionManager] Instance re-registered after domain reload.");
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private IEnumerator Start()
    {
        // Wait one frame so WallManager.Start() registers all walls first
        yield return null;
        BuildPositions();
    }

    /// <summary>
    /// Iterate all walls, extract tower-end positions, deduplicate by XZ proximity.
    /// </summary>
    public void BuildPositions()
    {
        towers.Clear();

        if (WallManager.Instance == null)
        {
            Debug.LogWarning("[TowerPositionManager] WallManager not found. No towers built.");
            return;
        }

        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (wall == null) continue;
            var corners = wall.Corners;
            if (corners == null) continue;

            for (int sign = -1; sign <= 1; sign += 2)
            {
                Vector3 endCenter = corners.GetEndCenter(sign);
                endCenter.y = TOWER_TOP_Y;
                AddOrMergeTower(endCenter, wall);
            }

            // Subscribe to wall destroyed event
            wall.OnWallDestroyed -= HandleWallDestroyed;
            wall.OnWallDestroyed += HandleWallDestroyed;
        }

        Debug.Log($"[TowerPositionManager] Built {towers.Count} tower positions from {WallManager.Instance.AllWalls.Count} walls.");

        // After domain reload, defenders still hold old TowerPosition references.
        // Re-link any defender that thinks it's on a tower to the new tower list.
        foreach (var defender in Object.FindObjectsByType<Defender>(FindObjectsSortMode.None))
        {
            if (defender.IsDead) continue;
            if (defender.IsOnTower)
            {
                var nearest = FindNearestTower(defender.transform.position);
                if (nearest != null && nearest.Occupant == null)
                {
                    nearest.Occupant = defender;
                    defender.ReassignTower(nearest);
                    Debug.Log($"[TowerPositionManager] Re-linked {defender.name} to tower at {nearest.WorldPos} after rebuild.");
                }
            }
        }
    }

    /// <summary>
    /// Register a newly placed wall's tower positions (e.g. during build mode).
    /// </summary>
    public void RegisterNewWall(Wall wall)
    {
        if (wall == null) return;
        var corners = wall.Corners;
        if (corners == null) return;

        for (int sign = -1; sign <= 1; sign += 2)
        {
            Vector3 endCenter = corners.GetEndCenter(sign);
            endCenter.y = TOWER_TOP_Y;
            AddOrMergeTower(endCenter, wall);
        }

        wall.OnWallDestroyed -= HandleWallDestroyed;
        wall.OnWallDestroyed += HandleWallDestroyed;

        Debug.Log($"[TowerPositionManager] Registered new wall {wall.name}. Total towers: {towers.Count}");
    }

    private void AddOrMergeTower(Vector3 pos, Wall wall)
    {
        // Check if an existing tower is close enough in XZ
        foreach (var tower in towers)
        {
            float xzDist = Vector2.Distance(
                new Vector2(tower.WorldPos.x, tower.WorldPos.z),
                new Vector2(pos.x, pos.z));
            if (xzDist < DEDUP_THRESHOLD)
            {
                if (!tower.Walls.Contains(wall))
                    tower.Walls.Add(wall);
                return;
            }
        }

        // New tower position
        var newTower = new TowerPosition { WorldPos = pos };
        newTower.Walls.Add(wall);
        towers.Add(newTower);
    }

    /// <summary>
    /// Get the best unoccupied tower on an intact wall.
    /// When enemies exist, picks the tower whose nearest enemy is closest (best firing position).
    /// When no enemies, prefers towers far from other occupied towers (spread coverage).
    /// </summary>
    public TowerPosition GetBestTower(Vector3 defenderPos)
    {
        int enemyCount = Enemy.ActiveEnemies.Count;

        TowerPosition best = null;
        float bestScore = float.MaxValue;

        if (enemyCount > 0)
        {
            // Score each tower by distance to its nearest enemy
            // Lower = better (enemy is closer to this tower = better firing position)
            foreach (var tower in towers)
            {
                if (tower.Occupant != null) continue;
                if (!HasIntactWall(tower)) continue;
                if (HasNearbyOccupiedTower(tower)) continue;

                float nearestEnemyDist = float.MaxValue;
                Vector2 towerXZ = new Vector2(tower.WorldPos.x, tower.WorldPos.z);
                foreach (var enemy in Enemy.ActiveEnemies)
                {
                    if (enemy == null || enemy.IsDead) continue;
                    float d = Vector2.Distance(towerXZ,
                        new Vector2(enemy.transform.position.x, enemy.transform.position.z));
                    if (d < nearestEnemyDist) nearestEnemyDist = d;
                }

                // Small bias toward closer towers to avoid long walks
                float distToDefender = Vector3.Distance(defenderPos, tower.WorldPos);
                float score = nearestEnemyDist + distToDefender * 0.1f;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = tower;
                }
            }
        }
        else
        {
            // No enemies — spread out: pick tower farthest from other occupied towers
            foreach (var tower in towers)
            {
                if (tower.Occupant != null) continue;
                if (!HasIntactWall(tower)) continue;
                if (HasNearbyOccupiedTower(tower)) continue;

                float minDistToOccupied = float.MaxValue;
                foreach (var other in towers)
                {
                    if (other.Occupant == null || other == tower) continue;
                    float d = Vector2.Distance(
                        new Vector2(tower.WorldPos.x, tower.WorldPos.z),
                        new Vector2(other.WorldPos.x, other.WorldPos.z));
                    if (d < minDistToOccupied) minDistToOccupied = d;
                }

                // Invert: we want max distance from others → use negative as score
                // For first defender (no occupied towers), fall back to distance from defender
                float score = minDistToOccupied < float.MaxValue
                    ? -minDistToOccupied
                    : Vector3.Distance(defenderPos, tower.WorldPos);

                if (score < bestScore)
                {
                    bestScore = score;
                    best = tower;
                }
            }
        }

        return best;
    }

    /// <summary>
    /// Find the tower closest to a world position (used for re-linking after rebuild).
    /// </summary>
    private TowerPosition FindNearestTower(Vector3 pos)
    {
        TowerPosition nearest = null;
        float nearestDist = float.MaxValue;
        Vector2 posXZ = new Vector2(pos.x, pos.z);
        foreach (var tower in towers)
        {
            float d = Vector2.Distance(posXZ, new Vector2(tower.WorldPos.x, tower.WorldPos.z));
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = tower;
            }
        }
        return nearest;
    }

    /// <summary>
    /// Returns true if any nearby tower (within MIN_TOWER_SPACING) is occupied.
    /// Prevents two defenders from mounting adjacent towers and clipping into each other.
    /// </summary>
    private bool HasNearbyOccupiedTower(TowerPosition tower)
    {
        Vector2 towerXZ = new Vector2(tower.WorldPos.x, tower.WorldPos.z);
        foreach (var other in towers)
        {
            if (other == tower || other.Occupant == null) continue;
            float xzDist = Vector2.Distance(towerXZ,
                new Vector2(other.WorldPos.x, other.WorldPos.z));
            if (xzDist < MIN_TOWER_SPACING)
                return true;
        }
        return false;
    }

    private bool HasIntactWall(TowerPosition tower)
    {
        foreach (var wall in tower.Walls)
        {
            if (wall != null && !wall.IsDestroyed && !wall.IsUnderConstruction)
                return true;
        }
        return false;
    }

    public void Claim(TowerPosition tower, Defender defender)
    {
        if (tower == null) return;
        tower.Occupant = defender;
        Debug.Log($"[TowerPositionManager] {defender.name} claimed tower at {tower.WorldPos}");
    }

    public void Release(TowerPosition tower)
    {
        if (tower == null) return;
        Debug.Log($"[TowerPositionManager] Released tower at {tower.WorldPos} (was {(tower.Occupant != null ? tower.Occupant.name : "null")})");
        tower.Occupant = null;
    }

    private void HandleWallDestroyed(Wall wall)
    {
        // Check each tower — if ALL contributing walls are destroyed, force dismount
        foreach (var tower in towers)
        {
            if (!tower.Walls.Contains(wall)) continue;

            bool anyIntact = HasIntactWall(tower);
            if (!anyIntact && tower.Occupant != null)
            {
                Debug.Log($"[TowerPositionManager] All walls for tower at {tower.WorldPos} destroyed. Forcing {tower.Occupant.name} to dismount.");
                tower.Occupant.ForceDescend();
            }
        }
    }
}
