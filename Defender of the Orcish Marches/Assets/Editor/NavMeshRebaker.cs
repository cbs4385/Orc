using UnityEditor;
using UnityEngine;
using Unity.AI.Navigation;

public static class NavMeshRebaker
{
    [MenuItem("Tools/Rebake NavMesh")]
    public static void RebakeNavMesh()
    {
        var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        if (surfaces.Length == 0)
        {
            Debug.LogError("[NavMeshRebaker] No NavMeshSurface found in scene!");
            return;
        }

        foreach (var surface in surfaces)
        {
            surface.BuildNavMesh();
            Debug.Log($"[NavMeshRebaker] Rebaked NavMesh on '{surface.gameObject.name}'.");
        }

        foreach (var surface in surfaces)
            EditorUtility.SetDirty(surface.gameObject);
    }
}
