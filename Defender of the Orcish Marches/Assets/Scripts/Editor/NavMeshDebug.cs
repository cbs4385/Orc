using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public static class NavMeshDebug
{
    [MenuItem("Tools/NavMesh/Debug Friendly NavMesh At Walls")]
    public static void DebugAtWalls()
    {
        // Check if the friendly NavMesh (agentTypeID=0) has walkable area at wall positions
        var walls = Object.FindObjectsByType<Wall>(FindObjectsSortMode.None);
        if (walls.Length == 0)
        {
            Debug.LogError("[NavMeshDebug] No walls found! Run in play mode or ensure walls exist in scene.");
            return;
        }

        int walkable = 0, blocked = 0;
        foreach (var wall in walls)
        {
            Vector3 wallPos = wall.transform.position;
            // Sample at ground level (Y=0) at the wall's XZ position
            Vector3 testPos = new Vector3(wallPos.x, 0, wallPos.z);

            NavMeshHit hit;
            bool hasNavMesh = NavMesh.SamplePosition(testPos, out hit, 0.5f, NavMesh.AllAreas);

            var collider = wall.GetComponent<BoxCollider>();
            string colliderInfo = collider != null
                ? $"enabled={collider.enabled}, size={collider.size}, center={collider.center}"
                : "NO COLLIDER";

            if (hasNavMesh)
            {
                walkable++;
                Debug.LogWarning($"[NavMeshDebug] WALKABLE at wall {wall.name} pos={testPos} hitPos={hit.position} dist={hit.distance:F2} — NavMesh should NOT be walkable here! Collider: {colliderInfo}");
            }
            else
            {
                blocked++;
                Debug.Log($"[NavMeshDebug] BLOCKED at wall {wall.name} pos={testPos} — correct, NavMesh has hole here. Collider: {colliderInfo}");
            }
        }

        Debug.Log($"[NavMeshDebug] Results: {blocked} blocked (correct), {walkable} walkable (WRONG — NavMesh didn't carve)");

        // Also test at wall gaps (between wall segments)
        Debug.Log("[NavMeshDebug] Testing gap positions:");
        // South wall: segments at x=-3.31, -1.10, 1.10, 3.31, z=-4.41
        // Gaps at x=-2.205, 0, 2.205 (midpoint between adjacent segments)
        float[] gapXPositions = { -2.205f, 0f, 2.205f };
        foreach (float gapX in gapXPositions)
        {
            Vector3 gapPos = new Vector3(gapX, 0, -4.41f);
            NavMeshHit gapHit;
            bool gapWalkable = NavMesh.SamplePosition(gapPos, out gapHit, 0.5f, NavMesh.AllAreas);
            Debug.Log($"[NavMeshDebug] Gap at {gapPos}: walkable={gapWalkable}" + (gapWalkable ? $" hitPos={gapHit.position}" : ""));
        }
    }
}
