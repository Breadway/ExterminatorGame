using UnityEngine;

/// <summary>
/// Poolable visual effect for status effects.
/// Attach this to status effect VFX prefabs to enable pooling.
/// </summary>
public class StatusEffectVFX : MonoBehaviour, IPoolable {
    
    [Header("Configuration")]
    [SerializeField] private bool autoReturnOnDisable = false;
    [SerializeField] private ParticleSystem[] particleSystems;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private Transform followTarget;
    private bool isFollowing;
    
    void Awake() {
        // Cache all particle systems if not already set
        if (particleSystems == null || particleSystems.Length == 0) {
            particleSystems = GetComponentsInChildren<ParticleSystem>();
        }
    }
    
    void Update() {
        // Follow the target transform if assigned
        if (isFollowing && followTarget != null) {
            transform.position = followTarget.position;
        }
    }
    
    /// <summary>
    /// Set the target transform for this VFX to follow.
    /// </summary>
    public void SetFollowTarget(Transform target) {
        if (enableDebugLogs) {
            GameEvents.DebugLog($"[StatusEffectVFX] SetFollowTarget called on {gameObject.name}, target: {target?.name ?? "NULL"}", DebugCategory.Combat);
        }
        
        followTarget = target;
        isFollowing = target != null;
        
        if (isFollowing) {
            transform.position = target.position;
            
            if (enableDebugLogs) {
                GameEvents.DebugLog($"[StatusEffectVFX] {gameObject.name} now following {target.name} at position {target.position}", DebugCategory.Combat);
            }
        }
    }
    
    /// <summary>
    /// Stop following the target.
    /// </summary>
    public void StopFollowing() {
        isFollowing = false;
        followTarget = null;
    }
    
    public void OnSpawnFromPool() {
        // Reset state
        isFollowing = false;
        followTarget = null;
        
        // Play all particle systems
        if (particleSystems != null) {
            foreach (var ps in particleSystems) {
                if (ps != null) {
                    ps.Clear();
                    ps.Play();
                }
            }
        }
        
        if (enableDebugLogs) {
            GameEvents.DebugLog($"[StatusEffectVFX] {gameObject.name} spawned from pool", DebugCategory.Combat);
        }
    }
    
    public void OnReturnToPool() {
        // Stop all particle systems
        if (particleSystems != null) {
            foreach (var ps in particleSystems) {
                if (ps != null) {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
        
        // Clear follow target
        isFollowing = false;
        followTarget = null;
        
        if (enableDebugLogs) {
            GameEvents.DebugLog($"[StatusEffectVFX] {gameObject.name} returned to pool", DebugCategory.Combat);
        }
    }
    
    void OnDisable() {
        if (autoReturnOnDisable && PoolingSystem.Instance != null) {
            PoolingSystem.Instance.Return(gameObject);
        }
    }
}
