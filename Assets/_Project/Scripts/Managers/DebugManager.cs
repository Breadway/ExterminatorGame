using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized debug logging manager with category-based filtering.
/// Provides granular control over what types of debug messages are shown.
/// </summary>
public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }
    
    [Header("Master Debug Toggle")]
    [SerializeField] private bool masterDebugEnabled = true;
    
    [Header("Debug Categories")]
    [SerializeField] private bool logCombat = true;
    [SerializeField] private bool logPlayerStats = true;
    [SerializeField] private bool logEnemyAI = false;
    [SerializeField] private bool logUpgrades = true;
    [SerializeField] private bool logXPAndLeveling = true;
    [SerializeField] private bool logRooms = true;
    [SerializeField] private bool logAudio = false;
    [SerializeField] private bool logUI = false;
    [SerializeField] private bool logInput = false;
    [SerializeField] private bool logPerformance = false;
    [SerializeField] private bool logGeneral = true;
    
    // Specific script-level toggles (granular filtering)
    [Header("Script-Level Debug Toggles (Specific) ")]
    [SerializeField] private bool logFlamethrowerAttack = true;
    [SerializeField] private bool logStatusEffectController = true;
    [SerializeField] private bool logActiveStatusEffect = true;
    [SerializeField] private bool logHealth = true;
    [SerializeField] private bool logEnemy = true;
    [SerializeField] private bool logPlayer = true;
    [SerializeField] private bool logGameEvents = true;
    [SerializeField] private bool logPooling = true;
    [SerializeField] private bool logSpawnManager = true;
    [SerializeField] private bool logUpgradeManager = true;
    [SerializeField] private bool logAudioManager = true;
    [SerializeField] private bool logUIManager = true;
    [SerializeField] private bool logInputManager = true;

    private Dictionary<DebugCategory, bool> categoryStates;

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCategoryStates();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeCategoryStates()
    {
        categoryStates = new Dictionary<DebugCategory, bool>
        {
            { DebugCategory.Combat, logCombat },
            { DebugCategory.PlayerStats, logPlayerStats },
            { DebugCategory.EnemyAI, logEnemyAI },
            { DebugCategory.Upgrades, logUpgrades },
            { DebugCategory.XPAndLeveling, logXPAndLeveling },
            { DebugCategory.Rooms, logRooms },
            { DebugCategory.Audio, logAudio },
            { DebugCategory.UI, logUI },
            { DebugCategory.Input, logInput },
            { DebugCategory.Performance, logPerformance },
            { DebugCategory.General, logGeneral },

            // Specific script categories
            { DebugCategory.FlamethrowerAttack, logFlamethrowerAttack },
            { DebugCategory.StatusEffectController, logStatusEffectController },
            { DebugCategory.ActiveStatusEffect, logActiveStatusEffect },
            { DebugCategory.Health, logHealth },
            { DebugCategory.Enemy, logEnemy },
            { DebugCategory.Player, logPlayer },
            { DebugCategory.GameEvents, logGameEvents },
            { DebugCategory.Pooling, logPooling },
            { DebugCategory.SpawnManager, logSpawnManager },
            { DebugCategory.UpgradeManager, logUpgradeManager },
            { DebugCategory.AudioManager, logAudioManager },
            { DebugCategory.UIManager, logUIManager },
            { DebugCategory.InputManager, logInputManager }
        };
    }

    /// <summary>
    /// Check if a specific debug category is enabled.
    /// </summary>
    public bool IsCategoryEnabled(DebugCategory category)
    {
        if (!masterDebugEnabled) return false;
        
        if (categoryStates.TryGetValue(category, out bool isEnabled))
        {
            return isEnabled;
        }
        
        // Default to true for unknown categories
        return true;
    }

    /// <summary>
    /// Enable or disable a specific debug category at runtime.
    /// </summary>
    public void SetCategoryEnabled(DebugCategory category, bool isEnabled)
    {
        if (categoryStates.ContainsKey(category))
        {
            categoryStates[category] = isEnabled;
            
            // Update serialized field for Inspector visibility
            switch (category)
            {
                case DebugCategory.Combat: logCombat = isEnabled; break;
                case DebugCategory.PlayerStats: logPlayerStats = isEnabled; break;
                case DebugCategory.EnemyAI: logEnemyAI = isEnabled; break;
                case DebugCategory.Upgrades: logUpgrades = isEnabled; break;
                case DebugCategory.XPAndLeveling: logXPAndLeveling = isEnabled; break;
                case DebugCategory.Rooms: logRooms = isEnabled; break;
                case DebugCategory.Audio: logAudio = isEnabled; break;
                case DebugCategory.UI: logUI = isEnabled; break;
                case DebugCategory.Input: logInput = isEnabled; break;
                case DebugCategory.Performance: logPerformance = isEnabled; break;
                case DebugCategory.General: logGeneral = isEnabled; break;

                // Specific scripts
                case DebugCategory.FlamethrowerAttack: logFlamethrowerAttack = isEnabled; break;
                case DebugCategory.StatusEffectController: logStatusEffectController = isEnabled; break;
                case DebugCategory.ActiveStatusEffect: logActiveStatusEffect = isEnabled; break;
                case DebugCategory.Health: logHealth = isEnabled; break;
                case DebugCategory.Enemy: logEnemy = isEnabled; break;
                case DebugCategory.Player: logPlayer = isEnabled; break;
                case DebugCategory.GameEvents: logGameEvents = isEnabled; break;
                case DebugCategory.Pooling: logPooling = isEnabled; break;
                case DebugCategory.SpawnManager: logSpawnManager = isEnabled; break;
                case DebugCategory.UpgradeManager: logUpgradeManager = isEnabled; break;
                case DebugCategory.AudioManager: logAudioManager = isEnabled; break;
                case DebugCategory.UIManager: logUIManager = isEnabled; break;
                case DebugCategory.InputManager: logInputManager = isEnabled; break;
            }
        }
    }
    
    /// <summary>
    /// Toggle master debug on/off.
    /// </summary>
    public void SetMasterDebugEnabled(bool enabled)
    {
        masterDebugEnabled = enabled;
    }
    
    /// <summary>
    /// Enable all debug categories.
    /// </summary>
    public void EnableAllCategories()
    {
        foreach (DebugCategory category in Enum.GetValues(typeof(DebugCategory)))
        {
            SetCategoryEnabled(category, true);
        }
    }
    
    /// <summary>
    /// Disable all debug categories.
    /// </summary>
    public void DisableAllCategories()
    {
        foreach (DebugCategory category in Enum.GetValues(typeof(DebugCategory)))
        {
            SetCategoryEnabled(category, false);
        }
    }
    
    /// <summary>
    /// Get a formatted prefix for log messages based on category.
    /// </summary>
    public string GetCategoryPrefix(DebugCategory category)
    {
        return $"[{category}]";
    }
}

/// <summary>
/// Debug log categories for filtering.
/// Add new categories as needed for different systems.
/// </summary>
public enum DebugCategory
{
    Combat,          // Weapon attacks, damage, hit detection
    PlayerStats,     // Stat changes, upgrades applied to player
    EnemyAI,         // Enemy behavior, pathfinding, state changes
    Upgrades,        // Upgrade selection, application
    XPAndLeveling,   // XP gain, level ups
    Rooms,           // Room clearing, progression
    Audio,           // Sound effects, music
    UI,              // UI interactions, updates
    Input,           // Input events, controls
    Performance,     // Performance metrics, optimization
    General,         // Miscellaneous logs that don't fit other categories

    // Specific categories for granular control
    FlamethrowerAttack,      // Flamethrower attack logs
    StatusEffectController,  // Status effect application and removal
    ActiveStatusEffect,      // Active status effect updates
    Health,                  // Health-related logs
    Enemy,                   // Enemy-specific logs
    Player,                  // Player-specific logs
    GameEvents,             // Game event triggers and responses
    Pooling,                 // Object pooling logs
    SpawnManager,           // Enemy and item spawn events
    UpgradeManager,         // Upgrade application and effects
    AudioManager,           // Audio playback and management
    UIManager,              // UI updates and interactions
    InputManager             // Input handling and events
}
