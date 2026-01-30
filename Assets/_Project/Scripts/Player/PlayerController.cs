using System;
using UnityEngine;
/// <summary>
/// Central controller for the player character.
/// Responsibilities:
/// - Initialize and hold references to player components (movement, aiming, shooting, health, stats, etc)
/// - Orchestrate interactions between components
/// - Handle player-specific events (damage reactions, death behavior, level up effects, etc)
/// - Expose public API for other systems to interact with the player
/// </summary>
/// 
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAiming))]
[RequireComponent(typeof(PlayerAttack))]
public class PlayerController : MonoBehaviour {
    [Header("Components")]
    [SerializeField] private Health health;
    private PlayerMovement movement;
    private PlayerAiming aiming;
    private PlayerAttack shooting;
    // Add more as needed
    
    [Header("Stats")]
    public PlayerStats stats { get; private set; }
    
    [Header("References")]
    public static PlayerController Instance { get; private set; } // Singleton for easy access
    
    void Awake() {
        // Singleton setup
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }
        
        // Get components
        health = GetComponent<Health>();
        movement = GetComponent<PlayerMovement>();
        aiming = GetComponent<PlayerAiming>();
        shooting = GetComponent<PlayerAttack>();
        
        // Initialize stats
        stats = new PlayerStats();
        
        // Apply stats to health
        health.SetMaxHP(stats.currentMaxHP);
    }

    public void GetHealth(out Health outHealth) {
        outHealth = health;
    }
    
    void OnEnable() {
        // Subscribe to health events
        health.OnDamaged += HandleDamaged;
        health.OnHealed += HandleHealed;
        health.OnDied += HandleDeath;
        
        // Subscribe to game events
        GameEvents.OnLevelUp += OnLevelUp;
    }
    
    void OnDisable() {
        // ALWAYS unsubscribe
        health.OnDamaged -= HandleDamaged;
        health.OnHealed -= HandleHealed;
        health.OnDied -= HandleDeath;
        
        GameEvents.OnLevelUp -= OnLevelUp;
    }
    
    void FixedUpdate() {
        // Orchestrate movement
    }
    
    void Update() {
        // Orchestrate attacks
    }
    
    // === DAMAGE HANDLING ===
    
    void HandleDamaged(float amount, Vector2 hitPoint, Vector2 hitDirection) {
        // Player-specific damage reactions
        
        // Screen effects
        // CameraShake or ScreenFlash component handles this via events
        
        // Audio
        // AudioManager listens to health.OnDamaged
        
        // Knockback
        movement.ApplyKnockback(hitDirection);
        
        // Fire game event for other systems
        GameEvents.OnPlayerDamaged?.Invoke(amount, health.CurrentHP, health.MaxHP);
        GameEvents.PlayerHealthChanged(health.CurrentHP, health.MaxHP);
    }
    
    void HandleHealed(float amount) {
        // Fire event for UI/audio
        GameEvents.PlayerHealed(amount);
        GameEvents.PlayerHealthChanged(health.CurrentHP, health.MaxHP);
    }
    
    void HandleDeath() {
        // Player-specific death behavior
        
        // Disable input
        movement.enabled = false;
        shooting.enabled = false;
        
        // Play death animation
        // animator.SetTrigger("Death");
        
        // Fire game event
        GameEvents.PlayerDied();
        GameEvents.EndRun(false); // Player lost
        Destroy(gameObject);
        // GameManager listens to OnPlayerDied and shows game over screen
    }
    
    // === PROGRESSION ===
    
    void OnLevelUp(int newLevel) {
        // Heal % on level up (from your GDD)
        float healAmount = health.MaxHP * 0.2f; // 20% (adjust as needed)
        health.Heal(healAmount);
        
        // Visual feedback
        // Spawn level-up particles, play sound, etc.
        // (These are handled by systems listening to GameEvents.OnLevelUp)
    }
    
    void ApplyUpgrade(UpgradeData upgrade) {
        // Apply upgrade to stats
        stats.ApplyUpgrade(upgrade);
        
        // Update health if max HP changed
        if (upgrade.healthBonus > 0) {
            health.SetMaxHP(stats.currentMaxHP);
            health.Heal(upgrade.healthBonus); // Heal the bonus amount
        }
        
        // Update movement speed
        if (upgrade.speedMultiplier != 1f) {
            movement.UpdateSpeed(stats.currentSpeed);
        }
        
        // Fire event for UI updates
        GameEvents.PlayerStatsChanged(stats);
    }
    
    // === PUBLIC API (for other systems to call) ===
    
    public void TakeDamageFromGameEvent(float amount) {
        // If you want GameManager to directly call damage
        // (Though I recommend using IDamageable instead)
        health.TakeDamage(amount, transform.position, Vector2.zero);
    }
}