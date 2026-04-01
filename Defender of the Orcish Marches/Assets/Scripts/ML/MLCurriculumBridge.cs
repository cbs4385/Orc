using UnityEngine;
using Unity.MLAgents;

/// <summary>
/// Reads environment parameters from the ML-Agents Academy and applies them
/// to MLTrainingManager. This bridges the Academy's curriculum system
/// (which MLTrainingManager doesn't directly reference) to the game config.
/// </summary>
public class MLCurriculumBridge : MonoBehaviour
{
    private void Update()
    {
        if (!MLTrainingManager.IsMLControlled) return;
        if (MLTrainingManager.Instance == null) return;
        if (Academy.Instance == null) return;

        // Read max_day from environment parameters (set by curriculum)
        float maxDayParam = Academy.Instance.EnvironmentParameters.GetWithDefault("max_day", -1f);
        if (maxDayParam > 0f)
        {
            int newMaxDay = Mathf.RoundToInt(maxDayParam);
            if (newMaxDay != MLTrainingManager.Instance.MaxDay)
            {
                MLTrainingManager.Instance.SetMaxDay(newMaxDay);
            }
        }
    }
}
