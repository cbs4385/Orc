using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class InputFixer : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Game/Fix Input System")]
    public static void FixInputSystem()
    {
        // Set Active Input Handling to "Both" (0=Old, 1=New, 2=Both)
#pragma warning disable CS0618
        PlayerSettings.SetPropertyString("activeInputHandler", "2", BuildTargetGroup.Standalone);
#pragma warning restore CS0618
        Debug.Log("Input System set to 'Both'. Unity may need to restart the editor for this to take effect.");
    }
#endif
}
