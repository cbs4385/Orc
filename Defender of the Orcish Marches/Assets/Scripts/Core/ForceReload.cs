using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ForceReload : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Game/Force Domain Reload")]
    public static void Reload()
    {
        EditorUtility.RequestScriptReload();
        Debug.Log("Domain reload requested.");
    }
#endif
}
