using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

/// <summary>
/// Editor script to wire up the BasicOrc FBX model into the Enemy prefab
/// with AnimatorController, collider, and animation clips.
/// Run via: Tools > Wire Enemy Prefab
/// </summary>
public class EnemyPrefabWiring
{
    [MenuItem("Tools/Wire Enemy Prefab")]
    public static void WireEnemyPrefab()
    {
        // ── 1. Load assets ──────────────────────────
        string fbxPath = "Assets/Models/BasicOrc.fbx";
        string prefabPath = "Assets/Prefabs/Enemies/Enemy.prefab";
        string controllerPath = "Assets/Animations/EnemyAnimator.controller";

        var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null)
        {
            Debug.LogError($"[EnemyPrefabWiring] FBX not found at {fbxPath}");
            return;
        }

        // ── 2. Extract animation clips from FBX ────
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        AnimationClip walkClip = null, attackClip = null, dieClip = null;

        foreach (var asset in allAssets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
            {
                string lower = clip.name.ToLower();
                Debug.Log($"[EnemyPrefabWiring] Found clip: {clip.name} (length={clip.length:F2}s)");

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
            Debug.LogError($"[EnemyPrefabWiring] Missing clips! Walk={walkClip != null}, Attack={attackClip != null}, Die={dieClip != null}");
            Debug.Log("[EnemyPrefabWiring] Available clips in FBX:");
            foreach (var asset in allAssets)
            {
                if (asset is AnimationClip c)
                    Debug.Log($"  - {c.name}");
            }
            return;
        }

        // ── 3. Configure walk clip to loop ──────────
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
                    Debug.Log($"[EnemyPrefabWiring] Set {clipAnim.name} to loop");
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

        // ── 4. Build AnimatorController ─────────────
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        // Clear existing layers/parameters
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
        Debug.Log("[EnemyPrefabWiring] AnimatorController configured with Walk, Attack, Die states");

        // ── 5. Rebuild Enemy Prefab ─────────────────
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[EnemyPrefabWiring] Prefab not found at {prefabPath}");
            return;
        }

        // Open prefab for editing
        var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

        // ── Remove ALL children (Body, Head, Model, etc.) ──
        // Collect first, then destroy to avoid modifying collection while iterating
        var toRemove = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in prefabContents.transform)
        {
            toRemove.Add(child.gameObject);
        }
        foreach (var go in toRemove)
        {
            Debug.Log($"[EnemyPrefabWiring] Removing old child: {go.name}");
            Object.DestroyImmediate(go);
        }

        // Remove any existing Animator from root (we'll put it on Model child)
        var oldAnimator = prefabContents.GetComponent<Animator>();
        if (oldAnimator != null)
        {
            Debug.Log("[EnemyPrefabWiring] Removing old Animator from Enemy root");
            Object.DestroyImmediate(oldAnimator);
        }

        // Remove any existing collider (we'll rebuild it)
        var oldCollider = prefabContents.GetComponent<Collider>();
        if (oldCollider != null)
        {
            Debug.Log("[EnemyPrefabWiring] Removing old Collider from Enemy root");
            Object.DestroyImmediate(oldCollider);
        }

        // ── Instantiate FBX model as child ──
        var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, prefabContents.transform);
        modelInstance.name = "Model";
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        // ── Add Animator to the Model child (not Enemy root!) ──
        // This keeps FBX axis-conversion rotation within the model hierarchy
        // and prevents it from overriding the Enemy root's transform/NavMeshAgent.
        var animator = modelInstance.GetComponent<Animator>();
        if (animator == null)
            animator = modelInstance.AddComponent<Animator>();

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        // ── Add CapsuleCollider to root for hit detection ──
        // direction=1 means Y-axis aligned; radius 0.4 covers the model
        // with enough margin for fast projectiles
        var capsule = prefabContents.AddComponent<CapsuleCollider>();
        capsule.direction = 1; // Y-axis
        capsule.center = new Vector3(0, 0.45f, 0);
        capsule.radius = 0.4f;
        capsule.height = 0.9f;

        Debug.Log("[EnemyPrefabWiring] Added CapsuleCollider: direction=Y, center=(0,0.45,0), radius=0.4, height=0.9");

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabContents);

        AssetDatabase.Refresh();
        Debug.Log("[EnemyPrefabWiring] Enemy prefab rebuilt with BasicOrc model, Animator on Model child, and CapsuleCollider!");
        Debug.Log("[EnemyPrefabWiring] Done! All wiring complete.");
    }
}
