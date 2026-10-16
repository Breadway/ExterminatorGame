using System;
using System.Collections.Generic;

/// <summary>
/// Everything WaveSpawner needs to spawn one wave. RoomManager builds these from
/// room definitions; without a provider the spawner builds its own endless waves.
/// </summary>
public class WaveSpec
{
    public int enemyCount = 4;
    public List<EnemyData> table;
    public EnemyData boss;
    public float healthMultiplier = 1f;
    public float speedMultiplier = 1f;
    public float xpMultiplier = 1f;
    public float damageMultiplier = 1f;
}

public enum RoomType
{
    Combat,
    Elite,
    Boss,
    Unknown,
    Shop
}

public enum RoomModifier
{
    None,
    Swarm,      // many weaker enemies
    Frenzy,     // fast enemies
    Hardened,   // tough enemies
    Lean        // few enemies, big XP
}

[Serializable]
public class RoomDefinition
{
    public string roomName = "Sector";
    public RoomType type = RoomType.Combat;
    public RoomModifier modifier = RoomModifier.None;
    public int enemyCount = 6;
    [UnityEngine.Tooltip("Leave empty to use the spawner's default enemy table.")]
    public EnemyData[] enemyTable;
    public EnemyData boss;
    [UnityEngine.Tooltip("Extra XP multiplier for this room.")]
    public float xpMultiplier = 1f;
    [UnityEngine.Tooltip("Unknown-room event: acid_rain, shock_plates, web_nest, fuel_leak, supply_cache, document_cache, hidden_cache, ambush. Empty = none.")]
    public string eventId = "";
}

public static class RoomModifiers
{
    public static void Apply(RoomModifier modifier, WaveSpec spec)
    {
        switch (modifier)
        {
            case RoomModifier.Swarm:
                spec.enemyCount = (int)Math.Round(spec.enemyCount * 1.6);
                spec.healthMultiplier *= 0.6f;
                break;
            case RoomModifier.Frenzy:
                spec.speedMultiplier *= 1.3f;
                break;
            case RoomModifier.Hardened:
                spec.healthMultiplier *= 1.6f;
                spec.xpMultiplier *= 1.25f;
                break;
            case RoomModifier.Lean:
                spec.enemyCount = Math.Max(1, (int)Math.Round(spec.enemyCount * 0.5));
                spec.xpMultiplier *= 2f;
                break;
        }
    }

    public static string Label(RoomModifier modifier)
    {
        switch (modifier)
        {
            case RoomModifier.Swarm: return "INFESTATION SURGE";
            case RoomModifier.Frenzy: return "STIMULANT LEAK";
            case RoomModifier.Hardened: return "REINFORCED HOSTILES";
            case RoomModifier.Lean: return "SKELETON CREW";
            default: return "";
        }
    }
}
