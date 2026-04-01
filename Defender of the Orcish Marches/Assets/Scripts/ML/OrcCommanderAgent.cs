using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/// <summary>
/// ML-Agents Agent that controls enemy spawning.
/// Decides which enemy type to spawn and at what angle within the spawn arc.
/// </summary>
public class OrcCommanderAgent : Agent, IMLAgent
{
    public override void Initialize()
    {
        // In inference mode, no step limit — game runs until natural end
        MaxStep = MLTrainingManager.IsInference ? 0 : 20000;
        Debug.Log($"[OrcCommanderAgent] Initialized. MaxStep={MaxStep}, mode={MLTrainingManager.Instance?.CurrentMode}");
        DontDestroyOnLoad(gameObject);

        if (MLTrainingManager.Instance != null)
            MLTrainingManager.Instance.RegisterOrcAgent(this);

        var tracker = FindAnyObjectByType<MLRewardTracker>();
        if (tracker != null) tracker.RegisterOrcAgent(this);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Shared observations (~42 values)
        var obs = MLGameStateObserver.CollectSharedObservations();
        foreach (float v in obs)
            sensor.AddObservation(v);
    }

    private int lastKnownDay;

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;
        bool isDay = DayNightCycle.Instance != null && DayNightCycle.Instance.IsDay;

        // Refresh spawn budget when a new day starts.
        // Event subscriptions are unreliable across in-place resets, so we detect
        // day changes here and force the budget to be set.
        if (isDay && dayNumber != lastKnownDay && EnemySpawnManager.Instance != null)
        {
            lastKnownDay = dayNumber;
            EnemySpawnManager.Instance.InitSpawnBudgetForDay(dayNumber);
        }

        int spawnsRemaining = EnemySpawnManager.Instance != null
            ? EnemySpawnManager.Instance.RegularSpawnsRemaining : 0;

        // Branch 0: Enemy type (0-5 = types, 6 = skip)
        if (EnemySpawnManager.Instance != null)
        {
            if (dayNumber < 2 || EnemySpawnManager.Instance.GetEnemyDataByType(1) == null)
                actionMask.SetActionEnabled(0, 1, false);
            if (dayNumber < 3 || EnemySpawnManager.Instance.GetEnemyDataByType(2) == null)
                actionMask.SetActionEnabled(0, 2, false);
            if (dayNumber < 4 || EnemySpawnManager.Instance.GetEnemyDataByType(3) == null)
                actionMask.SetActionEnabled(0, 3, false);
            if (dayNumber < 5 || EnemySpawnManager.Instance.GetEnemyDataByType(4) == null)
                actionMask.SetActionEnabled(0, 4, false);
        }

        // War Boss (index 5): only when arc conditions met
        bool bossAllowed = false;
        if (EnemySpawnManager.Instance != null)
        {
            var arc = EnemySpawnManager.Instance.GetCurrentArcBounds();
            bossAllowed = arc.halfSpread >= 90f && EnemySpawnManager.Instance.GetEnemyDataByType(5) != null;
        }
        if (!bossAllowed) actionMask.SetActionEnabled(0, 5, false);

        // If no spawns remaining or it's night, force skip
        if (spawnsRemaining <= 0 || !isDay)
        {
            for (int i = 0; i < 6; i++)
                actionMask.SetActionEnabled(0, i, false);
        }
    }

    private int actionCount;
    private int spawnCount;
    private int skipCount;

    public override void OnEpisodeBegin()
    {
        Debug.Log("[OrcCommanderAgent] Episode begin.");
        actionCount = 0;
        spawnCount = 0;
        skipCount = 0;
        lastKnownDay = 0; // Force budget refresh on first decision

        // Re-register with tracker every episode — the reference can be lost
        // after in-place resets due to DontDestroyOnLoad timing issues.
        if (MLTrainingManager.Instance != null)
            MLTrainingManager.Instance.RegisterOrcAgent(this);
        var tracker = FindAnyObjectByType<MLRewardTracker>();
        if (tracker != null) tracker.RegisterOrcAgent(this);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // During ML training, MLTrainingManager controls everything — only block if explicitly training-only logic needed
        int typeIndex = actions.DiscreteActions[0];
        int angleBin = actions.DiscreteActions[1];
        actionCount++;

        // Skip action
        if (typeIndex == 6)
        {
            skipCount++;
            // Log every 100 skips to track how much the orc is stuck
            if (skipCount % 100 == 1)
                Debug.Log($"[OrcCommanderAgent] Skip #{skipCount} (actions={actionCount}, spawns={spawnCount}, budget={EnemySpawnManager.Instance?.RegularSpawnsRemaining}, isDay={DayNightCycle.Instance?.IsDay})");
            return;
        }

        if (EnemySpawnManager.Instance == null) return;
        if (EnemySpawnManager.Instance.RegularSpawnsRemaining <= 0) return;
        if (DayNightCycle.Instance == null || !DayNightCycle.Instance.IsDay) return;

        EnemyData data = EnemySpawnManager.Instance.GetEnemyDataByType(typeIndex);
        if (data == null) return;

        // Convert angle bin to actual angle within arc
        var arcBounds = EnemySpawnManager.Instance.GetCurrentArcBounds();
        float arcWidth = arcBounds.halfSpread * 2f;
        float angleDeg = arcBounds.centerAngle - arcBounds.halfSpread + (angleBin + 0.5f) * arcWidth / 12f;

        Enemy spawned = EnemySpawnManager.Instance.SpawnEnemyML(data, angleDeg);
        if (spawned != null)
        {
            spawnCount++;
            EnemySpawnManager.Instance.DecrementSpawnBudget();

            // Base spawn reward
            AddReward(0.1f);

            // Bonus for melee/WallBreaker units — these are the ones that can actually
            // walk through breaches and reach the tower. Cannoneers can't win the game.
            bool isMelee = data.enemyType == EnemyType.Melee || data.enemyType == EnemyType.WallBreaker;
            if (isMelee)
            {
                AddReward(0.3f); // melee bonus

                // Extra bonus when spawning melee toward a weak/destroyed wall —
                // this is the winning move: breach + flood
                float lowestWallFrac = GetLowestWallHPFraction();
                if (lowestWallFrac < 0.3f)
                {
                    AddReward(1.0f); // big bonus: melee near a breach
                }
            }

            if (spawnCount <= 3 || spawnCount % 50 == 0)
                Debug.Log($"[OrcCommanderAgent] Spawned #{spawnCount}: {data.enemyName} at {angleDeg:F0}°, budget={EnemySpawnManager.Instance.RegularSpawnsRemaining}");
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;

        if (EnemySpawnManager.Instance == null || EnemySpawnManager.Instance.RegularSpawnsRemaining <= 0)
        {
            discrete[0] = 6; // Skip
            discrete[1] = 0;
            return;
        }

        int dayNumber = DayNightCycle.Instance != null ? DayNightCycle.Instance.DayNumber : 1;
        float roll = Random.value;
        if (dayNumber >= 5 && roll < 0.05f) discrete[0] = 4;
        else if (dayNumber >= 4 && roll < 0.15f) discrete[0] = 3;
        else if (dayNumber >= 3 && roll < 0.25f) discrete[0] = 2;
        else if (dayNumber >= 2 && roll < 0.45f) discrete[0] = 1;
        else discrete[0] = 0;

        discrete[1] = Random.Range(0, 12);
    }

    private float GetLowestWallHPFraction()
    {
        if (WallManager.Instance == null) return 1f;
        float lowest = 1f;
        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (wall == null || wall.IsDestroyed) return 0f;
            float frac = wall.MaxHP > 0 ? (float)wall.CurrentHP / wall.MaxHP : 1f;
            if (frac < lowest) lowest = frac;
        }
        return lowest;
    }

    // IMLAgent implementation
    public void GiveReward(float reward) => AddReward(reward);
    public void FinishEpisode() => EndEpisode();
}
