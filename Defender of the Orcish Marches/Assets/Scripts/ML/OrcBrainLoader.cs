using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.Barracuda;

/// <summary>
/// Loads the appropriate OrcCommander brain (.onnx) based on game difficulty.
/// Attach to the OrcCommanderAgent GameObject.
///
/// In inference mode, this swaps the agent's model at runtime so the ML brain
/// controls enemy spawning instead of the deterministic EnemySpawnManager.
///
/// Easy difficulty uses Heuristic (random) spawning — no brain needed.
/// Normal uses an early-training brain (~1.2M steps).
/// Hard/Nightmare use the fully trained brain (~10M steps).
/// </summary>
public class OrcBrainLoader : MonoBehaviour
{
    [Header("Brain Models (assign in Inspector)")]
    [Tooltip("Early-training brain for Normal difficulty")]
    [SerializeField] private NNModel normalBrain;

    [Tooltip("Fully trained brain for Hard/Nightmare difficulty")]
    [SerializeField] private NNModel hardBrain;

    private void Start()
    {
        // Load brain for normal gameplay and inference mode; skip during ML training (agents learn from scratch)
        if (MLTrainingManager.IsTraining)
            return;

        var agent = GetComponent<OrcCommanderAgent>();
        if (agent == null)
        {
            Debug.LogError("[OrcBrainLoader] No OrcCommanderAgent found on this GameObject!");
            return;
        }

        var behaviorParams = GetComponent<BehaviorParameters>();
        if (behaviorParams == null)
        {
            Debug.LogError("[OrcBrainLoader] No BehaviorParameters found on this GameObject!");
            return;
        }

        Difficulty diff = GameSettings.CurrentDifficulty;

        switch (diff)
        {
            case Difficulty.Easy:
                // Use Heuristic — random spawning, no brain
                behaviorParams.BehaviorType = BehaviorType.HeuristicOnly;
                Debug.Log("[OrcBrainLoader] Easy difficulty — using Heuristic (random) spawning.");
                return;

            case Difficulty.Normal:
                if (normalBrain == null)
                {
                    Debug.LogError("[OrcBrainLoader] Normal brain not assigned! Falling back to Heuristic.");
                    behaviorParams.BehaviorType = BehaviorType.HeuristicOnly;
                    return;
                }
                agent.SetModel("OrcCommander", normalBrain);
                Debug.Log($"[OrcBrainLoader] Loaded Normal brain: {normalBrain.name}");
                break;

            case Difficulty.Hard:
            case Difficulty.Nightmare:
                if (hardBrain == null)
                {
                    Debug.LogError($"[OrcBrainLoader] Hard brain not assigned! Falling back to Heuristic.");
                    behaviorParams.BehaviorType = BehaviorType.HeuristicOnly;
                    return;
                }
                agent.SetModel("OrcCommander", hardBrain);
                Debug.Log($"[OrcBrainLoader] Loaded {diff} brain: {hardBrain.name}");
                break;
        }
    }
}
