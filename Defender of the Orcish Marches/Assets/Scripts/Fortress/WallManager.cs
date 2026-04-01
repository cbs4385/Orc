using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class WallManager : MonoBehaviour
{
    public static WallManager Instance { get; private set; }

    [SerializeField] private GameObject wallPrefab;

    [Header("Debug — pre-built extension walls for testing")]
    [SerializeField] private bool debugPrebuiltWestWalls;
    [Tooltip("Recreates the issue 6 layout: 8 extension walls on west side with NW/SW corner angles")]
    [SerializeField] private bool debugIssue6WestExtensions;

    private List<Wall> allWalls = new List<Wall>();
    private bool enemyNavMeshDirty;

    public static event System.Action OnWallBuilt;

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

    private void LateUpdate()
    {
        if (enemyNavMeshDirty)
        {
            enemyNavMeshDirty = false;
            DoRebakeEnemyNavMesh();
        }
    }

    private void Start()
    {
        // Register any walls already in the scene
        foreach (var wall in FindObjectsByType<Wall>(FindObjectsSortMode.None))
        {
            RegisterWall(wall);
        }

        if (debugPrebuiltWestWalls)
            PlaceDebugWestExtensions();
        if (debugIssue6WestExtensions)
            PlaceDebugIssue6Extensions();

        // Ensure PathingRayManager exists and compute initial ray costs
        if (PathingRayManager.Instance == null)
        {
            var go = new GameObject("[PathingRayManager]");
            go.AddComponent<PathingRayManager>();
            Debug.Log("[WallManager] Auto-created PathingRayManager.");
        }
        else
        {
            PathingRayManager.Instance.Recalculate();
        }

        // Rebake enemy NavMesh to include tower colliders created at runtime in Wall.Awake()
        RebakeEnemyNavMesh();

        // Rebake friendly NavMesh with walls included so menials/refugees path through gaps
        RebakeFriendlyNavMesh();

        // Log initial wall state for bug report reproduction
        LogAllWallState();
    }

    /// <summary>
    /// Debug helper: places 4 extension walls forming a second layer on the west side,
    /// instantly completed (full HP, colliders enabled). Toggle via inspector checkbox.
    /// </summary>
    private void PlaceDebugWestExtensions()
    {
        var positions = new (Vector3 pos, float yRot)[]
        {
            (new Vector3(-5.52f, 1f, 2.21f),  180f),
            (new Vector3(-5.52f, 1f, -2.21f), 180f),
            (new Vector3(-6.62f, 1f, -1.10f), 270f),
            (new Vector3(-6.62f, 1f, 1.10f),  270f),
        };

        foreach (var (pos, yRot) in positions)
        {
            var go = PlaceWall(pos, Quaternion.Euler(0, yRot, 0));
            if (go != null)
            {
                var wall = go.GetComponent<Wall>();
                if (wall != null)
                    wall.Repair(wall.MaxHP); // Instantly complete construction
            }
        }
        Debug.Log($"[WallManager] Debug: placed {positions.Length} pre-built west extension walls");
    }

    /// <summary>
    /// Debug helper: recreates the issue 6 wall layout — 8 extension walls on the west side
    /// with NW and SW corner angles. Positions captured from tester's scene.
    /// </summary>
    private void PlaceDebugIssue6Extensions()
    {
        var positions = new (Vector3 pos, float yRot)[]
        {
            // 4 vertical west extension walls at x≈-8.1
            (new Vector3(-8.12f, 1f, -3.63f),  89f),   // West south
            (new Vector3(-8.12f, 1f, -1.31f),  89f),   // West south-center
            (new Vector3(-8.27f, 1f,  0.89f),  87f),   // West center
            (new Vector3(-8.19f, 1f,  3.14f),  90f),   // West north
            // 2 SW corner angles
            (new Vector3(-5.37f, 1f, -5.41f), 315f),   // SW inner corner
            (new Vector3(-7.11f, 1f, -5.44f),  38f),   // SW outer corner
            // 2 NW corner angles
            (new Vector3(-7.43f, 1f,  5.26f), 129f),   // NW outer corner
            (new Vector3(-5.87f, 1f,  7.00f), 313f),   // NW inner corner
        };

        foreach (var (pos, yRot) in positions)
        {
            var go = PlaceWall(pos, Quaternion.Euler(0, yRot, 0));
            if (go != null)
            {
                var wall = go.GetComponent<Wall>();
                if (wall != null)
                    wall.Repair(wall.MaxHP); // Instantly complete construction
            }
        }
        Debug.Log($"[WallManager] Debug: placed {positions.Length} issue 6 west extension walls");
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

    /// <summary>
    /// Logs positions, rotations, scale, and HP of all walls. Output is parseable for game state recreation.
    /// </summary>
    public void LogAllWallState()
    {
        Debug.Log($"[WallManager] === WALL STATE ({allWalls.Count} walls) ===");
        foreach (var wall in allWalls)
        {
            if (wall == null) continue;
            var t = wall.transform;
            var rot = t.rotation.eulerAngles;
            var scale = t.localScale;
            Debug.Log($"[WallManager] WALL: name={wall.name} pos=({t.position.x:F4},{t.position.y:F4},{t.position.z:F4}) rot=({rot.x:F1},{rot.y:F1},{rot.z:F1}) scaleX={scale.x:F3} hp={wall.CurrentHP}/{wall.MaxHP} destroyed={wall.IsDestroyed}");
        }
        Debug.Log("[WallManager] === END WALL STATE ===");
    }

    /// <summary>
    /// Marks the enemy NavMesh as dirty. The actual rebuild runs once in LateUpdate,
    /// batching multiple wall changes into a single NavMesh rebuild per frame.
    /// </summary>
    public void RebakeEnemyNavMesh()
    {
        enemyNavMeshDirty = true;
    }

    private void DoRebakeEnemyNavMesh()
    {
        // Temporarily disable tower colliders adjacent to breaches so the NavMesh
        // gap is wide enough for enemies to walk through destroyed wall positions.
        var disabledTowers = DisableBreachAdjacentTowerColliders();

        bool found = false;
        var surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        foreach (var surface in surfaces)
        {
            if (surface.agentTypeID == -334000983) // Enemy agent type
            {
                // Ensure PhysicsColliders mode — RenderMeshes fails at runtime
                // ("Source mesh does not allow read access")
                if (surface.useGeometry != NavMeshCollectGeometry.PhysicsColliders)
                {
                    Debug.LogWarning("[WallManager] Enemy NavMeshSurface was using RenderMeshes — switching to PhysicsColliders for runtime safety.");
                    surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                }
                surface.BuildNavMesh();
                Debug.Log($"[WallManager] Rebaked enemy NavMesh (agentTypeID={surface.agentTypeID})");
                found = true;
                break;
            }
        }

        // Re-enable tower colliders (they only need to be off during the bake)
        foreach (var go in disabledTowers)
            go.SetActive(true);

        if (!found)
            Debug.LogWarning("[WallManager] No enemy NavMeshSurface found for rebake");

        // Also rebake the friendly NavMesh (agentTypeID=0) so menials/refugees
        // path through wall gaps instead of through wall geometry.
        RebakeFriendlyNavMesh();
    }

    private void RebakeFriendlyNavMesh()
    {
        // Disable ALL tower colliders during the friendly bake.
        // Confirmed via gap walkability test: all 12 gaps are BLOCKED because
        // tower CapsuleColliders (radius ~0.6, TOWER_OFFSET=1.1) overlap between
        // adjacent wall segments, closing every gap.
        var disabledTowers = new List<GameObject>();
        foreach (var wall in allWalls)
        {
            if (wall == null) continue;
            foreach (string towerName in new[] { "TowerCollider_L", "TowerCollider_R" })
            {
                var tower = wall.transform.Find(towerName);
                if (tower != null && tower.gameObject.activeSelf)
                {
                    tower.gameObject.SetActive(false);
                    disabledTowers.Add(tower.gameObject);
                }
            }
        }

        var surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        int count = 0;
        foreach (var surface in surfaces)
        {
            if (surface.agentTypeID == 0) // Humanoid / friendly agent type
            {
                // Include ALL layers (including wall layer 9).
                // Ignore NavMeshObstacles — use only physics colliders to define wall boundaries.
                // With ignoreNavMeshObstacle=false, both colliders AND obstacles contribute,
                // double-blocking the gaps. Using colliders only preserves the physical gap width.
                surface.layerMask = -1;
                surface.ignoreNavMeshObstacle = true;

                if (surface.useGeometry != NavMeshCollectGeometry.PhysicsColliders)
                    surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

                surface.BuildNavMesh();
                count++;

                // Verify: test multiple points across each wall to check NavMesh blocking
                int walkablePoints = 0, blockedPoints = 0;
                var testResults = new System.Text.StringBuilder();
                foreach (var wall in allWalls)
                {
                    if (wall == null) continue;
                    Vector3 center = wall.transform.position;
                    Vector3 right = wall.transform.right;
                    Vector3 fwd = wall.transform.forward;
                    Vector3[] testOffsets = {
                        Vector3.zero,
                        right * 0.4f,
                        -right * 0.4f,
                        fwd * 0.2f,
                        -fwd * 0.2f
                    };
                    foreach (var offset in testOffsets)
                    {
                        Vector3 testPos = new Vector3(center.x + offset.x, 0, center.z + offset.z);
                        UnityEngine.AI.NavMeshHit hit;
                        if (UnityEngine.AI.NavMesh.SamplePosition(testPos, out hit, 0.3f, UnityEngine.AI.NavMesh.AllAreas))
                        {
                            walkablePoints++;
                            testResults.Append($"\n  WALKABLE: {wall.name} offset=({offset.x:F2},0,{offset.z:F2}) testPos=({testPos.x:F2},0,{testPos.z:F2}) hitPos={hit.position}");
                        }
                        else
                        {
                            blockedPoints++;
                        }
                    }
                }
                Debug.Log($"[WallManager] Rebaked friendly NavMesh on '{surface.gameObject.name}' (layerMask={surface.layerMask.value}, ignoreObstacles={surface.ignoreNavMeshObstacle}). Points: {blockedPoints} blocked, {walkablePoints} WALKABLE");
                if (walkablePoints > 0)
                    Debug.LogWarning($"[WallManager] WALKABLE points found at walls:{testResults}");
                // Don't break — rebake ALL friendly surfaces
            }
        }
        // Re-enable tower colliders after bake
        foreach (var go in disabledTowers)
            if (go != null) go.SetActive(true);
        Debug.Log($"[WallManager] Re-enabled {disabledTowers.Count} tower colliders after friendly bake.");

        // Test gap walkability
        TestGapWalkability();

        Debug.Log($"[WallManager] Rebaked {count} friendly NavMesh surface(s).");
    }

    private void TestGapWalkability()
    {
        // Test at the midpoint of each gap between adjacent wall segments on each side
        // South side: walls at z=-4.41, segments at x=-3.31, -1.10, 1.10, 3.31
        // Gaps at x=-2.2, 0.0, 2.2 (midpoints between segments)
        float[][] sideGaps = {
            new float[] { -4.41f },  // South Z
            new float[] { 4.41f },   // North Z
        };
        float[] gapXPositions = { -2.2f, 0f, 2.2f };

        var sb = new System.Text.StringBuilder();
        sb.Append("[WallManager] Gap walkability test:");
        int walkable = 0, blocked = 0;

        // Test N/S gaps at multiple Z offsets through the wall depth
        // Wall BoxCollider is 0.5 deep centered at wall Z. Test outside, at wall, and inside.
        foreach (float wallZ in new[] { -4.41f, 4.41f })
        {
            string side = wallZ < 0 ? "S" : "N";
            float sign = wallZ < 0 ? -1f : 1f;
            foreach (float x in gapXPositions)
            {
                // Test at 3 Z positions: outside wall, at wall center, inside wall
                foreach (float zOffset in new[] { 0.5f, 0f, -0.5f })
                {
                    Vector3 pos = new Vector3(x, 0, wallZ + sign * zOffset);
                    UnityEngine.AI.NavMeshHit hit;
                    bool w = UnityEngine.AI.NavMesh.SamplePosition(pos, out hit, 0.3f, UnityEngine.AI.NavMesh.AllAreas);
                    sb.Append($"\n  {side} gap x={x:F1} z={pos.z:F2}: {(w ? "WALKABLE" : "BLOCKED")}");
                    if (w) walkable++; else blocked++;
                }
            }
        }
        // Test E/W gaps at multiple X offsets
        foreach (float wallX in new[] { -4.41f, 4.41f })
        {
            string side = wallX < 0 ? "W" : "E";
            float sign = wallX < 0 ? -1f : 1f;
            float[] gapZPositions = { -2.2f, 0f, 2.2f };
            foreach (float z in gapZPositions)
            {
                foreach (float xOffset in new[] { 0.5f, 0f, -0.5f })
                {
                    Vector3 pos = new Vector3(wallX + sign * xOffset, 0, z);
                    UnityEngine.AI.NavMeshHit hit;
                    bool w = UnityEngine.AI.NavMesh.SamplePosition(pos, out hit, 0.3f, UnityEngine.AI.NavMesh.AllAreas);
                    sb.Append($"\n  {side} gap z={z:F1} x={pos.x:F2}: {(w ? "WALKABLE" : "BLOCKED")}");
                    if (w) walkable++; else blocked++;
                }
            }
        }
        sb.Append($"\n  TOTAL: {walkable} walkable, {blocked} blocked");

        // Path connectivity test: can we compute a path from outside each wall side to center?
        // Use NavMeshQueryFilter with Humanoid agent type (agentTypeID=0) to avoid ambiguity
        // when multiple NavMeshSurfaces exist for different agent types.
        int cardinalConnected = 0, cardinalDisconnected = 0;
        int cornerConnected = 0, cornerDisconnected = 0;
        Vector3 center = GameManager.FortressCenter;

        var filter = new UnityEngine.AI.NavMeshQueryFilter();
        filter.agentTypeID = 0; // Humanoid
        filter.areaMask = UnityEngine.AI.NavMesh.AllAreas;

        // Cardinal paths — test from outside the wall ring. NavMeshSurface bake volume is 100×100.
        // Use SamplePosition to snap test points to the nearest navmesh point before pathing,
        // since raw world positions may not sit on the Humanoid navmesh surface.
        Vector3[] cardinalPositions = {
            new Vector3(0, 0, -6f),   // south
            new Vector3(0, 0, 6f),    // north
            new Vector3(-6f, 0, 0),   // west
            new Vector3(6f, 0, 0),    // east
        };
        foreach (var outsidePos in cardinalPositions)
        {
            string label = $"({outsidePos.x:F0},{outsidePos.z:F0})";
            UnityEngine.AI.NavMeshHit startHit;
            if (!UnityEngine.AI.NavMesh.SamplePosition(outsidePos, out startHit, 2f, filter.areaMask))
            {
                sb.Append($"\n  PATH {label}->center: NO NAVMESH within 2m of start point");
                cardinalDisconnected++;
                continue;
            }
            UnityEngine.AI.NavMeshHit endHit;
            if (!UnityEngine.AI.NavMesh.SamplePosition(center, out endHit, 2f, filter.areaMask))
            {
                sb.Append($"\n  PATH {label}->center: NO NAVMESH within 2m of center");
                cardinalDisconnected++;
                continue;
            }
            var path = new UnityEngine.AI.NavMeshPath();
            UnityEngine.AI.NavMesh.CalculatePath(startHit.position, endHit.position, filter, path);
            sb.Append($"\n  PATH {label}->center: status={path.status}, corners={path.corners.Length} (snapped from {outsidePos} to {startHit.position})");
            if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete) cardinalConnected++;
            else cardinalDisconnected++;
        }

        // Corner paths
        Vector3[] cornerPositions = {
            new Vector3(5f, 0, -5f),  // SE
            new Vector3(-5f, 0, 5f),  // NW
            new Vector3(5f, 0, 5f),   // NE
            new Vector3(-5f, 0, -5f), // SW
        };
        foreach (var outsidePos in cornerPositions)
        {
            string label = $"({outsidePos.x:F0},{outsidePos.z:F0})";
            UnityEngine.AI.NavMeshHit startHit;
            if (!UnityEngine.AI.NavMesh.SamplePosition(outsidePos, out startHit, 2f, filter.areaMask))
            {
                sb.Append($"\n  CORNER {label}->center: NO NAVMESH within 2m of start point");
                cornerDisconnected++;
                continue;
            }
            UnityEngine.AI.NavMeshHit endHit;
            if (!UnityEngine.AI.NavMesh.SamplePosition(center, out endHit, 2f, filter.areaMask))
            {
                sb.Append($"\n  CORNER {label}->center: NO NAVMESH within 2m of center");
                cornerDisconnected++;
                continue;
            }
            var path = new UnityEngine.AI.NavMeshPath();
            UnityEngine.AI.NavMesh.CalculatePath(startHit.position, endHit.position, filter, path);
            sb.Append($"\n  CORNER {label}->center: status={path.status}, corners={path.corners.Length} (snapped from {outsidePos} to {startHit.position})");
            if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete) cornerConnected++;
            else cornerDisconnected++;
        }

        int totalDisconnected = cardinalDisconnected + cornerDisconnected;
        sb.Append($"\n  CARDINAL: {cardinalConnected} connected, {cardinalDisconnected} disconnected");
        sb.Append($"\n  CORNERS: {cornerConnected} connected, {cornerDisconnected} disconnected");

        Debug.Log(sb.ToString());
        if (totalDisconnected > 0)
            Debug.LogError($"[WallManager] {totalDisconnected} approach paths are DISCONNECTED — refugees cannot reach fortress center!");
    }

    /// <summary>
    /// Immediately rebake enemy NavMesh (widening breach gaps) and retarget all enemies.
    /// Call this when a wall is destroyed or construction completes so enemies get
    /// fresh NavMesh data before choosing their next destination.
    /// </summary>
    public void RebakeImmediateAndRetarget()
    {
        enemyNavMeshDirty = false; // Cancel any pending deferred rebake
        DoRebakeEnemyNavMesh();
        EnemyMovement.ForceAllRetarget();
    }

    /// <summary>
    /// When a wall is destroyed, its adjacent walls still have tower CapsuleColliders at
    /// the shared endpoints. These narrow the breach gap to ~1.0 unit, which is too tight
    /// for the enemy NavMesh agent to path through. This method temporarily disables those
    /// tower colliders so the NavMesh bake produces a full-width gap at each breach.
    /// Returns the list of disabled GameObjects that must be re-enabled after the bake.
    /// </summary>
    private List<GameObject> DisableBreachAdjacentTowerColliders()
    {
        var disabled = new List<GameObject>();

        // Collect XZ positions of all breach walls' tower endpoints
        var breachTowerXZ = new List<Vector2>();
        foreach (var wall in allWalls)
        {
            if (wall == null) continue;
            // A wall is a breach if destroyed or inactive
            if (!wall.IsDestroyed && wall.gameObject.activeInHierarchy) continue;

            Vector3 right = wall.transform.right;
            Vector3 pos = wall.transform.position;
            float offset = WallCorners.TOWER_OFFSET;
            Vector3 leftTower = pos - right * offset;
            Vector3 rightTower = pos + right * offset;
            breachTowerXZ.Add(new Vector2(leftTower.x, leftTower.z));
            breachTowerXZ.Add(new Vector2(rightTower.x, rightTower.z));
        }

        if (breachTowerXZ.Count == 0) return disabled;

        // Disable matching tower colliders on active walls
        foreach (var wall in allWalls)
        {
            if (wall == null || wall.IsDestroyed || !wall.gameObject.activeInHierarchy) continue;

            DisableTowerIfAtBreach(wall.transform, "TowerCollider_L", breachTowerXZ, disabled);
            DisableTowerIfAtBreach(wall.transform, "TowerCollider_R", breachTowerXZ, disabled);
        }

        if (disabled.Count > 0)
            Debug.Log($"[WallManager] Temporarily disabled {disabled.Count} breach-adjacent tower collider(s) for NavMesh rebake");

        return disabled;
    }

    private void DisableTowerIfAtBreach(Transform wallTransform, string towerName,
        List<Vector2> breachPositions, List<GameObject> disabledList)
    {
        var tower = wallTransform.Find(towerName);
        if (tower == null) return;

        Vector2 towerXZ = new Vector2(tower.position.x, tower.position.z);
        foreach (var bp in breachPositions)
        {
            if (Vector2.Distance(towerXZ, bp) < 0.5f)
            {
                tower.gameObject.SetActive(false);
                disabledList.Add(tower.gameObject);
                Debug.Log($"[WallManager] Disabled {towerName} on {wallTransform.name} at breach endpoint ({bp.x:F2}, {bp.y:F2})");
                break;
            }
        }
    }

    public GameObject PlaceWall(Vector3 position, Quaternion rotation = default, float scaleX = 1f)
    {
        if (wallPrefab == null) return null;
        if (rotation == default) rotation = Quaternion.identity;
        var go = Instantiate(wallPrefab, position, rotation, transform);
        if (scaleX != 1f)
            go.transform.localScale = new Vector3(scaleX, 1f, 1f);
        var wall = go.GetComponent<Wall>();
        if (wall != null)
        {
            RegisterWall(wall);
            wall.SetUnderConstruction();

            // Register tower positions for the new wall
            if (TowerPositionManager.Instance != null)
                TowerPositionManager.Instance.RegisterNewWall(wall);
        }
        Debug.Log($"[WallManager] Wall placed (under construction) at {position}, rotation={rotation.eulerAngles}, scaleX={scaleX:F3}.");
        OnWallBuilt?.Invoke();
        return go;
    }

    /// <summary>Place a wall with specific HP (for save/load restore). Skips construction state if HP > 0.</summary>
    public GameObject PlaceWallWithHP(Vector3 position, Quaternion rotation, float scaleX, int currentHP, int maxHP, bool isUnderConstruction)
    {
        if (wallPrefab == null) return null;
        var go = Instantiate(wallPrefab, position, rotation, transform);
        if (scaleX != 1f)
            go.transform.localScale = new Vector3(scaleX, 1f, 1f);
        var wall = go.GetComponent<Wall>();
        if (wall != null)
        {
            RegisterWall(wall);
            if (isUnderConstruction)
            {
                wall.SetUnderConstruction();
                // Partially repair to the saved HP
                if (currentHP > 0)
                    wall.Repair(currentHP);
            }
            else
            {
                // Complete wall — set to full HP first via Repair, then reduce to saved HP
                wall.RestoreHP(currentHP, maxHP);
            }

            if (TowerPositionManager.Instance != null)
                TowerPositionManager.Instance.RegisterNewWall(wall);
        }
        Debug.Log($"[WallManager] Restored wall at {position}, HP={currentHP}/{maxHP}, construction={isUnderConstruction}");
        return go;
    }

    /// <summary>Destroy all current walls (for save restore — clear defaults before placing saved walls).</summary>
    public void DestroyAllWalls()
    {
        for (int i = allWalls.Count - 1; i >= 0; i--)
        {
            if (allWalls[i] != null)
                Destroy(allWalls[i].gameObject);
        }
        allWalls.Clear();
        Debug.Log("[WallManager] All walls destroyed for save restore.");
    }
}
