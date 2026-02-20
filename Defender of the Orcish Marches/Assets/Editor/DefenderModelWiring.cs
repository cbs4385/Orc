using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

/// <summary>
/// Editor script to wire defender/menial FBX models into their data assets and prefabs.
/// Creates AnimatorControllers with Walk/Attack/Die states for each character.
/// Run via: Tools > Wire Defender Models > [Character]
/// </summary>
public class DefenderModelWiring
{
    // ── Wire All ──────────────────────────────────────
    [MenuItem("Tools/Wire Defender Models/Wire All")]
    public static void WireAll()
    {
        WireMenial();
        WireRefugee();
        WireTreasure();
        WireEngineer();
        WirePikeman();
        WireCrossbowman();
        WireWizard();
        Debug.Log("[DefenderModelWiring] All characters wired!");
    }

    // ── Menial ────────────────────────────────────────
    [MenuItem("Tools/Wire Defender Models/Menial")]
    public static void WireMenial()
    {
        string fbxPath = "Assets/Models/Menial.fbx";
        string controllerPath = "Assets/Animations/MenialAnimator.controller";
        string prefabPath = "Assets/Prefabs/Characters/Menial.prefab";

        var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null)
        {
            Debug.LogError($"[DefenderModelWiring] FBX not found at {fbxPath}");
            return;
        }

        var clips = ExtractClips(fbxPath);
        ConfigureWalkLoop(fbxPath, ref clips);
        var controller = BuildAnimatorController(controllerPath, clips);

        // Wire the Menial prefab directly (no ScriptableObject)
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[DefenderModelWiring] Menial prefab not found at {prefabPath}");
            return;
        }

        var menial = prefab.GetComponent<Menial>();
        if (menial == null)
        {
            Debug.LogError("[DefenderModelWiring] Menial component not found on prefab");
            return;
        }

        // Use SerializedObject to set private serialized fields
        var so = new SerializedObject(menial);
        so.FindProperty("modelPrefab").objectReferenceValue = fbxAsset;
        so.FindProperty("animatorController").objectReferenceValue = controller;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log($"[DefenderModelWiring] Menial prefab wired: model={fbxAsset.name}, controller={controller.name}");
    }

    // ── Refugee ────────────────────────────────────────
    [MenuItem("Tools/Wire Defender Models/Refugee")]
    public static void WireRefugee()
    {
        string fbxPath = "Assets/Models/Refugee.fbx";
        string controllerPath = "Assets/Animations/RefugeeAnimator.controller";
        string prefabPath = "Assets/Prefabs/Characters/Refugee.prefab";

        var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null)
        {
            Debug.LogError($"[DefenderModelWiring] FBX not found at {fbxPath}");
            return;
        }

        var clips = ExtractClips(fbxPath);
        ConfigureWalkLoop(fbxPath, ref clips);
        var controller = BuildAnimatorController(controllerPath, clips);

        // Wire the Refugee prefab directly (no ScriptableObject)
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[DefenderModelWiring] Refugee prefab not found at {prefabPath}");
            return;
        }

        var refugee = prefab.GetComponent<Refugee>();
        if (refugee == null)
        {
            Debug.LogError("[DefenderModelWiring] Refugee component not found on prefab");
            return;
        }

        var so = new SerializedObject(refugee);
        so.FindProperty("modelPrefab").objectReferenceValue = fbxAsset;
        so.FindProperty("animatorController").objectReferenceValue = controller;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log($"[DefenderModelWiring] Refugee prefab wired: model={fbxAsset.name}, controller={controller.name}");
    }

    // ── Treasure ──────────────────────────────────────
    [MenuItem("Tools/Wire Defender Models/Treasure")]
    public static void WireTreasure()
    {
        string fbxPath = "Assets/Models/TreasureChest.fbx";
        string prefabPath = "Assets/Prefabs/Loot/TreasurePickup.prefab";

        var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null)
        {
            Debug.LogError($"[DefenderModelWiring] FBX not found at {fbxPath}");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[DefenderModelWiring] TreasurePickup prefab not found at {prefabPath}");
            return;
        }

        var pickup = prefab.GetComponent<TreasurePickup>();
        if (pickup == null)
        {
            Debug.LogError("[DefenderModelWiring] TreasurePickup component not found on prefab");
            return;
        }

        var so = new SerializedObject(pickup);
        so.FindProperty("modelPrefab").objectReferenceValue = fbxAsset;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log($"[DefenderModelWiring] TreasurePickup prefab wired: model={fbxAsset.name}");
    }

    // ── Engineer ──────────────────────────────────────
    [MenuItem("Tools/Wire Defender Models/Engineer")]
    public static void WireEngineer()
    {
        WireDefenderData("Engineer",
            "Assets/Models/Engineer.fbx",
            "Assets/Animations/EngineerAnimator.controller",
            "Assets/ScriptableObjects/Defenders/Engineer.asset");
    }

    // ── Pikeman ───────────────────────────────────────
    [MenuItem("Tools/Wire Defender Models/Pikeman")]
    public static void WirePikeman()
    {
        WireDefenderData("Pikeman",
            "Assets/Models/Pikeman.fbx",
            "Assets/Animations/PikemanAnimator.controller",
            "Assets/ScriptableObjects/Defenders/Pikeman.asset");
    }

    // ── Crossbowman ──────────────────────────────────
    [MenuItem("Tools/Wire Defender Models/Crossbowman")]
    public static void WireCrossbowman()
    {
        WireDefenderData("Crossbowman",
            "Assets/Models/Crossbowman.fbx",
            "Assets/Animations/CrossbowmanAnimator.controller",
            "Assets/ScriptableObjects/Defenders/Crossbowman.asset");
    }

    // ── Wizard ────────────────────────────────────────
    [MenuItem("Tools/Wire Defender Models/Wizard")]
    public static void WireWizard()
    {
        WireDefenderData("Wizard",
            "Assets/Models/Wizard.fbx",
            "Assets/Animations/WizardAnimator.controller",
            "Assets/ScriptableObjects/Defenders/Wizard.asset");
    }

    // ── Shared: Wire a DefenderData asset ─────────────
    private static void WireDefenderData(string charName, string fbxPath, string controllerPath, string dataPath)
    {
        var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (fbxAsset == null)
        {
            Debug.LogError($"[DefenderModelWiring] FBX not found at {fbxPath}");
            return;
        }
        Debug.Log($"[DefenderModelWiring] Loaded FBX: {fbxPath}");

        var clips = ExtractClips(fbxPath);
        ConfigureWalkLoop(fbxPath, ref clips);
        var controller = BuildAnimatorController(controllerPath, clips);

        var defenderData = AssetDatabase.LoadAssetAtPath<DefenderData>(dataPath);
        if (defenderData == null)
        {
            Debug.LogError($"[DefenderModelWiring] DefenderData not found at {dataPath}");
            return;
        }

        defenderData.modelPrefab = fbxAsset;
        defenderData.animatorController = controller;
        EditorUtility.SetDirty(defenderData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DefenderModelWiring] {charName} wired: modelPrefab={fbxAsset.name}, controller={controller.name}");
    }

    // ── Extract Walk/Attack/Die clips from FBX ────────
    private struct ClipSet
    {
        public AnimationClip walk, attack, die;
    }

    private static ClipSet ExtractClips(string fbxPath)
    {
        var clips = new ClipSet();
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);

        foreach (var asset in allAssets)
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
            {
                string lower = clip.name.ToLower();
                Debug.Log($"[DefenderModelWiring] Found clip: {clip.name} (length={clip.length:F2}s)");

                if (lower.Contains("walk"))
                    clips.walk = clip;
                else if (lower.Contains("attack"))
                    clips.attack = clip;
                else if (lower.Contains("die") || lower.Contains("death"))
                    clips.die = clip;
            }
        }

        if (clips.walk == null)
            Debug.LogWarning($"[DefenderModelWiring] No Walk clip found in {fbxPath}");
        if (clips.die == null)
            Debug.LogWarning($"[DefenderModelWiring] No Die clip found in {fbxPath}");

        return clips;
    }

    // ── Configure walk clip to loop via ModelImporter ──
    private static void ConfigureWalkLoop(string fbxPath, ref ClipSet clips)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        var clipAnimations = importer.clipAnimations;
        if (clipAnimations == null || clipAnimations.Length == 0)
            clipAnimations = importer.defaultClipAnimations;

        bool changed = false;
        foreach (var clipAnim in clipAnimations)
        {
            if (clipAnim.name.ToLower().Contains("walk") && !clipAnim.loopTime)
            {
                clipAnim.loopTime = true;
                changed = true;
                Debug.Log($"[DefenderModelWiring] Set {clipAnim.name} to loop");
            }
        }

        if (changed)
        {
            importer.clipAnimations = clipAnimations;
            importer.SaveAndReimport();

            // Re-extract clips after reimport
            clips = ExtractClips(fbxPath);
        }
    }

    // ── Build AnimatorController with Walk/Attack/Die ──
    private static AnimatorController BuildAnimatorController(string controllerPath, ClipSet clips)
    {
        // Ensure Animations folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Clear extra layers
        while (controller.layers.Length > 1)
            controller.RemoveLayer(1);

        // Ensure parameters
        bool hasAttack = false, hasDie = false;
        foreach (var p in controller.parameters)
        {
            if (p.name == "Attack") hasAttack = true;
            if (p.name == "Die") hasDie = true;
        }
        if (!hasAttack) controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        if (!hasDie) controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        // Clear existing states
        var rootSM = controller.layers[0].stateMachine;
        foreach (var state in rootSM.states.ToArray())
            rootSM.RemoveState(state.state);

        // Add Walk state (default)
        if (clips.walk != null)
        {
            var walkState = rootSM.AddState("Walk", new Vector3(300, 0, 0));
            walkState.motion = clips.walk;
            rootSM.defaultState = walkState;

            // Add Attack state if clip exists
            if (clips.attack != null)
            {
                var attackState = rootSM.AddState("Attack", new Vector3(300, 100, 0));
                attackState.motion = clips.attack;

                var walkToAttack = walkState.AddTransition(attackState);
                walkToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                walkToAttack.hasExitTime = false;
                walkToAttack.duration = 0.1f;

                var attackToWalk = attackState.AddTransition(walkState);
                attackToWalk.hasExitTime = true;
                attackToWalk.exitTime = 0.9f;
                attackToWalk.duration = 0.1f;
            }

            // Add Die state if clip exists
            if (clips.die != null)
            {
                var dieState = rootSM.AddState("Die", new Vector3(300, 200, 0));
                dieState.motion = clips.die;

                var anyToDie = rootSM.AddAnyStateTransition(dieState);
                anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
                anyToDie.hasExitTime = false;
                anyToDie.duration = 0.1f;
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"[DefenderModelWiring] AnimatorController created at {controllerPath}");

        return controller;
    }
}
