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
    public static Action<Vector3> OnPlayerAimDirectionChanged;

    public static Action<Vector3> OnPlayerDash;

    
    // ========================================
    // COMBAT EVENTS
    // ========================================
    
    /// <summary>
    /// Fired when an enemy is killed. Passes the enemy that died.
    /// Listeners: XPManager (award XP), RoomManager (check room clear), AudioManager (death sound), UI (kill counter)
    /// </summary>
    public static event Action<Enemy> OnEnemyKilled;
    
    /// <summary>
    /// Fired when an enemy takes damage (non-fatal).
    /// Parameters: damage amount, hit position, hit direction
    /// Listeners: VFXManager (spawn damage numbers), AudioManager (hit sound)
    /// </summary>
    public static event Action<float, Vector3, Vector3> OnEnemyDamaged;
    
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
    // PROGRESSION EVENTS
    // ========================================
    
    /// <summary>
    /// Fired when player gains XP.
    /// Parameters: XP amount gained
    /// Listeners: XPBarUI (update bar), VFXManager (XP gain popup)
    /// </summary>
    public static event Action<int> OnXPGained;
    
    /// <summary>
    /// Fired when player levels up.
    /// Parameters: new level
    /// Listeners: Player (heal on level up), UI (level up popup), PathSystem (check for path unlock at 10/20/30)
    /// </summary>
    public static event Action<int> OnLevelUp;
    
    /// <summary>
    /// Fired when player chooses an upgrade after room clear.
    /// Parameters: chosen upgrade data
    /// Listeners: Player (apply stats), UI (close upgrade panel), SaveManager (track upgrades taken)
    /// </summary>
    public static event Action<UpgradeData> OnUpgradeChosen;
    
    /// <summary>
    /// Fired when upgrade choices are offered to player.
    /// Parameters: current level, array of 3 upgrade options
    /// Listeners: UpgradeUI (display choices)
    /// </summary>
    public static event Action<int, UpgradeData[]> OnUpgradeOffered;
    
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
    /// Parameters: sound ID/name, position (Vector3.zero for 2D sounds)
    /// Listeners: AudioManager
    /// </summary>
    public static event Action<string, Vector3> OnPlaySound;
    
    /// <summary>
    /// Fired when music should change.
    /// Parameters: music track ID/name
    /// Listeners: AudioManager
    /// </summary>
    public static event Action<string> OnPlayMusic;
    
    
    // ========================================
    // DEBUG EVENTS (Development only)
    // ========================================
    
    #if UNITY_EDITOR
    /// <summary>
    /// Fired when debug command is executed.
    /// Parameters: command name, arguments
    /// Listeners: DebugConsole, various managers
    /// </summary>
    public static event Action<string, string[]> OnDebugCommand;
    #endif
    
    
    // ========================================
    // HELPER METHODS (Optional - for safer invocation)
    // ========================================
    
    /// <summary>
    /// Safely invoke OnEnemyKilled with null check and error handling.
    /// </summary>
    public static void EnemyKilled(Enemy enemy) {
        try {
            OnEnemyKilled?.Invoke(enemy);
        } catch (Exception e) {
            Debug.LogError($"Error in OnEnemyKilled listeners: {e.Message}");
        }
    }
    
    /// <summary>
    /// Safely invoke OnPlayerDamaged with null check and error handling.
    /// </summary>
    public static void PlayerDamaged(float amount, float currentHP, float maxHP) {
        try {
            OnPlayerDamaged?.Invoke(amount, currentHP, maxHP);
        } catch (Exception e) {
            Debug.LogError($"Error in OnPlayerDamaged listeners: {e.Message}");
        }
    }
    
    /// <summary>
    /// Safely invoke OnLevelUp with null check and error handling.
    /// </summary>
    public static void LevelUp(int newLevel) {
        try {
            OnLevelUp?.Invoke(newLevel);
        } catch (Exception e) {
            Debug.LogError($"Error in OnLevelUp listeners: {e.Message}");
        }
    }
    
    /// <summary>
    /// Safely invoke OnRoomCleared with null check and error handling.
    /// </summary>
    public static void RoomCleared(int roomIndex) {
        try {
            OnRoomCleared?.Invoke(roomIndex);
        } catch (Exception e) {
            Debug.LogError($"Error in OnRoomCleared listeners: {e.Message}");
        }
    }
    /// <summary>
    /// Safely invoke OnPlayerHealed with null check and error handling.   
    /// </summary>
    public static void PlayerHealed(float amount) {   // Helper method
        OnPlayerHealed?.Invoke(amount);
    }   

    /// <summary>
    /// Safely invoke OnPlayerDied with null check and error handling.  
    /// </summary>
    public static void PlayerDied() {   // Helper method
        OnPlayerDied?.Invoke();
    }

    public static void PlayerStatsChanged(PlayerStats stats)
    {
        OnPlayerStatsChanged?.Invoke(stats);
    }

    // ========================================
    // CAMERA EVENTS
    // ========================================

    /// <summary>
    /// Fired to request the camera rotate by a delta Euler angle (degrees).
    /// Parameter: Vector3 deltaEuler (x=pitch,y=yaw,z=roll)
    /// Listeners: PlayerCamera
    /// </summary>
    public static event Action<Vector3> OnCameraRotateBy;

    public static void CameraRotateBy(Vector3 deltaEuler)
    {
        try
        {
            OnCameraRotateBy?.Invoke(deltaEuler);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in OnCameraRotateBy listeners: {e.Message}");
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
        
        // Progression
        OnXPGained = null;
        OnLevelUp = null;
        OnUpgradeChosen = null;
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
        
        #if UNITY_EDITOR
        OnDebugCommand = null;
        #endif
        
        Debug.Log("All GameEvents listeners cleared.");
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