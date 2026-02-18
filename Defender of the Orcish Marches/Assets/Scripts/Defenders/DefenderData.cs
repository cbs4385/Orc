using UnityEngine;

public enum DefenderType
{
    Engineer,
    Pikeman,
    Crossbowman,
    Wizard
}

[CreateAssetMenu(fileName = "NewDefender", menuName = "Game/Defender Data")]
public class DefenderData : ScriptableObject
{
    public string defenderName = "Defender";
    public DefenderType defenderType;
    public int menialCost = 2;
    public int treasureCost = 30;
    public int damage = 10;
    public float range = 5f;
    public float attackRate = 1f;
    public Color bodyColor = new Color(0.2f, 0.4f, 0.8f);
}
