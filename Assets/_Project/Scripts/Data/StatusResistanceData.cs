using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable class representing resistance to a specific status effect type.
/// </summary>
[Serializable]
public class StatusResistance {
    [SerializeField]
    [Tooltip("The type of status effect this resistance applies to")]
    private StatusEffectType effectType;
    
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("0 = no resistance, 1 = immune")]
    private float resistance;
    
    [SerializeField]
    [Tooltip("If true, completely immune to this effect")]
    private bool isImmune;
    
    public StatusEffectType EffectType => effectType;
    public float Resistance => resistance;
    public bool IsImmune => isImmune;
}

/// <summary>
/// ScriptableObject that defines an entity's resistances to status effects.
/// Create instances in Assets/_Project/Data/StatusResistances/
/// Right-click → Create → Exterminator → Status Resistance Data
/// </summary>
[CreateAssetMenu(fileName = "NewStatusResistance", menuName = "Exterminator/Status Resistance Data")]
public class StatusResistanceData : ScriptableObject {
    [Header("Resistances")]
    [SerializeField]
    [Tooltip("List of status effect resistances for this entity")]
    private List<StatusResistance> resistances = new List<StatusResistance>();
    
    private Dictionary<StatusEffectType, StatusResistance> resistanceMap;
    
    public void Initialize() {
        resistanceMap = new Dictionary<StatusEffectType, StatusResistance>();
        foreach (var resistance in resistances) {
            resistanceMap[resistance.EffectType] = resistance;
        }
    }
    
    public bool IsImmune(StatusEffectType type) {
        if (resistanceMap == null) Initialize();
        return resistanceMap != null && 
               resistanceMap.TryGetValue(type, out var resistance) && 
               resistance.IsImmune;
    }
    
    public float GetResistance(StatusEffectType type) {
        if (resistanceMap == null) Initialize();
        if (resistanceMap == null) return 0f;
        return resistanceMap.TryGetValue(type, out var resistance) ? resistance.Resistance : 0f;
    }
    
    /// <summary>
    /// Returns modified duration after resistance is applied.
    /// </summary>
    public float ApplyResistance(StatusEffectType type, float baseDuration) {
        float resistanceValue = GetResistance(type);
        return baseDuration * (1f - resistanceValue);
    }
}
