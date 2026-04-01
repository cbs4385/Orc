using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public static class RebakeFriendlyNavMesh
{
    [MenuItem("Tools/NavMesh/Rebake Friendly NavMesh (Include Walls)")]
    public static void Rebake()
    {
        var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        foreach (var surface in surfaces)
        {
            if (surface.agentTypeID == 0) // Humanoid / friendly
            {
                // Include ALL layers (including layer 9 = Walls)
                surface.layerMask = -1;
                // Include NavMeshObstacles so wall obstacles carve properly
                surface.ignoreNavMeshObstacle = false;
                // Use physics colliders for runtime safety
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

                surface.BuildNavMesh();

                Debug.Log($"[RebakeFriendlyNavMesh] Rebaked friendly NavMesh: layerMask={surface.layerMask.value}, ignoreObstacles={surface.ignoreNavMeshObstacle}, geometry={surface.useGeometry}");

                // Mark scene dirty so it saves
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                return;
            }
        }
        Debug.LogError("[RebakeFriendlyNavMesh] No friendly NavMeshSurface (agentTypeID=0) found!");
    }
}
