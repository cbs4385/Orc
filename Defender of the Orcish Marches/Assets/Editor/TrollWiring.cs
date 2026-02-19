using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

/// <summary>
/// Editor script to wire the Troll FBX model into the Troll EnemyData asset.
/// Creates a TrollAnimator controller with Walk/Attack/Die states,
/// then assigns modelPrefab + animatorController on Troll.asset.
/// Run via: Tools > Wire Troll Model
/// </summary>
public class TrollWiring
{
    [MenuItem("Tools/Wire Troll Model")]
    public static void WireTrollModel()
    {
        string fbxPath = "Assets/Models/Troll.fbx";
        string controllerPath = "Assets/Animations/TrollAnimator.controller";
        string enemyDataPath = "Assets/ScriptableObjects/Enemies/Troll.asset";

        // ── 1. Load FBX ──────────────────────────────
        var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null)
        {
            Debug.LogError($"[TrollWiring] FBX not found at {fbxPath}. Export from Blender first.");
            return;
        }
        Debug.Log($"[TrollWiring] Loaded FBX: {fbxPath}");

        // ── 2. Extract animation clips from FBX ─────
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        AnimationClip walkClip = null, attackClip = null, dieClip = null;

        foreach (var asset in allAssets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
            {
                string lower = clip.name.ToLower();
                Debug.Log($"[TrollWiring] Found clip: {clip.name} (length={clip.length:F2}s)");

                if (lower.Contains("walk"))
                    walkClip = clip;
                else if (lower.Contains("attack"))
                    attackClip = clip;
                else if (lower.Contains("die") || lower.Contains("death"))
                    dieClip = clip;
            }
        }

        if (walkClip == null || attackClip == null || dieClip == null)
        {
            Debug.LogError($"[TrollWiring] Missing clips! Walk={walkClip != null}, Attack={attackClip != null}, Die={dieClip != null}");
            Debug.Log("[TrollWiring] Available clips in FBX:");
            foreach (var asset in allAssets)
            {
                if (asset is AnimationClip c)
                    Debug.Log($"  - {c.name}");
            }
            return;
        }

        // ── 3. Configure walk clip to loop ───────────
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer != null)
        {
            var clipAnimations = importer.clipAnimations;
            if (clipAnimations == null || clipAnimations.Length == 0)
            {
                clipAnimations = importer.defaultClipAnimations;
            }

            bool changed = false;
            foreach (var clipAnim in clipAnimations)
            {
                if (clipAnim.name.ToLower().Contains("walk") && !clipAnim.loopTime)
                {
                    clipAnim.loopTime = true;
                    changed = true;
                    Debug.Log($"[TrollWiring] Set {clipAnim.name} to loop");
                }
            }

            if (changed)
            {
                importer.clipAnimations = clipAnimations;
                importer.SaveAndReimport();

                // Re-load clips after reimport
                allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                foreach (var asset in allAssets)
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
                    {
                        string lower = clip.name.ToLower();
                        if (lower.Contains("walk")) walkClip = clip;
                        else if (lower.Contains("attack")) attackClip = clip;
                        else if (lower.Contains("die") || lower.Contains("death")) dieClip = clip;
                    }
                }
            }
        }

        // ── 4. Build AnimatorController ──────────────
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        // Clear existing layers beyond base
        while (controller.layers.Length > 1)
        {
            controller.RemoveLayer(1);
        }

        // Ensure parameters exist
        bool hasAttack = false, hasDie = false;
        foreach (var p in controller.parameters)
        {
            if (p.name == "Attack") hasAttack = true;
            if (p.name == "Die") hasDie = true;
        }
        if (!hasAttack) controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        if (!hasDie) controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        // Get the base layer state machine
        var rootStateMachine = controller.layers[0].stateMachine;

        // Clear existing states
        foreach (var state in rootStateMachine.states.ToArray())
        {
            rootStateMachine.RemoveState(state.state);
        }

        // Add states with clips
        var walkState = rootStateMachine.AddState("Walk", new Vector3(300, 0, 0));
        walkState.motion = walkClip;

        var attackState = rootStateMachine.AddState("Attack", new Vector3(300, 100, 0));
        attackState.motion = attackClip;

        var dieState = rootStateMachine.AddState("Die", new Vector3(300, 200, 0));
        dieState.motion = dieClip;

        // Set Walk as default
        rootStateMachine.defaultState = walkState;

        // ── Transitions ─────────────────────────────

        // Walk → Attack (on trigger)
        var walkToAttack = walkState.AddTransition(attackState);
        walkToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        walkToAttack.hasExitTime = false;
        walkToAttack.duration = 0.1f;

        // Attack → Walk (when done)
        var attackToWalk = attackState.AddTransition(walkState);
        attackToWalk.hasExitTime = true;
        attackToWalk.exitTime = 0.9f;
        attackToWalk.duration = 0.1f;

        // Any State → Die (on trigger)
        var anyToDie = rootStateMachine.AddAnyStateTransition(dieState);
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
        anyToDie.hasExitTime = false;
        anyToDie.duration = 0.1f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"[TrollWiring] AnimatorController created at {controllerPath} with Walk, Attack, Die states");

        // ── 5. Wire Troll.asset ScriptableObject ────
        var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(enemyDataPath);
        if (enemyData == null)
        {
            Debug.LogError($"[TrollWiring] EnemyData not found at {enemyDataPath}");
            return;
        }

        enemyData.modelPrefab = fbxAsset;
        enemyData.animatorController = controller;
        EditorUtility.SetDirty(enemyData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[TrollWiring] Troll.asset wired: modelPrefab={fbxAsset.name}, animatorController={controller.name}");
        Debug.Log("[TrollWiring] Done! Troll enemies will now use the custom model at runtime.");
    }
}
