using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages active status effects on an entity.
/// Attach to any entity that can receive status effects (Player, Enemies).
/// </summary>
[RequireComponent(typeof(Health))]
public class StatusEffectController : MonoBehaviour {
    [Header("Configuration")]
    [SerializeField] private StatusResistanceData resistanceData;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private Health health;
    private readonly Dictionary<StatusEffectType, ActiveStatusEffect> activeEffects = new Dictionary<StatusEffectType, ActiveStatusEffect>();
    private readonly List<StatusEffectType> effectsToRemove = new List<StatusEffectType>();
    
    // Cached component references for modifiers
    private IMovementModifiable movementComponent;
    
    // Pre-allocated for spread detection
    private const int MAX_SPREAD_HITS = 16;
    private Collider2D[] spreadHitBuffer = new Collider2D[MAX_SPREAD_HITS];
    
    // Aggregate modifiers (recalculated when effects change)
    public float MoveSpeedMultiplier { get; private set; } = 1f;
    public float DamageTakenMultiplier { get; private set; } = 1f;
    
    void Awake() {
        health = GetComponent<Health>();
        movementComponent = GetComponent<IMovementModifiable>();
        
        if (resistanceData != null) {
            resistanceData.Initialize();
        }
    }
    
    void Update() {
        // Performance optimization: Early exit if no active effects
        // Critical for maintaining 60 FPS with 2000+ enemies
        if (activeEffects.Count == 0) return;
        
        effectsToRemove.Clear();
        
        foreach (var kvp in activeEffects) {
            var effect = kvp.Value;
            effect.UpdateTick(Time.deltaTime);
            
            // Handle damage ticks
            if (effect.Data.DamagePerTick > 0 && effect.ShouldTick()) {
                float damage = effect.GetTotalDamagePerTick();
                health.TakeDamage(damage, transform.position, Vector2.zero);
                
                if (enableDebugLogs) {
                    GameEvents.DebugLog($"[StatusEffect] {effect.Data.EffectName} dealt {damage} damage to {gameObject.name}", DebugCategory.Combat);
                }
            }
            
            // Handle spread (fire propagation)
            if (effect.ShouldSpread()) {
                TrySpreadEffect(effect);
            }
            
            // Mark expired effects for removal
            if (effect.IsExpired) {
                effectsToRemove.Add(kvp.Key);
            }
        }
        
        // Remove expired effects (do NOT call RemoveEffect, handle cleanup directly here)
        foreach (var type in effectsToRemove) {
            if (activeEffects.TryGetValue(type, out var effect)) {
                // Clean up visual - return to pool if possible
                if (effect.VisualInstance != null) {
                    ReturnEffectVisual(effect.VisualInstance);
                }
                
                activeEffects.Remove(type);
                
                // Fire event
                GameEvents.OnStatusEffectRemoved?.Invoke(gameObject, effect.Data);
                
                if (enableDebugLogs) {
                    GameEvents.DebugLog($"[StatusEffect] Removed expired {effect.Data.EffectName} from {gameObject.name}", DebugCategory.Combat);
                }
            }
        }
        
        // Recalculate modifiers if any effects were removed
        if (effectsToRemove.Count > 0) {
            RecalculateModifiers();
        }
    }
    
    /// <summary>
    /// Apply a status effect to this entity.
    /// Returns true if the effect was applied, false if immune or resisted.
    /// </summary>
    public bool ApplyEffect(StatusEffectData effectData, float durationMultiplier = 1f) {
        if (effectData == null) return false;
        
        // Check immunity
        if (resistanceData != null && resistanceData.IsImmune(effectData.EffectType)) {
            if (enableDebugLogs) {
                GameEvents.DebugLog($"[StatusEffect] {gameObject.name} is immune to {effectData.EffectName}", DebugCategory.Combat);
            }
            return false;
        }
        
        // Apply resistance to duration
        float finalDuration = effectData.Duration * durationMultiplier;
        if (resistanceData != null) {
            finalDuration = resistanceData.ApplyResistance(effectData.EffectType, finalDuration);
        }
        
        // Duration reduced to zero means fully resisted
        if (finalDuration <= 0f) {
            if (enableDebugLogs) {
                GameEvents.DebugLog($"[StatusEffect] {gameObject.name} fully resisted {effectData.EffectName}", DebugCategory.Combat);
            }
            return false;
        }
        
        // Check if effect already exists
        if (activeEffects.TryGetValue(effectData.EffectType, out var existingEffect)) {
            if (effectData.CanStack && existingEffect.CurrentStacks < effectData.MaxStacks) {
                existingEffect.AddStack();
                if (enableDebugLogs) {
                    GameEvents.DebugLog($"[StatusEffect] {effectData.EffectName} stacked on {gameObject.name} (x{existingEffect.CurrentStacks})", DebugCategory.Combat);
                }
            }
            
            if (effectData.RefreshDurationOnReapply) {
                existingEffect.RefreshDuration();
            }
            
            RecalculateModifiers();
            return true;
        }
        
        // Create new effect
        var newEffect = new ActiveStatusEffect(effectData, finalDuration);
        activeEffects[effectData.EffectType] = newEffect;
        
        // Spawn visual effect using pooling
        if (effectData.EffectPrefab != null) {
            SpawnEffectVisual(newEffect);
        }
        else if (enableDebugLogs) {
            GameEvents.DebugLog($"[StatusEffect] No EffectPrefab assigned for {effectData.EffectName}", DebugCategory.Combat);
        }
        
        RecalculateModifiers();
        
        // Fire event for UI/audio
        GameEvents.OnStatusEffectApplied?.Invoke(gameObject, effectData);
        
        if (enableDebugLogs) {
            GameEvents.DebugLog($"[StatusEffect] Applied {effectData.EffectName} to {gameObject.name} for {finalDuration}s", DebugCategory.Combat);
        }
        
        return true;
    }
    
    /// <summary>
    /// Remove a specific status effect.
    /// </summary>
    public void RemoveEffect(StatusEffectType type) {
        if (activeEffects.TryGetValue(type, out var effect)) {
            // Clean up visual - return to pool if possible
            if (effect.VisualInstance != null) {
                ReturnEffectVisual(effect.VisualInstance);
            }
            
            activeEffects.Remove(type);
            RecalculateModifiers();
            
            // Fire event
            GameEvents.OnStatusEffectRemoved?.Invoke(gameObject, effect.Data);
            
            if (enableDebugLogs) {
                GameEvents.DebugLog($"[StatusEffect] Removed {effect.Data.EffectName} from {gameObject.name}", DebugCategory.Combat);
            }
        }
    }
    
    /// <summary>
    /// Remove all active status effects.
    /// </summary>
    public void ClearAllEffects() {
        foreach (var effect in activeEffects.Values) {
            if (effect.VisualInstance != null) {
                ReturnEffectVisual(effect.VisualInstance);
            }
        }
        activeEffects.Clear();
        RecalculateModifiers();
    }
    
    /// <summary>
    /// Check if entity has a specific effect type active.
    /// </summary>
    public bool HasEffect(StatusEffectType type) {
        return activeEffects.ContainsKey(type);
    }
    
    /// <summary>
    /// Get the active effect of a specific type, or null if not present.
    /// </summary>
    public ActiveStatusEffect GetEffect(StatusEffectType type) {
        return activeEffects.TryGetValue(type, out var effect) ? effect : null;
    }
    
    private void TrySpreadEffect(ActiveStatusEffect effect) {
        if (effect.Data.SpreadTargetMask.value == 0) return;
        
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position, 
            effect.Data.SpreadRadius, 
            spreadHitBuffer, 
            effect.Data.SpreadTargetMask
        );
        
        for (int i = 0; i < hitCount; i++) {
            var col = spreadHitBuffer[i];
            if (col == null || col.gameObject == gameObject) continue;
            
            var targetController = col.GetComponent<StatusEffectController>();
            if (targetController != null && !targetController.HasEffect(effect.Data.EffectType)) {
                targetController.ApplyEffect(effect.Data);
                
                if (enableDebugLogs) {
                    GameEvents.DebugLog($"[StatusEffect] {effect.Data.EffectName} spread from {gameObject.name} to {col.gameObject.name}", DebugCategory.Combat);
                }
            }
        }
    }
    
    private void RecalculateModifiers() {
        MoveSpeedMultiplier = 1f;
        DamageTakenMultiplier = 1f;
        
        foreach (var effect in activeEffects.Values) {
            MoveSpeedMultiplier *= effect.Data.MoveSpeedMultiplier;
            DamageTakenMultiplier *= effect.Data.DamageTakenMultiplier;
        }
        
        // Notify movement component of change
        movementComponent?.SetSpeedMultiplier(MoveSpeedMultiplier);
    }
    
    /// <summary>
    /// Spawn a visual effect from pool and attach it to follow this entity.
    /// </summary>
    private void SpawnEffectVisual(ActiveStatusEffect effect) {
        // Validate prefab reference
        if (effect.Data.EffectPrefab == null) {
            if (enableDebugLogs) {
                GameEvents.DebugLog($"[StatusEffect] EffectPrefab is NULL for {effect.Data.EffectName}", DebugCategory.Combat);
            }
            return;
        }
        
        GameObject vfxInstance = null;
        
        if (enableDebugLogs) {
            GameEvents.DebugLog($"[StatusEffect] SpawnEffectVisual called for {effect.Data.EffectName} on {gameObject.name}", DebugCategory.Combat);
            GameEvents.DebugLog($"[StatusEffect] EffectPrefab: {effect.Data.EffectPrefab.name}", DebugCategory.Combat);
        }
        
        // Try to use pooling system
        if (PoolingSystem.Instance != null) {
            if (enableDebugLogs) {
                GameEvents.DebugLog($"[StatusEffect] PoolingSystem found, attempting to get VFX from pool", DebugCategory.Combat);
            }
            
            try {
                vfxInstance = PoolingSystem.Instance.Get(
                    effect.Data.EffectPrefab,
                    transform.position,
                    Quaternion.identity
                );
                
                if (enableDebugLogs) {
                    GameEvents.DebugLog($"[StatusEffect] PoolingSystem.Get returned: {vfxInstance?.name ?? "NULL"}", DebugCategory.Combat);
                }
            }
            catch (System.Exception ex) {
                if (enableDebugLogs) {
                    GameEvents.DebugLog($"[StatusEffect] PoolingSystem.Get failed: {ex.Message}. Falling back to Instantiate.", DebugCategory.Combat);
                }
                vfxInstance = null;
            }
        }
        
        // Fallback to instantiate if pooling failed or not available
        if (vfxInstance == null) {
            if (enableDebugLogs) {
                GameEvents.DebugLog($"[StatusEffect] Using Instantiate as fallback", DebugCategory.Combat);
            }
            
            vfxInstance = Instantiate(effect.Data.EffectPrefab, transform.position, Quaternion.identity);
            
            if (enableDebugLogs) {
                GameEvents.DebugLog($"[StatusEffect] Instantiate returned: {vfxInstance?.name ?? "NULL"}", DebugCategory.Combat);
            }
        }
        
        if (vfxInstance != null) {
            if (enableDebugLogs) {
                GameEvents.DebugLog($"[StatusEffect] VFX instance created successfully: {vfxInstance.name}, active: {vfxInstance.activeSelf}", DebugCategory.Combat);
            }
            
            // Set up VFX to follow this entity
            var vfxComponent = vfxInstance.GetComponent<StatusEffectVFX>();
            if (vfxComponent != null) {
                if (enableDebugLogs) {
                    GameEvents.DebugLog($"[StatusEffect] StatusEffectVFX component found, setting follow target to {gameObject.name}", DebugCategory.Combat);
                }
                vfxComponent.SetFollowTarget(transform);
            }
            else {
                // Fallback: parent it if no VFX component
                if (enableDebugLogs) {
                    GameEvents.DebugLog($"[StatusEffect] VFX prefab missing StatusEffectVFX component, using parent fallback", DebugCategory.Combat);
                }
                vfxInstance.transform.SetParent(transform);
                vfxInstance.transform.localPosition = Vector3.zero;
            }
            
            effect.VisualInstance = vfxInstance;
            
            if (enableDebugLogs) {
                GameEvents.DebugLog($"[StatusEffect] ✓ Successfully spawned and attached VFX for {effect.Data.EffectName} on {gameObject.name}", DebugCategory.Combat);
            }
        }
        else {
            if (enableDebugLogs) {
                GameEvents.DebugLog($"[StatusEffect] ✗ FAILED to spawn VFX - vfxInstance is NULL!", DebugCategory.Combat);
            }
        }
    }
    
    /// <summary>
    /// Return a visual effect to the pool.
    /// </summary>
    private void ReturnEffectVisual(GameObject vfxInstance) {
        if (vfxInstance == null) return;
        
        try {
            // Stop following before returning to pool
            var vfxComponent = vfxInstance.GetComponent<StatusEffectVFX>();
            if (vfxComponent != null) {
                vfxComponent.StopFollowing();
            }
            
            // Try to return to pool
            if (PoolingSystem.Instance != null) {
                PoolingSystem.Instance.Return(vfxInstance);
            }
            else {
                // Fallback to destroy if no pooling system
                Destroy(vfxInstance);
            }
        }
        catch (System.Exception ex) {
            if (enableDebugLogs) {
                GameEvents.DebugLog($"[StatusEffect] Error returning VFX to pool: {ex.Message}", DebugCategory.Combat);
            }
        }
    }
    
    void OnDisable() {
        ClearAllEffects();
    }
}