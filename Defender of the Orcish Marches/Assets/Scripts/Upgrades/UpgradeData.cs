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
}
