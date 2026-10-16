using UnityEngine;

/// <summary>
/// Tuning and content for a full run: how many rooms, which bosses, which enemies and layouts,
/// and how fast difficulty rises. RoomManager reads this; nothing here is hardcoded in code.
/// </summary>
[CreateAssetMenu(fileName = "NewRun", menuName = "Exterminator/Run Data")]
public class RunData : ScriptableObject
{
    [Min(1)] public int RoomCount = 25;

    [Header("Bosses (1-based room numbers, 0 = none)")]
    [Min(0)] public int MidBossRoom = 12;
    public EnemyData MidBoss;
    public EnemyData FinalBoss;

    [Header("Content")]
    public EnemyData[] EnemyTable;
    public RoomLayoutData[] Layouts;

    [Header("Difficulty")]
    public int BaseEnemies = 5;
    public float EnemiesPerRoom = 0.8f;
    public int MaxEnemies = 55;
    public float HealthPerRoom = 0.08f;
    public float SpeedPerRoom = 0.004f;
    public float MaxSpeedBonus = 0.4f;

    [Header("Room choices")]
    [Range(0f, 1f)] public float ModifierChance = 0.25f;
    [Range(0f, 1f)] public float EliteChance = 0.25f;
    [Range(0f, 1f)] public float ShopChance = 0.2f;
    [Range(0f, 1f)] public float UnknownChance = 0.2f;
    [Range(0f, 1f)] public float ShopHealFraction = 0.4f;

    [Header("Endless")]
    [Tooltip("In endless mode a boss appears every N rooms, alternating the two bosses.")]
    public int EndlessBossEvery = 5;
}
