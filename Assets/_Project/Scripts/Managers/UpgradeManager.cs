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
    
    [Header("Level Paths")]
    [Tooltip("Path assets (PathData). Tier 1 is offered at level 10, tier 2 at 20, tier 3 at 30 (endless only).")]
    [SerializeField] private PathData[] allPaths;
    [SerializeField] private int tier1Level = 10;
    [SerializeField] private int tier2Level = 20;
    [SerializeField] private int tier3Level = 30;

    [Header("Settings")]
    [Tooltip("Number of upgrade choices offered to player")]
    [SerializeField] private int upgradeChoiceCount = 3;
    
    public static UpgradeManager Instance { get; private set; }
    private readonly Queue<int> pendingLevels = new Queue<int>();
    private UpgradeData[] currentChoices = System.Array.Empty<UpgradeData>();
    private bool runEnded;
    private bool selecting;
    public IReadOnlyList<UpgradeData> CurrentChoices => System.Array.AsReadOnly(currentChoices);
    public bool IsChoosing => currentChoices.Length > 0;

    void OnEnable() {
        GameEvents.OnLevelUp += QueueUpgrade;
        GameEvents.OnRunEnded += HandleRunEnded;
    }

    void OnDisable() {
        GameEvents.OnLevelUp -= QueueUpgrade;
        GameEvents.OnRunEnded -= HandleRunEnded;
        pendingLevels.Clear();
        currentChoices = System.Array.Empty<UpgradeData>();
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    void HandleRunEnded(bool victory) {
        runEnded = true;
        pendingLevels.Clear();
        currentChoices = System.Array.Empty<UpgradeData>();
    }

    void QueueUpgrade(int level) {
        if (runEnded) return;
        pendingLevels.Enqueue(level);
        OfferNextUpgrade();
    }

    void OfferNextUpgrade() {
        if (IsChoosing || runEnded) return;
        while (pendingLevels.Count > 0) {
            int level = pendingLevels.Dequeue();
            bool bonus = level < 0;
            level = Mathf.Abs(level);
            if (!bonus && TryGrantEndlessBoon(level)) continue;
            var stats = PlayerController.Instance?.stats;
            currentChoices = (bonus ? null : PathChoicesForLevel(level, stats)) ?? GetRandomUpgradeChoices(stats);
            if (!IsChoosing) continue;
            // Keep the keyboard adapter available even when no visual UI exists.
            Time.timeScale = 0f;
            GameEvents.ChangeGameState(GameState.UpgradeChoice);
            // Clone so listeners cannot replace the authoritative choices.
            GameEvents.OfferUpgrades(level, (UpgradeData[])currentChoices.Clone());
            return;
        }
    }

    private bool endless;

    /// <summary>Queue one extra upgrade choice outside of a level-up (shop rooms).</summary>
    public void OfferBonusChoice() {
        int level = XPManager.Instance != null ? XPManager.Instance.GetCurrentLevel() : 1;
        // Use a level that can never be a path level so a bonus is always a normal upgrade.
        QueueUpgrade(-level);
    }

    /// <summary>Re-open the choice flow after a victory, for endless mode (adds tier 3 paths).</summary>
    public void ResumeAfterVictory() {
        runEnded = false;
        endless = true;
    }

    private int PathTierForLevel(int level) {
        if (level == tier1Level) return 1;
        if (level == tier2Level) return 2;
        if (level == tier3Level && endless) return 3;
        return 0;
    }

    /// <summary>Paths available at this level: the right tier, prerequisites met, no conflicts.</summary>
    private UpgradeData[] PathChoicesForLevel(int level, PlayerStats stats) {
        int tier = PathTierForLevel(level);
        if (tier == 0 || allPaths == null || stats == null) return null;
        string character = PlayerController.Instance != null ? PlayerController.Instance.CharacterId : "";
        var options = new List<UpgradeData>();
        foreach (var path in allPaths) {
            if (path == null || path.tier != tier || options.Contains(path)) continue;
            if (!string.IsNullOrEmpty(path.characterId) && path.characterId != character) continue;
            if (CanTakeUpgrade(path, stats)) options.Add(path);
        }
        return options.Count > 0 ? options.ToArray() : null;
    }

    /// <summary>Level 31+ endless boon: a small automatic character-specific buff each level, no choice UI.</summary>
    private bool TryGrantEndlessBoon(int level) {
        if (!endless || level <= tier3Level) return false;
        var player = PlayerController.Instance;
        if (player == null || player.stats == null) return false;
        string character = player.CharacterId;
        // Same shape for everyone, tuned per character: damage for strikers, durability for tanks
        float damage = 1.05f, attack = 1.03f, speed = 1.03f;
        switch (character) {
            case "custodian": damage = 1.06f; speed = 1.02f; break;
            case "intern": damage = 1.03f; speed = 1.02f; break;
            case "electrician": attack = 1.05f; break;
            case "scientist": damage = 1.07f; attack = 1.02f; break;
            case "apiarist": attack = 1.04f; speed = 1.04f; break;
        }
        player.stats.SetBuff("endless" + level, speed, attack, damage);
        player.GetHealth(out var health);
        if (health != null) health.Heal(health.MaxHP * 0.1f);
        GameEvents.PlayerStatsChanged(player.stats);
        GameEvents.ShowNotification("OVERTIME BONUS");
        return true;
    }

    /// <summary>True while the current offer is a path choice, so the UI can label it.</summary>
    public bool CurrentChoicesArePaths {
        get {
            foreach (var c in currentChoices) if (c is PathData) return true;
            return false;
        }
    }

    /// <summary>UI-independent selection API. Rejects stale or unoffered choices.</summary>
    public bool ChooseUpgrade(int index) {
        if (runEnded || selecting || index < 0 || index >= currentChoices.Length) return false;
        var choice = currentChoices[index];
        var player = PlayerController.Instance;
        if (player == null || !CanTakeUpgrade(choice, player.stats)) return false;
        selecting = true;
        try {
            ApplyUpgrade(choice);
        } finally {
            selecting = false;
        }
        currentChoices = System.Array.Empty<UpgradeData>();
        OfferNextUpgrade();
        if (!IsChoosing && !runEnded) {
            Time.timeScale = 1f;
            GameEvents.ChangeGameState(GameState.Playing);
        }
        return true;
    }
    
    void Awake() {
        if (Instance == null) {
            Instance = this;
            // Provide nonvisual keyboard selection until a designed view is connected.
            if (GetComponent<UpgradeUI>() == null) gameObject.AddComponent<UpgradeUI>();
        } else {
            Destroy(gameObject);
            return;
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
            if (u != null && u.type == type && CanTakeUpgrade(u, player.stats) && !pool.Contains(u)) {
                pool.Add(u);
            }
        }

        if (pool.Count == 0) return outputList;

        // Select up to count items, weighted by rarity. Avoid duplicates unless stackable.
        for (int i = 0; i < count && pool.Count > 0; i++) {
            UpgradeData selected = SelectWeightedUpgrade(pool);
            if (selected == null) break;
            outputList.Add(selected);

            // Choices are alternatives, not stacks being acquired together.
            pool.Remove(selected);
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
        if (upgrade == null || runEnded) return;
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
            if (upgrade != null && CanTakeUpgrade(upgrade, stats) && !available.Contains(upgrade)) {
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
                available.Remove(selected);
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
