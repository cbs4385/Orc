using UnityEngine;

/// <summary>
/// Subscribes to game events and accumulates reward signals for both ML agents.
///
/// ORC REWARD PHILOSOPHY: WIN OR DIE TRYING. The orc must breach walls and push
/// melee units through to the tower. Chip damage from artillery is worth little.
/// The reward gradient points toward: breach wall → push melee inside → reach tower → WIN.
///
/// REWARD HIERARCHY (orc):
///   1. WIN (game over):          +100 — the entire point, must dwarf all else
///   2. Enemy inside walls:       +5/check — melee unit past the wall ring = on path to victory
///   3. Wall destroyed:           +20  — opens the path
///   4. Focus-fire progress:      +5   — concentrating on weakest wall
///   5. Wall damage (melee-range): +1-3 — enemies attacking walls up close
///   6. Wall damage (any):        +0.2 — chip damage is worth little
///   7. Spawn budget used:        +0.2 — must spawn, but spawning isn't the goal
///   8. LOSE penalty:             -5   — failing to win must hurt
///
/// DEFENDER REWARD PHILOSOPHY: Reward economy and survival.
/// </summary>
public class MLRewardTracker : MonoBehaviour
{
    private IMLAgent orcAgent;

    public void RegisterOrcAgent(IMLAgent agent) { orcAgent = agent; }

    private float orcRewardAccum;

    // Track wall HP delta for continuous reward
    private float lastTotalWallHP = -1f;
    private float wallHPCheckTimer;
    private const float WALL_HP_CHECK_INTERVAL = 1f;

    // Track lowest wall HP — reward progress toward breaking ANY single wall
    private float lowestWallHPFraction = 1f;

    // Cap total wall damage reward per episode to prevent Cannoneer farming
    private float wallDamageRewardThisEpisode;
    private const float MAX_WALL_DAMAGE_REWARD_PER_EPISODE = 30f;

    // Breach penetration check
    private float breachCheckTimer;
    private const float BREACH_CHECK_INTERVAL = 0.5f;
    private const float WALL_RING_RADIUS = 4.5f; // Enemies inside this radius are past the walls
    private const float TOWER_RADIUS = 3f; // Game over distance

    private void OnEnable()
    {
        Enemy.OnEnemyDied += HandleEnemyDied;
        Defender.OnDefenderDied += HandleDefenderDied;
        Menial.OnMenialDied += HandleMenialDied;

        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.OnNewDay += HandleNewDay;

        if (WallManager.Instance != null)
        {
            foreach (var wall in WallManager.Instance.AllWalls)
                SubscribeToWall(wall);
        }
        WallManager.OnWallBuilt += HandleWallBuilt;

        Ballista.OnBallistaShotFired += HandleBallistaShot;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyDied -= HandleEnemyDied;
        Defender.OnDefenderDied -= HandleDefenderDied;
        Menial.OnMenialDied -= HandleMenialDied;
        WallManager.OnWallBuilt -= HandleWallBuilt;
        Ballista.OnBallistaShotFired -= HandleBallistaShot;

        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.OnNewDay -= HandleNewDay;
    }

    private void Update()
    {
        if (!MLTrainingManager.IsMLControlled) return;

        // Flush accumulated rewards
        if (orcAgent != null && orcRewardAccum != 0f)
        {
            orcAgent.GiveReward(orcRewardAccum);
            orcRewardAccum = 0f;
        }

        // Wall HP delta + focus fire
        wallHPCheckTimer -= Time.deltaTime;
        if (wallHPCheckTimer <= 0f)
        {
            wallHPCheckTimer = WALL_HP_CHECK_INTERVAL;
            ComputeWallHPDeltaReward();
            ComputeLowestWallReward();
        }

        // Breach penetration — reward enemies inside the wall ring
        breachCheckTimer -= Time.deltaTime;
        if (breachCheckTimer <= 0f)
        {
            breachCheckTimer = BREACH_CHECK_INTERVAL;
            ComputeBreachPenetrationReward();
        }
    }

    // ==================== EVENT HANDLERS ====================

    private void HandleEnemyDied(Enemy enemy)
    {
        // No defender agent to reward
    }

    private void HandleDefenderDied(Defender defender)
    {
        orcRewardAccum += 2.0f;
    }

    private void HandleMenialDied()
    {
        orcRewardAccum += 1.0f;
    }

    private void HandleWallDamaged(Wall wall)
    {
        // Capped per episode — prevents Cannoneer farming from being the dominant strategy.
        // Once the cap is hit, the only way to earn more is to breach and push through.
        if (wallDamageRewardThisEpisode >= MAX_WALL_DAMAGE_REWARD_PER_EPISODE)
            return;

        float hpFrac = wall.MaxHP > 0 ? (float)wall.CurrentHP / wall.MaxHP : 1f;
        float reward = Mathf.Lerp(1.0f, 0.2f, hpFrac);
        wallDamageRewardThisEpisode += reward;
        orcRewardAccum += reward;
    }

    private void HandleWallDestroyed(Wall wall)
    {
        // MASSIVE reward — breaching opens the path to the tower
        orcRewardAccum += 20.0f;
        Debug.Log($"[MLRewardTracker] WALL DESTROYED! Orc +20.0");
    }

    private void HandleNewDay(int dayNumber)
    {
        if (WallManager.Instance != null)
        {
            foreach (var wall in WallManager.Instance.AllWalls)
                SubscribeToWall(wall);
        }
        lastTotalWallHP = -1f;
        lowestWallHPFraction = 1f;
    }

    private void HandleWallBuilt()
    {
        if (WallManager.Instance != null)
        {
            foreach (var wall in WallManager.Instance.AllWalls)
                SubscribeToWall(wall);
        }
        lastTotalWallHP = -1f;
    }

    private void HandleBallistaShot()
    {
        // Orc agent doesn't need ballista shot info
    }

    // ==================== ORC STRATEGIC REWARDS ====================

    /// <summary>
    /// Continuous wall HP delta reward. Reduced from 0.02 to 0.005 per HP —
    /// chip damage shouldn't be the primary reward source.
    /// </summary>
    private void ComputeWallHPDeltaReward()
    {
        if (orcAgent == null) return;
        if (wallDamageRewardThisEpisode >= MAX_WALL_DAMAGE_REWARD_PER_EPISODE) return;

        float currentHP = GetTotalWallHP();
        if (lastTotalWallHP < 0f)
        {
            lastTotalWallHP = currentHP;
            return;
        }

        float hpLost = lastTotalWallHP - currentHP;
        if (hpLost > 0f)
        {
            float reward = hpLost * 0.005f;
            wallDamageRewardThisEpisode += reward;
            orcRewardAccum += reward;
        }

        lastTotalWallHP = currentHP;
    }

    /// <summary>
    /// Rewards the orc for concentrating damage on ONE wall.
    /// </summary>
    private void ComputeLowestWallReward()
    {
        if (orcAgent == null) return;
        if (WallManager.Instance == null) return;

        float currentLowest = 1f;
        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (wall == null || wall.IsDestroyed) { currentLowest = 0f; break; }
            float frac = wall.MaxHP > 0 ? (float)wall.CurrentHP / wall.MaxHP : 1f;
            if (frac < currentLowest)
                currentLowest = frac;
        }

        if (currentLowest < lowestWallHPFraction)
        {
            float progress = lowestWallHPFraction - currentLowest;
            orcRewardAccum += progress * 5.0f;
            lowestWallHPFraction = currentLowest;
        }
    }

    /// <summary>
    /// BIG reward for enemies that have penetrated past the wall ring.
    /// This is the key reward that teaches the orc: breaching isn't enough,
    /// you need melee units INSIDE the fortress heading toward the tower.
    /// Cannoneers will never trigger this — they stand at range.
    /// Scales with proximity to tower: +5 at wall ring, +15 near tower.
    /// </summary>
    private void ComputeBreachPenetrationReward()
    {
        if (orcAgent == null) return;
        if (WallManager.Instance == null || !WallManager.Instance.HasBreach()) return;

        Vector3 center = GameManager.FortressCenter;
        float totalReward = 0f;

        foreach (var enemy in Enemy.ActiveEnemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            float dist = Vector3.Distance(enemy.transform.position, center);

            // Enemy is inside the wall ring
            if (dist < WALL_RING_RADIUS)
            {
                // Scale: +5 at wall ring edge, +15 right next to tower
                float penetration = 1f - (dist / WALL_RING_RADIUS); // 0 at edge, 1 at center
                float reward = Mathf.Lerp(5f, 15f, penetration);
                totalReward += reward;
            }
        }

        if (totalReward > 0f)
        {
            orcRewardAccum += totalReward;
        }
    }

    // ==================== HELPERS ====================

    private void SubscribeToWall(Wall wall)
    {
        if (wall == null) return;
        wall.OnWallDamaged -= HandleWallDamaged;
        wall.OnWallDamaged += HandleWallDamaged;
        wall.OnWallDestroyed -= HandleWallDestroyed;
        wall.OnWallDestroyed += HandleWallDestroyed;
    }

    private float GetTotalWallHP()
    {
        if (WallManager.Instance == null) return 0f;
        float total = 0f;
        foreach (var wall in WallManager.Instance.AllWalls)
        {
            if (wall != null)
                total += wall.CurrentHP;
        }
        return total;
    }

    /// <summary>Called by MLTrainingManager on episode reset to clear tracking state.</summary>
    public void ResetTracking()
    {
        lastTotalWallHP = -1f;
        lowestWallHPFraction = 1f;
        wallDamageRewardThisEpisode = 0f;
        orcRewardAccum = 0f;
    }
}
