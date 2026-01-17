using UnityEngine;

/// <summary>
/// ScriptableObject that defines an upgrade's properties.
/// Create instances in Assets/_Project/Data/Upgrades/
/// Right-click → Create → Game → Upgrade Data
/// </summary>
[CreateAssetMenu(fileName = "New Upgrade", menuName = "Exterminator/Upgrade Data")]
public class UpgradeData : ScriptableObject {
    
    [Header("Identification")]
    [Tooltip("Unique ID for this upgrade (used for saving/loading)")]
    public string upgradeID;
    
    [Tooltip("Display name shown to player")]
    public string upgradeName;
    
    [TextArea(3, 5)]
    [Tooltip("Description shown in upgrade UI")]
    public string description;
    
    [Tooltip("Icon shown in upgrade UI")]
    public Sprite icon;
    
    [Header("Category")]
    [Tooltip("Power, Defense, or Utility (determines which pool this upgrade appears in)")]
    public UpgradeType type;
    
    [Header("Rarity")]
    [Tooltip("Common, Rare, or Epic (affects spawn chance)")]
    public UpgradeRarity rarity = UpgradeRarity.Common;
    
    
    // ========================================
    // STAT MODIFIERS
    // ========================================
    
    [Header("Health Modifiers")]
    [Tooltip("Flat HP bonus (e.g., +20 HP)")]
    public float healthBonus = 0f;
    
    [Tooltip("Multiplicative HP bonus (e.g., 1.1 = +10% max HP)")]
    public float healthMultiplier = 1f;
    
    [Header("Damage Modifiers")]
    [Tooltip("Flat damage bonus (e.g., +5 damage)")]
    public float damageBonus = 0f;
    
    [Tooltip("Multiplicative damage bonus (e.g., 1.2 = +20% damage)")]
    public float damageMultiplier = 1f;
    
    [Tooltip("Crit chance increase (e.g., 0.1 = +10% crit chance)")]
    public float critChanceBonus = 0f;
    
    [Tooltip("Crit damage multiplier increase (e.g., 0.5 = crit does +0.5x more damage)")]
    public float critDamageBonus = 0f;
    
    [Header("Speed Modifiers")]
    [Tooltip("Multiplicative movement speed (e.g., 1.15 = +15% speed)")]
    public float speedMultiplier = 1f;
    
    [Tooltip("Multiplicative attack speed (e.g., 1.2 = +20% faster attacks)")]
    public float attackSpeedMultiplier = 1f;
    
    [Tooltip("Cooldown reduction (e.g., 0.2 = -20% cooldowns)")]
    [Range(0f, 0.5f)]
    public float cooldownReduction = 0f;
    
    [Header("Defense Modifiers")]
    [Tooltip("Flat armor bonus (e.g., +10 armor)")]
    public float armorBonus = 0f;
    
    
    // ========================================
    // SPECIAL EFFECTS (Phase 2+)
    // ========================================
    
    [Header("Special Effects (Optional)")]
    [Tooltip("Does this upgrade have a special effect beyond stat changes?")]
    public bool hasSpecialEffect = false;
    
    [Tooltip("ID of special effect to apply (e.g., 'lifesteal', 'pierce', 'explode_on_kill')")]
    public string specialEffectID;
    
    [TextArea(2, 3)]
    [Tooltip("Description of special effect (for tooltip)")]
    public string specialEffectDescription;
    
    
    // ========================================
    // RESTRICTIONS (Optional)
    // ========================================
    
    [Header("Restrictions (Optional)")]
    [Tooltip("Maximum times this upgrade can be taken in one run (0 = unlimited)")]
    public int maxStacks = 0;
    
    [Tooltip("Upgrades that cannot be taken with this one (mutually exclusive)")]
    public UpgradeData[] conflictingUpgrades;
    
    [Tooltip("Upgrades required before this can appear")]
    public UpgradeData[] requiredUpgrades;
    
    
    // ========================================
    // UTILITY METHODS
    // ========================================
    
    /// <summary>
    /// Get formatted tooltip text for UI display.
    /// </summary>
    public string GetTooltipText() {
        string tooltip = $"<b>{upgradeName}</b>\n\n{description}\n\n";
        
        // Add stat changes
        if (healthBonus > 0) tooltip += $"+{healthBonus} Max HP\n";
        if (healthMultiplier != 1f) tooltip += $"+{(healthMultiplier - 1f) * 100f:F0}% Max HP\n";
        if (damageBonus > 0) tooltip += $"+{damageBonus} Damage\n";
        if (damageMultiplier != 1f) tooltip += $"+{(damageMultiplier - 1f) * 100f:F0}% Damage\n";
        if (critChanceBonus > 0) tooltip += $"+{critChanceBonus * 100f:F0}% Crit Chance\n";
        if (critDamageBonus > 0) tooltip += $"+{critDamageBonus * 100f:F0}% Crit Damage\n";
        if (speedMultiplier != 1f) tooltip += $"+{(speedMultiplier - 1f) * 100f:F0}% Move Speed\n";
        if (attackSpeedMultiplier != 1f) tooltip += $"+{(attackSpeedMultiplier - 1f) * 100f:F0}% Attack Speed\n";
        if (cooldownReduction > 0) tooltip += $"-{cooldownReduction * 100f:F0}% Cooldowns\n";
        if (armorBonus > 0) tooltip += $"+{armorBonus} Armor\n";
        
        // Add special effect description
        if (hasSpecialEffect && !string.IsNullOrEmpty(specialEffectDescription)) {
            tooltip += $"\n<i>{specialEffectDescription}</i>";
        }
        
        return tooltip;
    }
    
    /// <summary>
    /// Check if this upgrade can be taken (based on restrictions).
    /// </summary>
    public bool CanBeTaken(PlayerStats stats) {
        // Check max stacks
        if (maxStacks > 0 && stats.HasUpgrade(this)) {
            int currentStacks = 0;
            foreach (UpgradeData upgrade in stats.AppliedUpgrades) {
                if (upgrade == this) currentStacks++;
            }
            if (currentStacks >= maxStacks) return false;
        }
        
        // Check conflicting upgrades
        foreach (UpgradeData conflict in conflictingUpgrades) {
            if (stats.HasUpgrade(conflict)) return false;
        }
        
        // Check required upgrades
        foreach (UpgradeData required in requiredUpgrades) {
            if (!stats.HasUpgrade(required)) return false;
        }
        
        return true;
    }
    
    
    // ========================================
    // VALIDATION (Editor only)
    // ========================================
    
    #if UNITY_EDITOR
    void OnValidate() {
        // Auto-generate ID from name if empty
        if (string.IsNullOrEmpty(upgradeID)) {
            upgradeID = name.ToLower().Replace(" ", "_");
        }
        
        // Ensure multipliers are at least 1 (or 0 for reduction)
        if (damageMultiplier < 0) damageMultiplier = 1f;
        if (speedMultiplier < 0) speedMultiplier = 1f;
        if (attackSpeedMultiplier < 0) attackSpeedMultiplier = 1f;
        if (healthMultiplier < 0) healthMultiplier = 1f;
    }
    #endif
}


// ========================================
// ENUMS
// ========================================

/// <summary>
/// Upgrade category (determines when/how it appears in upgrade choices).
/// </summary>
public enum UpgradeType {
    Power,      // Damage, attack speed, crit
    Defense,    // HP, armor, resistances
    Utility     // Movement speed, cooldowns, special effects
}

/// <summary>
/// Upgrade rarity (affects spawn chance).
/// </summary>
public enum UpgradeRarity {
    Common,     // 70% spawn chance
    Rare,       // 25% spawn chance
    Epic        // 5% spawn chance
}