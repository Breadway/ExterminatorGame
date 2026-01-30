using System;
using UnityEngine;

/// <summary>
/// Centralized event system for cross-system communication.
/// Decouples systems so they don't need direct references to each other.
/// </summary>
public static class GameEvents
{
    // ========== PLAYER EVENTS ==========
    
    /// <summary>
    /// Fired when player movement input changes.
    /// Allows systems to react to movement input independently (animation, sfx, etc).
    /// </summary>
    public static Action<Vector2> OnPlayerMovementInput;
    
    /// <summary>
    /// Fired when player rotation/aiming changes.
    /// Allows systems to react to aim updates (animation, UI, etc).
    /// </summary>
    public static Action<Vector2> OnPlayerAimDirectionChanged;

    public static Action<Vector2> OnPlayerDash;

    /// <summary>
    /// Fired when player selects an upgrade from the upgrade choice UI.   
    /// Parameters: chosen UpgradeData
    /// Listeners: Player (apply upgrade), UI (close upgrade panel), AudioManager (play selection sound)
    /// </summary>
    public static Action<UpgradeData> OnPlayerUpgradeApplied;

    
    // ========================================
    // COMBAT EVENTS
    // ========================================
    
    /// <summary>
    /// Fired when an enemy is killed. Passes the enemy that died.
    /// Listeners: XPManager (award XP), RoomManager (check room clear), AudioManager (death sound), UI (kill counter)
    /// </summary>
    public static Action<Enemy> OnEnemyKilled;
    
    /// <summary>
    /// Fired when an enemy takes damage (non-fatal).
    /// Parameters: damage amount, hit position, hit direction
    /// Listeners: VFXManager (spawn damage numbers), AudioManager (hit sound)
    /// </summary>
    public static event Action<float, Vector2, Vector2> OnEnemyDamaged;
    
    /// <summary>
    /// Fired when player takes damage.
    /// Parameters: damage amount, current HP, max HP
    /// Listeners: HealthBarUI (update bar), ScreenEffects (flash red), AudioManager (hurt sound)
    /// </summary>
    public static Action<float, float, float> OnPlayerDamaged;
    
    /// <summary>
    /// Fired when player is healed.
    /// Parameters: heal amount
    /// Listeners: HealthBarUI (update bar), VFXManager (heal particles), AudioManager (heal sound)
    /// </summary>
    public static event Action<float> OnPlayerHealed;
    
    /// <summary>
    /// Fired when player dies.
    /// Listeners: GameManager (game over screen), AudioManager (death sound), InputManager (disable input)
    /// </summary>
    public static event Action OnPlayerDied;
    
    
    // ========================================
    // STATUS EFFECT EVENTS
    // ========================================
    
    /// <summary>
    /// Fired when a status effect is applied to an entity.
    /// Parameters: affected GameObject, effect data
    /// Listeners: VFXManager (spawn effect visuals), AudioManager (play effect sound), UI (show status icon)
    /// </summary>
    public static Action<GameObject, StatusEffectData> OnStatusEffectApplied;
    
    /// <summary>
    /// Fired when a status effect is removed from an entity.
    /// Parameters: affected GameObject, effect data
    /// Listeners: VFXManager (clean up visuals), UI (remove status icon)
    /// </summary>
    public static Action<GameObject, StatusEffectData> OnStatusEffectRemoved;
    
    
    // ========================================
    // PROGRESSION EVENTS
    // ========================================
    
    /// <summary>
    /// Fired when player gains XP.
    /// Parameters: XP amount gained
    /// Listeners: XPBarUI (update bar), VFXManager (XP gain popup)
    /// </summary>
    public static System.Action<int, int> OnXPChanged; // (currentXP, xpRequired)
    
    /// <summary>
    /// Fired when player levels up.
    /// Parameters: new level
    /// Listeners: Player (heal on level up), UI (level up popup), PathSystem (check for path unlock at 10/20/30)
    /// </summary>
    public static System.Action<int> OnLevelUp; // (newLevel)
    
    /// <summary>
    /// Fired when upgrade choices are offered to player.
    /// Parameters: current level, array of 3 upgrade options
    /// Listeners: UpgradeUI (display choices)
    /// </summary>
    public static event Action<int, UpgradeData[]> OnUpgradeOffered;

    public static event Action<float, float> OnHealthChanged; // (currentHP, maxHP)
    
    /// <summary>
    /// Fired when player's stats change (from upgrades, buffs, debuffs).
    /// Parameters: updated PlayerStats
    /// Listeners: CharacterSheetUI (update display), SaveManager (track current stats)
    /// </summary>
    public static event Action<PlayerStats> OnPlayerStatsChanged;
    
    
    // ========================================
    // ROOM EVENTS
    // ========================================
    
    /// <summary>
    /// Fired when player enters a new room.
    /// Parameters: room index (0-24)
    /// Listeners: RoomManager (spawn enemies), UI (update room counter), MusicManager (change music)
    /// </summary>
    public static event Action<int> OnRoomEntered;
    
    /// <summary>
    /// Fired when all enemies in a room are cleared.
    /// Parameters: room index
    /// Listeners: UpgradeManager (show upgrade choices), DoorController (unlock doors), SaveManager (save progress)
    /// </summary>
    public static event Action<int> OnRoomCleared;
    
    /// <summary>
    /// Fired when player enters the boss room.
    /// Listeners: MusicManager (boss music), UI (boss health bar), CameraController (zoom out)
    /// </summary>
    public static event Action OnBossRoomEntered;
    
    /// <summary>
    /// Fired when a boss is defeated.
    /// Parameters: boss identifier (mid-boss or final boss)
    /// Listeners: GameManager (check win condition), UI (victory screen), SaveManager (unlock endless mode)
    /// </summary>
    public static event Action<string> OnBossDefeated;
    
    
    // ========================================
    // GAME STATE EVENTS
    // ========================================
    
    /// <summary>
    /// Fired when game state changes (menu, playing, paused, game over, etc.).
    /// Parameters: new game state
    /// Listeners: UI (show/hide panels), AudioManager (pause music), InputManager (enable/disable input)
    /// </summary>
    public static event Action<GameState> OnGameStateChanged;
    
    /// <summary>
    /// Fired when a new run starts.
    /// Listeners: All managers (reset state), SaveManager (create new run data), UI (reset displays)
    /// </summary>
    public static event Action OnRunStarted;
    
    /// <summary>
    /// Fired when run ends (win or loss).
    /// Parameters: was victory (true = won, false = died)
    /// Listeners: GameManager (show results screen), SaveManager (record run stats), MetaProgression (award currency)
    /// </summary>
    public static event Action<bool> OnRunEnded;
    
    
    // ========================================
    // UI EVENTS
    // ========================================
    
    /// <summary>
    /// Fired when a notification should be shown to player.
    /// Parameters: notification text
    /// Listeners: NotificationUI (display popup)
    /// </summary>
    public static event Action<string> OnShowNotification;
    
    /// <summary>
    /// Fired when game is paused or unpaused.
    /// Parameters: is paused
    /// Listeners: GameManager (set Time.timeScale), UI (show pause menu), AudioManager (pause sounds)
    /// </summary>
    public static event Action<bool> OnGamePaused;
    
    
    // ========================================
    // META-PROGRESSION EVENTS (Phase 3+)
    // ========================================
    
    /// <summary>
    /// Fired when player earns persistent currency.
    /// Parameters: currency amount
    /// Listeners: MetaProgressionUI (update display), SaveManager (save currency)
    /// </summary>
    public static event Action<int> OnCurrencyEarned;
    
    /// <summary>
    /// Fired when player unlocks a new character.
    /// Parameters: character ID
    /// Listeners: CharacterSelectUI (enable character), SaveManager (save unlock)
    /// </summary>
    public static event Action<string> OnCharacterUnlocked;
    
    /// <summary>
    /// Fired when player unlocks a new path tree for a character.
    /// Parameters: character ID, path tree ID
    /// Listeners: PathSystemUI (show new options), SaveManager (save unlock)
    /// </summary>
    public static event Action<string, string> OnPathTreeUnlocked;
    
    
    // ========================================
    // AUDIO EVENTS (Optional - can also subscribe directly to other events)
    // ========================================
    
    /// <summary>
    /// Fired when a sound effect should play.
    /// Parameters: sound ID/name, position (Vector2.zero for 2D sounds)
    /// Listeners: AudioManager
    /// </summary>
    public static event Action<string, Vector2> OnPlaySound;
    
    /// <summary>
    /// Fired when music should change.
    /// Parameters: music track ID/name
    /// Listeners: AudioManager
    /// </summary>
    public static event Action<string> OnPlayMusic;
    
    
    // ========================================
    // DEBUG EVENTS (Development only)
    // ========================================
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Fired when debug command is executed.
    /// Parameters: command name, arguments
    /// Listeners: DebugConsole, various managers
    /// </summary>
    public static event Action<string, string[]> OnDebugCommand;
    
    /// <summary>
    /// Debug logging event for combat/attack debugging.
    /// Parameters: log message, category
    /// </summary>
    public static event Action<string, DebugCategory> OnDebugLog;
    
    /// <summary>
    /// Helper method for debug logging with category filtering.
    /// Only logs if the category is enabled in DebugManager.
    /// Only active in Editor and Development builds.
    /// </summary>
    public static void DebugLog(string message, DebugCategory category = DebugCategory.General)
    {
        // Check if DebugManager exists and category is enabled
        if (DebugManager.Instance == null || !DebugManager.Instance.IsCategoryEnabled(category))
        {
            return;
        }
        
        // Add category prefix to message for easier filtering in console
        string prefixedMessage = $"{DebugManager.Instance.GetCategoryPrefix(category)} {message}";
        
        Debug.Log(prefixedMessage);
        OnDebugLog?.Invoke(prefixedMessage, category);
    }
    
    /// <summary>
    /// Backward compatibility: Log without category (uses General category).
    /// </summary>
    [System.Obsolete("Use DebugLog(message, category) instead for better filtering.")]
    public static void DebugLog(string message)
    {
        Debug.Log(message);
    }
    
    /// <summary>
    /// Helper method for debug warning logging with category filtering.
    /// Only logs if the category is enabled in DebugManager.
    /// Only active in Editor and Development builds.
    /// </summary>
    public static void DebugWarning(string message, DebugCategory category = DebugCategory.General)
    {
        // Check if DebugManager exists and category is enabled
        if (DebugManager.Instance == null || !DebugManager.Instance.IsCategoryEnabled(category))
        {
            return;
        }
        // Add category prefix to message for easier filtering in console
        string prefixedMessage = $"{DebugManager.Instance.GetCategoryPrefix(category)} {message}";
        Debug.LogWarning(prefixedMessage);
        OnDebugLog?.Invoke(prefixedMessage, category);
    }
    
    [System.Obsolete("Use DebugWarning(message, category) instead for better filtering.")]
    public static void DebugWarning(string message)
    {
        Debug.Log(message);
    }
    
    public static void DebugError(string message, DebugCategory category = DebugCategory.General)
    {
        // Check if DebugManager exists and category is enabled
        if (DebugManager.Instance == null || !DebugManager.Instance.IsCategoryEnabled(category))
        {
            return;
        }
        
        // Add category prefix to message for easier filtering in console
        string prefixedMessage = $"{DebugManager.Instance.GetCategoryPrefix(category)} {message}";
        
        Debug.LogError(prefixedMessage);
        OnDebugLog?.Invoke(prefixedMessage, category);
    }
    
    /// <summary>
    /// Backward compatibility: Log without category (uses General category).
    /// </summary>
    [System.Obsolete("Use DebugError(message, category) instead for better filtering.")]
    public static void DebugError(string message)
    {
        Debug.LogError(message);
    }
    #else
    /// <summary>
    /// No-op debug logging for release builds.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DebugLog(string message, DebugCategory category = DebugCategory.General) { }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Obsolete("Use DebugLog(message, category) instead for better filtering.")]
    public static void DebugLog(string message) { }
    #endif
    
    
    // ========================================
    // HELPER METHODS (Optional - for safer invocation)
    // ========================================

    public static void StartRun() {
        try {
            OnRunStarted?.Invoke();
        } catch (Exception e) {
            GameEvents.DebugError($"Error in OnRunStarted listeners: {e.Message}");
        }
    }

    public static void EndRun(bool wasVictory) {
        try {
            OnRunEnded?.Invoke(wasVictory);
        } catch (Exception e) {
            GameEvents.DebugError($"Error in OnRunEnded listeners: {e.Message}");
        }
    }
    
    /// <summary>
    /// Safely invoke OnEnemyKilled with null check and error handling.
    /// </summary>
    public static void EnemyKilled(Enemy enemy) {
        try {
            OnEnemyKilled?.Invoke(enemy);
        } catch (Exception e) {
            GameEvents.DebugError($"Error in OnEnemyKilled listeners: {e.Message}");
        }
    }
    
    /// <summary>
    /// Safely invoke OnPlayerDamaged with null check and error handling.
    /// </summary>
    public static void PlayerDamaged(float amount, float currentHP, float maxHP) {
        try {
            OnPlayerDamaged?.Invoke(amount, currentHP, maxHP);
        } catch (Exception e) {
            GameEvents.DebugError($"Error in OnPlayerDamaged listeners: {e.Message}");
        }
    }

    /// <summary>
    /// Safely invoke OnHealthChanged with null check and error handling.
    /// </summary>
    public static void PlayerHealthChanged(float currentHP, float maxHP) {
        try {
            OnHealthChanged?.Invoke(currentHP, maxHP);
        } catch (Exception e) {
            GameEvents.DebugError($"Error in OnPlayerHealthChanged listeners: {e.Message}");
        }
    }
    
    /// <summary>
    /// Safely invoke OnLevelUp with null check and error handling.
    /// </summary>
    public static void LevelUp(int newLevel) {
        try {
            OnLevelUp?.Invoke(newLevel);
        } catch (Exception e) {
            GameEvents.DebugError($"Error in OnLevelUp listeners: {e.Message}");
        }
    }
    
    /// <summary>
    /// Safely invoke OnRoomCleared with null check and error handling.
    /// </summary>
    public static void RoomCleared(int roomIndex) {
        try {
            OnRoomCleared?.Invoke(roomIndex);
        } catch (Exception e) {
            GameEvents.DebugError($"Error in OnRoomCleared listeners: {e.Message}");
        }
    }
    /// <summary>
    /// Safely invoke OnPlayerHealed with null check and error handling.   
    /// </summary>
    public static void PlayerHealed(float amount) {   // Helper method
        try {
            OnPlayerHealed?.Invoke(amount);
        } catch (Exception e) {
            GameEvents.DebugError($"Error in OnPlayerHealed listeners: {e.Message}");
        }
    }   

    /// <summary>
    /// Safely invoke OnPlayerDied with null check and error handling.  
    /// </summary>
    public static void PlayerDied() {   // Helper method
        try {
            OnPlayerDied?.Invoke();
        } catch (Exception e) {
            GameEvents.DebugError($"Error in OnPlayerDied listeners: {e.Message}");
        }
    }

    /// <summary>
    /// Safely invoke OnPlayerStatsChanged with null check and error handling.
    /// </summary>
    public static void PlayerStatsChanged(PlayerStats stats)
    {
        try {
            OnPlayerStatsChanged?.Invoke(stats);
        }
        catch (Exception e)
        {
            GameEvents.DebugError($"Error in OnPlayerStatsChanged listeners: {e.Message}");
        }
    }

    public static void XPChanged(int current, int required) {
    try {
        OnXPChanged?.Invoke(current, required);
    } catch (Exception e) {
        GameEvents.DebugError($"Error in OnXPChanged listeners: {e.Message}");
    }
    }

    public static void EnemyDamaged(float amount, Vector2 hitPoint, Vector2 hitDirection) {
        try {
            OnEnemyDamaged?.Invoke(amount, hitPoint, hitDirection);
        } catch (Exception e) {
            GameEvents.DebugError($"Error in OnEnemyDamaged listeners: {e.Message}");
        }
    }

    public static void UpgradeApplied(UpgradeData upgrade) {
        try {
            OnPlayerUpgradeApplied?.Invoke(upgrade);
        } catch (Exception e) {
            GameEvents.DebugError($"Error in OnPlayerUpgradeApplied listeners: {e.Message}");
        }
    }
    
    /// <summary>
    /// Safely invoke OnStatusEffectApplied with null check and error handling.
    /// </summary>
    public static void StatusEffectApplied(GameObject target, StatusEffectData effectData) {
        try {
            OnStatusEffectApplied?.Invoke(target, effectData);
        } catch (Exception e) {
            GameEvents.DebugError($"Error in OnStatusEffectApplied listeners: {e.Message}");
        }
    }
    
    /// <summary>
    /// Safely invoke OnStatusEffectRemoved with null check and error handling.
    /// </summary>
    public static void StatusEffectRemoved(GameObject target, StatusEffectData effectData) {
        try {
            OnStatusEffectRemoved?.Invoke(target, effectData);
        } catch (Exception e) {
            GameEvents.DebugError($"Error in OnStatusEffectRemoved listeners: {e.Message}");
        }
    }
    
    // Add more helper methods as needed for frequently used events
    
    
    // ========================================
    // CLEANUP (Call on scene unload to prevent memory leaks)
    // ========================================
    
    /// <summary>
    /// Unsubscribe all listeners. Call this when changing scenes or ending runs.
    /// WARNING: Only use this if you're absolutely sure you want to clear ALL listeners.
    /// Usually it's better to let each system manage its own subscriptions.
    /// </summary>
    public static void ClearAllListeners() {
        // Combat
        OnEnemyKilled = null;
        OnEnemyDamaged = null;
        OnPlayerDamaged = null;
        OnPlayerHealed = null;
        OnPlayerDied = null;
        
        // Status Effects
        OnStatusEffectApplied = null;
        OnStatusEffectRemoved = null;
        
        // Progression
        OnXPChanged = null;
        OnLevelUp = null;
        OnPlayerUpgradeApplied = null;
        OnUpgradeOffered = null;
        OnPlayerStatsChanged = null;
        
        // Room
        OnRoomEntered = null;
        OnRoomCleared = null;
        OnBossRoomEntered = null;
        OnBossDefeated = null;
        
        // Game State
        OnGameStateChanged = null;
        OnRunStarted = null;
        OnRunEnded = null;
        
        // UI
        OnShowNotification = null;
        OnGamePaused = null;
        
        // Meta
        OnCurrencyEarned = null;
        OnCharacterUnlocked = null;
        OnPathTreeUnlocked = null;
        
        // Audio
        OnPlaySound = null;
        OnPlayMusic = null;
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        OnDebugCommand = null;
        OnDebugLog = null;
        #endif
        
        GameEvents.DebugLog("All GameEvents listeners cleared.", DebugCategory.General);
    }
}


// ========================================
// SUPPORTING ENUMS
// ========================================

/// <summary>
/// Possible game states. Used with OnGameStateChanged event.
/// </summary>
public enum GameState {
    MainMenu,
    CharacterSelect,
    Playing,
    Paused,
    UpgradeChoice,
    GameOver,
    Victory,
    Loading
}

