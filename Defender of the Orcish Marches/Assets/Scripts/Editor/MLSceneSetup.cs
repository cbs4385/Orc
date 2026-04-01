using UnityEditor;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

/// <summary>
/// Editor script that creates and configures all ML training GameObjects in the current scene.
/// </summary>
public static class MLSceneSetup
{
    [MenuItem("Tools/ML/Setup Training Scene")]
    public static void SetupTrainingScene()
    {
        Debug.Log("[MLSceneSetup] Setting up ML training objects...");

        // --- 1. MLTrainingManager + MLRewardTracker + MLCurriculumBridge ---
        var managerGO = FindOrCreate("MLTrainingManager");
        var trainingMgr = EnsureComponent<MLTrainingManager>(managerGO);
        var rewardTracker = EnsureComponent<MLRewardTracker>(managerGO);
        EnsureComponent<MLCurriculumBridge>(managerGO);

        // Set control mode to MLTraining via SerializedObject
        var managerSO = new SerializedObject(trainingMgr);
        var controlModeProp = managerSO.FindProperty("controlMode");
        if (controlModeProp != null)
            controlModeProp.enumValueIndex = 1; // MLTraining
        var maxDayProp = managerSO.FindProperty("maxDayForEpisode");
        if (maxDayProp != null)
            maxDayProp.intValue = 20;
        var startDayProp = managerSO.FindProperty("startDay");
        if (startDayProp != null)
            startDayProp.intValue = 15;
        var timeScaleProp = managerSO.FindProperty("trainingTimeScale");
        if (timeScaleProp != null)
            timeScaleProp.floatValue = 3f;
        managerSO.ApplyModifiedProperties();

        // --- 2. OrcCommanderAgent ---
        var orcGO = FindOrCreate("OrcCommanderAgent");
        var orcAgent = EnsureComponent<OrcCommanderAgent>(orcGO);
        var orcDecision = EnsureComponent<DecisionRequester>(orcGO);
        orcDecision.DecisionPeriod = 5;

        // MaxStep guarantees the trainer sees done=True after N decision steps.
        // 5-day episode at 5x: 450s game / (0.02 fixedDt * 5 decisionPeriod) = 4500 steps.
        // Set 5000 with buffer. Our custom HandleNewDay/HandleGameOver will usually end sooner.
        var orcAgentSO = new SerializedObject(orcAgent);
        var orcMaxStepProp = orcAgentSO.FindProperty("m_MaxStep");
        if (orcMaxStepProp != null) orcMaxStepProp.intValue = 5000;
        orcAgentSO.ApplyModifiedProperties();

        // Configure BehaviorParameters
        var orcBehavior = orcGO.GetComponent<BehaviorParameters>();
        if (orcBehavior != null)
        {
            orcBehavior.BehaviorName = "OrcCommander";
            orcBehavior.BrainParameters.VectorObservationSize = 39;

            // Set discrete action branches: [7, 12]
            var actionSpec = ActionSpec.MakeDiscrete(7, 12);
            orcBehavior.BrainParameters.ActionSpec = actionSpec;

            // Set team for self-play (team 0 = orc)
            orcBehavior.TeamId = 0;

            Debug.Log("[MLSceneSetup] OrcCommander configured: obs=39, actions=[7,12], team=0");
        }

        // Mark scene dirty so changes are saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[MLSceneSetup] ML training scene setup complete!");
        Debug.Log("[MLSceneSetup] To train: run 'mlagents-learn ml_training_config.yaml --run-id=selfplay_v1' then press Play.");
    }

    [MenuItem("Tools/ML/Setup Inference Mode (OrcCommander AI)")]
    public static void SetupInferenceMode()
    {
        Debug.Log("[MLSceneSetup] Setting up OrcCommander inference mode...");

        // --- 1. MLTrainingManager set to MLInference ---
        var managerGO = FindOrCreate("MLTrainingManager");
        var trainingMgr = EnsureComponent<MLTrainingManager>(managerGO);

        var managerSO = new SerializedObject(trainingMgr);
        var controlModeProp = managerSO.FindProperty("controlMode");
        if (controlModeProp != null)
            controlModeProp.enumValueIndex = 2; // MLInference
        managerSO.ApplyModifiedProperties();

        // --- 2. OrcCommanderAgent with BrainLoader ---
        var orcGO = FindOrCreate("OrcCommanderAgent");
        EnsureComponent<OrcCommanderAgent>(orcGO);
        var orcDecision = EnsureComponent<DecisionRequester>(orcGO);
        orcDecision.DecisionPeriod = 5;
        EnsureComponent<OrcBrainLoader>(orcGO);

        // Configure BehaviorParameters for inference
        var orcBehavior = orcGO.GetComponent<BehaviorParameters>();
        if (orcBehavior != null)
        {
            orcBehavior.BehaviorName = "OrcCommander";
            orcBehavior.BrainParameters.VectorObservationSize = 39;
            var actionSpec = ActionSpec.MakeDiscrete(7, 12);
            orcBehavior.BrainParameters.ActionSpec = actionSpec;
            orcBehavior.BehaviorType = BehaviorType.InferenceOnly;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[MLSceneSetup] Inference setup complete!");
        Debug.Log("[MLSceneSetup] Assign Normal and Hard brain models to OrcBrainLoader in Inspector.");
    }

    private static GameObject FindOrCreate(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            Debug.Log($"[MLSceneSetup] Found existing '{name}' — reusing.");
            return existing;
        }
        var go = new GameObject(name);
        Debug.Log($"[MLSceneSetup] Created '{name}'");
        return go;
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var comp = go.GetComponent<T>();
        if (comp == null)
            comp = go.AddComponent<T>();
        return comp;
    }
}
