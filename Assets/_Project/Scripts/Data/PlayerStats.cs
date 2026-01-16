using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure C# class that manages all player statistics.
/// Handles base stats, modifiers from upgrades, and recalculation.
/// NOT a MonoBehaviour - PlayerController holds an instance of this.
/// </summary>
[System.Serializable]
public class PlayerStats {
    
    // ========================================
    // BASE STATS (Never change after init)
    // ========================================
    
    [Header("Base Stats")]
    [SerializeField] private float baseMaxHP = 100f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseSpeed = 7f;
    [SerializeField] private float baseAttackSpeed = 1f; // Attacks per second
    [SerializeField] private float baseCritChance = 0.05f; // 5%
    [SerializeField] private float baseCritMultiplier = 2f; // 2x damage
    [SerializeField] private float baseArmor = 0f;
    
    
    // ========================================
    // CURRENT STATS (Recalculated when upgrades applied)
    // ========================================
    
    // Health
    public float currentMaxHP { get; private set; }
    
    // Damage
    public float currentDamage { get; private set; }
    public float currentCritChance { get; private set; }
    public float currentCritMultiplier { get; private set; }
    
    // Speed
    public float currentSpeed { get; private set; }
    public float currentAttackSpeed { get; private set; }
    public float currentDashCooldown { get; private set; }
    
    // Defense
    public float currentArmor { get; private set; }
    
    
    // ========================================
    // UPGRADE TRACKING
    // ========================================
    
    private List<UpgradeData> appliedUpgrades = new List<UpgradeData>();
    
    /// <summary>
    /// Get read-only list of all upgrades applied this run.
    /// </summary>
    public IReadOnlyList<UpgradeData> AppliedUpgrades => appliedUpgrades.AsReadOnly();
    
    
    // ========================================
    // CONSTRUCTOR
    // ========================================
    
    /// <summary>
    /// Initialize stats with default base values.
    /// </summary>
    public PlayerStats() {
        // Use default base values
        RecalculateStats();
    }
    
    /// <summary>
    /// Initialize stats with custom base values (for different characters).
    /// </summary>
    public PlayerStats(float maxHP, float damage, float speed, float attackSpeed) {
        baseMaxHP = maxHP;
        baseDamage = damage;
        baseSpeed = speed;
        baseAttackSpeed = attackSpeed;
        
        RecalculateStats();
    }
    
    
    // ========================================
    // UPGRADE SYSTEM
    // ========================================
    
    /// <summary>
    /// Apply an upgrade to the player's stats.
    /// Automatically recalculates all current stats.
    /// </summary>
    public void ApplyUpgrade(UpgradeData upgrade) {
        if (upgrade == null) {
            Debug.LogWarning("Tried to apply null upgrade!");
            return;
        }
        
        appliedUpgrades.Add(upgrade);
        RecalculateStats();
        
        Debug.Log($"Applied upgrade: {upgrade.upgradeName}");
    }
    
    /// <summary>
    /// Recalculate all current stats based on base stats + all applied upgrades.
    /// Call this after applying upgrades or changing base stats.
    /// </summary>
    private void RecalculateStats() {
        // Start with base values
        currentMaxHP = baseMaxHP;
        currentDamage = baseDamage;
        currentSpeed = baseSpeed;
        currentAttackSpeed = baseAttackSpeed;
        currentCritChance = baseCritChance;
        currentCritMultiplier = baseCritMultiplier;
        currentArmor = baseArmor;
        currentDashCooldown = 2f; // Default dash cooldown
        
        // Apply all upgrades
        foreach (UpgradeData upgrade in appliedUpgrades) {
            ApplyUpgradeModifiers(upgrade);
        }
        
        // Clamp values to reasonable ranges
        currentCritChance = Mathf.Clamp01(currentCritChance); // 0-100%
        currentSpeed = Mathf.Max(1f, currentSpeed); // Minimum speed
        currentAttackSpeed = Mathf.Max(0.1f, currentAttackSpeed); // Minimum attack speed
        currentDashCooldown = Mathf.Max(0.1f, currentDashCooldown); // Minimum cooldown
    }
    
    /// <summary>
    /// Apply a single upgrade's modifiers to current stats.
    /// </summary>
    private void ApplyUpgradeModifiers(UpgradeData upgrade) {
        // Additive bonuses (flat amounts)
        currentMaxHP += upgrade.healthBonus;
        currentDamage += upgrade.damageBonus;
        currentArmor += upgrade.armorBonus;
        
        // Multiplicative bonuses (percentages)
        currentDamage *= upgrade.damageMultiplier;
        currentSpeed *= upgrade.speedMultiplier;
        currentAttackSpeed *= upgrade.attackSpeedMultiplier;
        currentMaxHP *= upgrade.healthMultiplier;
        
        // Cooldown reduction (note: reduction, not multiplier)
        currentDashCooldown *= (1f - upgrade.cooldownReduction);
        
        // Crit stats
        currentCritChance += upgrade.critChanceBonus;
        currentCritMultiplier += upgrade.critDamageBonus;
    }
    
    
    // ========================================
    // RESET SYSTEM (For new runs)
    // ========================================
    
    /// <summary>
    /// Reset all stats to base values (use when starting new run).
    /// </summary>
    public void ResetStats() {
        appliedUpgrades.Clear();
        RecalculateStats();
        Debug.Log("Player stats reset to base values.");
    }
    
    
    // ========================================
    // UTILITY METHODS
    // ========================================
    
    /// <summary>
    /// Calculate final damage for an attack, including crit chance.
    /// </summary>
    public float CalculateDamage() {
        float damage = currentDamage;
        
        // Roll for crit
        if (UnityEngine.Random.value < currentCritChance) {
            damage *= currentCritMultiplier;
            Debug.Log($"CRIT! Damage: {damage}");
        }
        
        return damage;
    }
    
    /// <summary>
    /// Calculate damage reduction from armor.
    /// Formula: Damage Reduction = Armor / (Armor + 100)
    /// This gives diminishing returns (100 armor = 50% reduction, 200 = 66%, etc.)
    /// </summary>
    public float CalculateDamageReduction() {
        return currentArmor / (currentArmor + 100f);
    }
    
    /// <summary>
    /// Apply armor damage reduction to incoming damage.
    /// </summary>
    public float ApplyArmorToDamage(float incomingDamage) {
        float reduction = CalculateDamageReduction();
        float finalDamage = incomingDamage * (1f - reduction);
        return Mathf.Max(1f, finalDamage); // Minimum 1 damage
    }
    
    /// <summary>
    /// Get total number of upgrades applied this run.
    /// </summary>
    public int GetUpgradeCount() {
        return appliedUpgrades.Count;
    }
    
    /// <summary>
    /// Get number of upgrades of a specific type.
    /// </summary>
    public int GetUpgradeCountByType(UpgradeType type) {
        int count = 0;
        foreach (UpgradeData upgrade in appliedUpgrades) {
            if (upgrade.type == type) {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// Check if player has a specific upgrade.
    /// </summary>
    public bool HasUpgrade(UpgradeData upgrade) {
        return appliedUpgrades.Contains(upgrade);
    }
    
    
    // ========================================
    // DEBUG / SERIALIZATION
    // ========================================
    
    /// <summary>
    /// Get a formatted string of all current stats (for debug UI).
    /// </summary>
    public string GetStatsString() {
        return $"=== PLAYER STATS ===\n" +
               $"Max HP: {currentMaxHP:F0}\n" +
               $"Damage: {currentDamage:F1}\n" +
               $"Speed: {currentSpeed:F1}\n" +
               $"Attack Speed: {currentAttackSpeed:F2} attacks/sec\n" +
               $"Crit Chance: {currentCritChance * 100f:F1}%\n" +
               $"Crit Multiplier: {currentCritMultiplier:F1}x\n" +
               $"Armor: {currentArmor:F0} ({CalculateDamageReduction() * 100f:F1}% reduction)\n" +
               $"Dash Cooldown: {currentDashCooldown:F2}s\n" +
               $"Upgrades Applied: {appliedUpgrades.Count}";
    }
    
    /// <summary>
    /// Serialize stats for saving (returns data that can be saved to JSON).
    /// </summary>
    public PlayerStatsData Serialize() {
        return new PlayerStatsData {
            appliedUpgradeIDs = appliedUpgrades.ConvertAll(u => u.upgradeID)
        };
    }
    
    /// <summary>
    /// Deserialize stats from save data (rebuild stats from saved upgrade IDs).
    /// </summary>
    public void Deserialize(PlayerStatsData data, List<UpgradeData> allUpgrades) {
        appliedUpgrades.Clear();
        
        // Rebuild upgrades from IDs
        foreach (string upgradeID in data.appliedUpgradeIDs) {
            UpgradeData upgrade = allUpgrades.Find(u => u.upgradeID == upgradeID);
            if (upgrade != null) {
                appliedUpgrades.Add(upgrade);
            }
        }
        
        RecalculateStats();
    }
}


// ========================================
// SERIALIZATION DATA CLASS
// ========================================

/// <summary>
/// Data structure for saving/loading player stats.
/// </summary>
[System.Serializable]
public class PlayerStatsData {
    public List<string> appliedUpgradeIDs = new List<string>();
}