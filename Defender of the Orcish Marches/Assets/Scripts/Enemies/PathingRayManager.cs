using UnityEngine;

/// <summary>
/// Casts 360 rays from the fortress center outward (one per degree) and calculates
/// the wall-crossing cost of each direction. Enemies use this to pick the cheapest
/// approach angle toward the central tower.
///
/// Wall destruction and repair trigger Recalculate() so ray costs stay current.
/// </summary>
public class PathingRayManager : MonoBehaviour
{
    public static PathingRayManager Instance { get; private set; }

    private const int RAY_COUNT = 360;
    private const float MAX_SAMPLE_DIST = 45f;  // Beyond map radius (40) to cover any wall placement
    /// <summary>Wall crossing count per ray direction (0 = clear path).</summary>
    private int[] rayCosts = new int[RAY_COUNT];

    /// <summary>Innermost wall hit on each ray (closest to tower — the first wall enemies must attack).</summary>
    private Wall[] rayTargetWall = new Wall[RAY_COUNT];

    /// <summary>Cached unit direction vector for each ray.</summary>
    private Vector3[] rayDirections = new Vector3[RAY_COUNT];

    /// <summary>Max cost across all rays (for gizmo color normalization).</summary>
    private int maxCost;

    // Layer mask for pathing cubes (cached on Awake)
    private int pathingLayerMask;

    // Shared buffer for wall deduplication during ray computation
    private static readonly Wall[] _wallBuffer = new Wall[32];

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        for (int i = 0; i < RAY_COUNT; i++)
        {
            float angle = i * Mathf.Deg2Rad;
            rayDirections[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        pathingLayerMask = LayerMask.GetMask("PathingRay");
        Debug.Log($"[PathingRayManager] Instance registered. pathingLayerMask={pathingLayerMask} (layer index={LayerMask.NameToLayer("PathingRay")})");
    }

    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[PathingRayManager] Instance re-registered in OnEnable.");
        }
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        Recalculate();
    }

    /// <summary>
    /// Recalculate all 360 ray costs. Call when walls are destroyed, repaired, or built.
    /// </summary>
    public void Recalculate()
    {
        maxCost = 0;
        Vector3 center = GameManager.FortressCenter;

        for (int i = 0; i < RAY_COUNT; i++)
        {
            ComputeRay(i, center);
            if (rayCosts[i] > maxCost) maxCost = rayCosts[i];
        }

        // Cost distribution summary
        int[] costBuckets = new int[maxCost + 1];
        int minCost = maxCost;
        for (int i = 0; i < RAY_COUNT; i++)
        {
            costBuckets[rayCosts[i]]++;
            if (rayCosts[i] < minCost) minCost = rayCosts[i];
        }
        string distStr = "";
        for (int c = 0; c <= maxCost; c++)
            distStr += $"cost{c}={costBuckets[c]} ";
        Debug.Log($"[PathingRayManager] Recalculated {RAY_COUNT} rays. Min={minCost}, Max={maxCost}. Distribution: {distStr.TrimEnd()}");

        // Log cardinal + ordinal sample rays with per-wall hit details
        int[] sampleAngles = { 0, 45, 90, 135, 180, 225, 270, 315 };
        string[] sampleNames = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };
        for (int s = 0; s < sampleAngles.Length; s++)
        {
            int idx = sampleAngles[s];
            string wallInfo = "none";
            if (rayTargetWall[idx] != null)
                wallInfo = $"{rayTargetWall[idx].name} at {rayTargetWall[idx].transform.position}";
            Debug.Log($"[PathingRayManager] Sample ray {idx}° ({sampleNames[s]}): cost={rayCosts[idx]}, innermost={wallInfo}");
        }
    }

    /// <summary>
    /// Analytical 2D ray-segment intersection — no physics involved.
    /// Each wall defines a line segment from left tower center to right tower center (in XZ).
    /// The ray from fortress center tests intersection with every active wall segment.
    /// Uses half-open interval [0, 1) on the segment parameter so shared tower endpoints
    /// are counted by exactly one wall (the one whose LEFT tower is at that point).
    /// Cost = number of unique wall segments the ray crosses.
    /// </summary>
    private void ComputeRay(int index, Vector3 center)
    {
        Vector2 origin2D = new Vector2(center.x, center.z);
        Vector2 dir2D = new Vector2(rayDirections[index].x, rayDirections[index].z);

        int wallCount = 0;
        Wall innermost = null;
        float innermostDist = float.MaxValue;

        if (WallManager.Instance == null) { rayCosts[index] = 0; rayTargetWall[index] = null; return; }

        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (wall.IsDestroyed || wall.IsUnderConstruction) continue;
            if (!wall.gameObject.activeInHierarchy) continue;

            // Wall segment: left tower center → right tower center (in XZ plane)
            Vector3 wallRight3D = wall.transform.right;
            Vector2 wallCenter = new Vector2(wall.transform.position.x, wall.transform.position.z);
            float offset = WallCorners.TOWER_OFFSET;
            Vector2 P = wallCenter - new Vector2(wallRight3D.x, wallRight3D.z) * offset; // left tower
            Vector2 Q = wallCenter + new Vector2(wallRight3D.x, wallRight3D.z) * offset; // right tower

            // 2D ray-segment intersection via cross products
            Vector2 S = Q - P;                // segment direction
            float denom = dir2D.x * S.y - dir2D.y * S.x;  // cross(dir, S)
            if (Mathf.Abs(denom) < 1e-6f) continue;        // parallel — no crossing

            Vector2 OP = P - origin2D;
            float t = (OP.x * S.y - OP.y * S.x) / denom;  // ray parameter
            float u = (OP.x * dir2D.y - OP.y * dir2D.x) / denom;  // segment parameter

            // t > 0: forward along ray; u in [0, 1): on segment (half-open avoids shared-tower double count)
            if (t > 0f && u >= 0f && u < 1f)
            {
                if (wallCount >= _wallBuffer.Length) break;
                _wallBuffer[wallCount] = wall;
                wallCount++;

                float dist = Vector3.Distance(
                    new Vector3(wall.transform.position.x, 0f, wall.transform.position.z),
                    new Vector3(center.x, 0f, center.z));
                if (dist < innermostDist)
                {
                    innermostDist = dist;
                    innermost = wall;
                }
            }
        }

        rayCosts[index] = wallCount;
        rayTargetWall[index] = innermost;

        // Detailed per-wall hit log for cardinal + ordinal rays
        if (index % 45 == 0 && wallCount > 0)
        {
            string hitList = "";
            for (int w = 0; w < wallCount; w++)
            {
                Wall ww = _wallBuffer[w];
                float d = Vector3.Distance(
                    new Vector3(ww.transform.position.x, 0f, ww.transform.position.z),
                    new Vector3(center.x, 0f, center.z));
                hitList += $"  {ww.name} pos={ww.transform.position} rot={ww.transform.eulerAngles.y:F0}° dist={d:F2}\n";
            }
            Debug.Log($"[PathingRayManager] Ray {index}° detail: {wallCount} walls hit:\n{hitList}");
        }
    }

    /// <summary>
    /// Maximum detour angle (degrees) enemies will scan for a cheaper approach.
    /// Picks the lowest-cost ray within this window; ties broken by smallest detour.
    /// </summary>
    private const int CHEAPEST_SEARCH_ANGLE = 60;

    /// <summary>
    /// Get the best ray for an enemy at the given position.
    /// Scans ±CHEAPEST_SEARCH_ANGLE from the direct approach ray and picks the
    /// cheapest direction. Among equal-cost rays the closest to direct wins.
    /// The returned wall is the INNERMOST wall on that ray (first barrier to break).
    /// </summary>
    public (int rayIndex, int cost, Wall firstWall) GetBestRay(Vector3 enemyPos)
    {
        Vector3 toEnemy = enemyPos - GameManager.FortressCenter;
        float enemyAngle = Mathf.Atan2(toEnemy.z, toEnemy.x) * Mathf.Rad2Deg;
        if (enemyAngle < 0f) enemyAngle += 360f;

        int directRay = Mathf.RoundToInt(enemyAngle) % RAY_COUNT;

        // Start with the direct approach ray
        int bestRay = directRay;
        int bestCost = rayCosts[directRay];
        float bestAngleDiff = 0f;

        // Scan ±CHEAPEST_SEARCH_ANGLE for a cheaper ray
        for (int offset = -CHEAPEST_SEARCH_ANGLE; offset <= CHEAPEST_SEARCH_ANGLE; offset++)
        {
            int i = ((directRay + offset) % RAY_COUNT + RAY_COUNT) % RAY_COUNT;
            int cost = rayCosts[i];
            float angleDiff = Mathf.Abs(offset);

            // Better if cheaper, or same cost but closer to direct approach
            if (cost < bestCost || (cost == bestCost && angleDiff < bestAngleDiff))
            {
                bestCost = cost;
                bestRay = i;
                bestAngleDiff = angleDiff;
            }
        }

        Wall bestWall = bestRay >= 0 ? rayTargetWall[bestRay] : null;

        // Diagnostic logging
        int directCost = rayCosts[directRay];
        string directWallName = rayTargetWall[directRay] != null ? rayTargetWall[directRay].name : "none";
        string bestWallName = bestWall != null ? $"{bestWall.name} at {bestWall.transform.position}" : "none";

        string neighborhood = "";
        for (int offset = -5; offset <= 5; offset++)
        {
            int ri = ((directRay + offset) % RAY_COUNT + RAY_COUNT) % RAY_COUNT;
            neighborhood += $"{ri}°={rayCosts[ri]} ";
        }

        string decision = bestRay == directRay
            ? "DIRECT"
            : $"DETOUR to {bestRay}° (offset={bestAngleDiff:F0}°, cost {directCost}→{bestCost})";
        Debug.Log($"[PathingRayManager] GetBestRay: enemyPos={enemyPos}, enemyAngle={enemyAngle:F1}°. " +
            $"Direct ray {directRay}°: cost={directCost} wall={directWallName}. " +
            $"Decision: {decision}, cost={bestCost} wall={bestWallName}. " +
            $"Nearby costs: [{neighborhood.TrimEnd()}]");

        return (bestRay, bestCost, bestWall);
    }

    /// <summary>Get the world-space direction of a ray by index.</summary>
    public Vector3 GetRayDirection(int rayIndex)
    {
        return rayDirections[rayIndex];
    }

    /// <summary>Get a point on the given ray at the specified distance from fortress center.</summary>
    public Vector3 GetPointOnRay(int rayIndex, float distanceFromCenter)
    {
        return GameManager.FortressCenter + rayDirections[rayIndex] * distanceFromCenter;
    }

    private void OnDrawGizmos()
    {
        if (rayCosts == null || rayDirections == null) return;

        Vector3 origin = GameManager.FortressCenter + Vector3.up * 0.3f;
        float displayLength = MAX_SAMPLE_DIST;

        for (int i = 0; i < RAY_COUNT; i++)
        {
            int cost = rayCosts[i];

            Color color;
            if (cost == 0)
                color = Color.green;
            else if (cost == 1)
                color = Color.yellow;
            else
                color = Color.red;

            color.a = 0.35f;
            Gizmos.color = color;

            Vector3 endPoint = origin + rayDirections[i] * displayLength;
            Gizmos.DrawLine(origin, endPoint);
        }
    }
}
