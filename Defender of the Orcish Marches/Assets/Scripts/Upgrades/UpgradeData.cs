using UnityEngine;

public enum UpgradeType
{
    WallRepair,
    NewBallista,
    BallistaDamage,
    BallistaFireRate,
    NewWall,
    SpawnEngineer,
    SpawnPikeman,
    SpawnCrossbowman,
    SpawnWizard
}

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "Game/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    public string upgradeName = "Upgrade";
    public string description = "";
    public int treasureCost;
    public int menialCost;
    public UpgradeType upgradeType;
    public Sprite icon;
    public bool repeatable = true;

    [Header("Cost Scaling")]
    [Tooltip("Each purchase increases cost by baseCost * costScaling. 0.25 = +25% per purchase.")]
    [Range(0, 2)] public float costScaling = 0f;

    public int GetTreasureCost(int purchased)
    {
        if (treasureCost <= 0) return 0;
        return Mathf.RoundToInt(treasureCost * (1f + costScaling * purchased));
    }

    public int GetMenialCost(int purchased)
    {
        if (menialCost <= 0) return 0;
        return Mathf.RoundToInt(menialCost * (1f + costScaling * purchased));
    }
}
