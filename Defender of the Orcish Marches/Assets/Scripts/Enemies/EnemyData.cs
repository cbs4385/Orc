using UnityEngine;

public enum EnemyType
{
    Melee,
    Ranged,
    WallBreaker,
    Suicide,
    Artillery
}

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "Enemy";
    public EnemyType enemyType = EnemyType.Melee;
    public int maxHP = 30;
    public float moveSpeed = 3f;
    public int damage = 5;
    public float attackRange = 1.5f;
    public float attackRate = 1f;
    public int treasureDrop = 10; // Legacy — kept for reference, not used at runtime
    [Header("Loot Drops")]
    public int minLootDrops = 1;
    public int maxLootDrops = 2;
    public int minLootValue = 3;
    public int maxLootValue = 7;
    public Color bodyColor = new Color(0.27f, 0.67f, 0.27f);
    public Vector3 bodyScale = Vector3.one;
    public GameObject projectilePrefab;
}
