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
    
    // ========== ENEMY EVENTS ==========
    
    /// <summary>
    /// Fired when an enemy is killed.
    /// XPManager, RoomManager, AudioManager, and UIManager listen to this.
    /// </summary>
    public static Action<GameObject> OnEnemyKilled;
}