using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages upgrade selection, validation, and application.
/// Checks for required upgrades (prerequisites) and conflicting upgrades (mutually exclusive).
/// </summary>
public class UpgradeManager : MonoBehaviour {
    
    [Header("Upgrade Pool")]
    [Tooltip("All available upgrades in the game. Assign all UpgradeData assets here.")]
    [SerializeField] private UpgradeData[] allUpgrades;
    
    [Header("Settings")]
    [Tooltip("Number of upgrade choices offered to player")]
    [SerializeField] private int upgradeChoiceCount = 3;
    
    public static UpgradeManager Instance { get; private set; }
    
    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    // ========================================
    // UPGRADE POOL SELECTION
    // ========================================
    public UpgradeData[] GetAllUpgrades() {
        return allUpgrades;
    }

    public List<UpgradeData> GetUpgradesByTypeAndCount(UpgradeType type, int count, List<UpgradeData> outputList) {
        if (outputList == null) return null;
        outputList.Clear();
        
        if (allUpgrades == null || allUpgrades.Length == 0 || count <= 0) return outputList;
        
        PlayerController player = PlayerController.Instance;
        if (player == null || player.stats == null) return outputList;
        
        // Build pool of matching type that can actually be taken
        List<UpgradeData> pool = new List<UpgradeData>();
        foreach (UpgradeData u in allUpgrades) {
            if (u != null && u.type == type && CanTakeUpgrade(u, player.stats)) {
                pool.Add(u);
            }
        }

        if (pool.Count == 0) return outputList;

        // Select up to count items, weighted by rarity. Avoid duplicates unless stackable.
        for (int i = 0; i < count && pool.Count > 0; i++) {
            UpgradeData selected = SelectWeightedUpgrade(pool);
            if (selected == null) break;
            outputList.Add(selected);

            // Remove non-stackable upgrades or upgrades at max stacks to prevent duplicates
            if (selected.maxStacks <= 1) {
                pool.Remove(selected);
            } else {
                // Check if we can still take this stackable upgrade
                int currentStacks = GetUpgradeStackCount(selected, player.stats) + 1; // +1 because we just added it
                if (currentStacks >= selected.maxStacks) {
                    pool.Remove(selected);
                }
            }
        }
        return outputList;
    }


    // ========================================
    // UPGRADE APPLICATION
    // ========================================
    
    /// <summary>
    /// Apply the chosen upgrade to the player after validation.
    /// </summary>
    public void ApplyUpgrade(UpgradeData upgrade) {
        PlayerController player = PlayerController.Instance;
        if (player == null) {
            GameEvents.DebugWarning("PlayerController instance not found. Cannot apply upgrade.", DebugCategory.Upgrades);
            return;
        }
        
        // Validate upgrade can be taken
        if (!CanTakeUpgrade(upgrade, player.stats)) {
            GameEvents.DebugWarning($"Cannot apply upgrade '{upgrade.upgradeName}': requirements not met.");
            return;
        }
        
        player.stats.ApplyUpgrade(upgrade);
        GameEvents.UpgradeApplied(upgrade);
        GameEvents.DebugLog($"Applied upgrade: {upgrade.upgradeName}", DebugCategory.Upgrades);
    }
    
    
    // ========================================
    // UPGRADE VALIDATION
    // ========================================
    
    /// <summary>
    /// Check if a specific upgrade can be taken by the player.
    /// Validates max stacks, required upgrades (prerequisites), and conflicting upgrades.
    /// </summary>
    public bool CanTakeUpgrade(UpgradeData upgrade, PlayerStats stats) {
        if (upgrade == null || stats == null) return false;
        
        // Use the UpgradeData's built-in validation
        return upgrade.CanBeTaken(stats);
    }
    
    /// <summary>
    /// Check if required upgrades (prerequisites) are satisfied.
    /// </summary>
    public bool HasRequiredUpgrades(UpgradeData upgrade, PlayerStats stats) {
        if (upgrade == null || stats == null) return false;
        if (upgrade.requiredUpgrades == null || upgrade.requiredUpgrades.Length == 0) return true;
        
        foreach (UpgradeData required in upgrade.requiredUpgrades) {
            if (required != null && !stats.HasUpgrade(required)) {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Check if any conflicting upgrades have been taken.
    /// </summary>
    public bool HasConflictingUpgrade(UpgradeData upgrade, PlayerStats stats) {
        if (upgrade == null || stats == null) return false;
        if (upgrade.conflictingUpgrades == null || upgrade.conflictingUpgrades.Length == 0) return false;
        
        foreach (UpgradeData conflict in upgrade.conflictingUpgrades) {
            if (conflict != null && stats.HasUpgrade(conflict)) {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Get the reason why an upgrade cannot be taken (for UI display).
    /// Returns null if upgrade can be taken.
    /// </summary>
    public string GetUpgradeBlockedReason(UpgradeData upgrade, PlayerStats stats) {
        if (upgrade == null) return "Invalid upgrade";
        if (stats == null) return "No player stats";
        
        // Check max stacks
        if (upgrade.maxStacks > 0) {
            int currentStacks = GetUpgradeStackCount(upgrade, stats);
            if (currentStacks >= upgrade.maxStacks) {
                return $"Max stacks reached ({currentStacks}/{upgrade.maxStacks})";
            }
        }
        
        // Check required upgrades
        if (upgrade.requiredUpgrades != null && upgrade.requiredUpgrades.Length > 0) {
            List<string> missingUpgrades = new List<string>();
            foreach (UpgradeData required in upgrade.requiredUpgrades) {
                if (required != null && !stats.HasUpgrade(required)) {
                    missingUpgrades.Add(required.upgradeName);
                }
            }
            if (missingUpgrades.Count > 0) {
                return $"Requires: {string.Join(", ", missingUpgrades)}";
            }
        }
        
        // Check conflicting upgrades
        if (upgrade.conflictingUpgrades != null && upgrade.conflictingUpgrades.Length > 0) {
            foreach (UpgradeData conflict in upgrade.conflictingUpgrades) {
                if (conflict != null && stats.HasUpgrade(conflict)) {
                    return $"Conflicts with: {conflict.upgradeName}";
                }
            }
        }
        
        return null; // No issues, upgrade can be taken
    }
    
    /// <summary>
    /// Get how many times the player has taken a specific upgrade.
    /// </summary>
    public int GetUpgradeStackCount(UpgradeData upgrade, PlayerStats stats) {
        if (upgrade == null || stats == null) return 0;
        
        int count = 0;
        foreach (UpgradeData applied in stats.AppliedUpgrades) {
            if (applied == upgrade) count++;
        }
        return count;
    }
    
    
    // ========================================
    // UPGRADE SELECTION / OFFERING
    // ========================================
    
    /// <summary>
    /// Get all upgrades that can currently be taken by the player.
    /// Filters out upgrades blocked by requirements, conflicts, or max stacks.
    /// </summary>
    public List<UpgradeData> GetAvailableUpgrades(PlayerStats stats) {
        List<UpgradeData> available = new List<UpgradeData>();
        
        if (allUpgrades == null || stats == null) return available;
        
        foreach (UpgradeData upgrade in allUpgrades) {
            if (upgrade != null && CanTakeUpgrade(upgrade, stats)) {
                available.Add(upgrade);
            }
        }
        
        return available;
    }
    
    /// <summary>
    /// Get available upgrades filtered by type (Power, Defense, Utility).
    /// </summary>
    public List<UpgradeData> GetAvailableUpgradesByType(PlayerStats stats, UpgradeType type) {
        List<UpgradeData> available = GetAvailableUpgrades(stats);
        available.RemoveAll(u => u.type != type);
        return available;
    }
    
    /// <summary>
    /// Get available upgrades filtered by rarity.
    /// </summary>
    public List<UpgradeData> GetAvailableUpgradesByRarity(PlayerStats stats, UpgradeRarity rarity) {
        List<UpgradeData> available = GetAvailableUpgrades(stats);
        available.RemoveAll(u => u.rarity != rarity);
        return available;
    }
    
    /// <summary>
    /// Offer random upgrade choices to the player.
    /// Respects rarity weights: Common (70%), Rare (25%), Epic (5%).
    /// </summary>
    public UpgradeData[] GetRandomUpgradeChoices(PlayerStats stats) {
        List<UpgradeData> available = GetAvailableUpgrades(stats);
        List<UpgradeData> choices = new List<UpgradeData>();
        
        if (available.Count == 0) {
            GameEvents.DebugWarning("No available upgrades to offer!", DebugCategory.Upgrades);
            return choices.ToArray();
        }
        
        // Select random upgrades (weighted by rarity)
        for (int i = 0; i < upgradeChoiceCount && available.Count > 0; i++) {
            UpgradeData selected = SelectWeightedUpgrade(available);
            if (selected != null) {
                choices.Add(selected);
                
                // Remove from pool to avoid duplicates (unless stackable)
                if (selected.maxStacks <= 1) {
                    available.Remove(selected);
                }
            }
        }
        
        return choices.ToArray();
    }
    
    /// <summary>
    /// Select a random upgrade weighted by rarity.
    /// Common: 70%, Rare: 25%, Epic: 5%
    /// </summary>
    private UpgradeData SelectWeightedUpgrade(List<UpgradeData> upgrades) {
        if (upgrades == null || upgrades.Count == 0) return null;
        
        // Build weighted list
        List<(UpgradeData upgrade, float weight)> weighted = new List<(UpgradeData, float)>();
        float totalWeight = 0f;
        
        foreach (UpgradeData upgrade in upgrades) {
            float weight = GetRarityWeight(upgrade.rarity);
            weighted.Add((upgrade, weight));
            totalWeight += weight;
        }
        
        // Random selection
        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        
        foreach (var (upgrade, weight) in weighted) {
            cumulative += weight;
            if (roll <= cumulative) {
                return upgrade;
            }
        }
        
        // Fallback (shouldn't happen)
        return upgrades[Random.Range(0, upgrades.Count)];
    }
    
    /// <summary>
    /// Get spawn weight for a rarity tier.
    /// </summary>
    private float GetRarityWeight(UpgradeRarity rarity) {
        return rarity switch {
            UpgradeRarity.Common => 70f,
            UpgradeRarity.Rare => 25f,
            UpgradeRarity.Epic => 5f,
            _ => 50f
        };
    }
    
    
    // ========================================
    // DEBUG / UTILITY
    // ========================================
    
    /// <summary>
    /// Log all available upgrades and their status for debugging.
    /// </summary>
    [ContextMenu("Debug: Log Upgrade Status")]
    public void DebugLogUpgradeStatus() {
        PlayerController player = PlayerController.Instance;
        if (player == null) {
            GameEvents.DebugWarning("No player found for upgrade status check.", DebugCategory.Upgrades);
            return;
        }
        
        GameEvents.DebugLog("=== UPGRADE STATUS ===", DebugCategory.Upgrades);
        
        if (allUpgrades == null || allUpgrades.Length == 0) {
            GameEvents.DebugLog("No upgrades in pool!", DebugCategory.Upgrades);
            return;
        }
        
        foreach (UpgradeData upgrade in allUpgrades) {
            if (upgrade == null) continue;
            
            bool canTake = CanTakeUpgrade(upgrade, player.stats);
            string reason = GetUpgradeBlockedReason(upgrade, player.stats);
            string status = canTake ? "AVAILABLE" : $"BLOCKED - {reason}";
            
            GameEvents.DebugLog($"[{upgrade.rarity}] {upgrade.upgradeName}: {status}", DebugCategory.Upgrades   );
        }
    }
}