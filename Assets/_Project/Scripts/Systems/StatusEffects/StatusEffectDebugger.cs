using UnityEngine;

/// <summary>
/// Debug script to test and diagnose status effect visual issues.
/// Attach this to any GameObject in the scene and press T to run diagnostics.
/// </summary>
public class StatusEffectDebugger : MonoBehaviour {
    
    [Header("Test Configuration")]
    [SerializeField] private StatusEffectData testEffect; // Assign in inspector
    [SerializeField] private GameObject testTarget; // Drag enemy here
    
    void Update() {
        // Press T to run diagnostics
        if (Input.GetKeyDown(KeyCode.T)) {
            RunDiagnostics();
        }
        
        // Press Y to apply test effect
        if (Input.GetKeyDown(KeyCode.Y)) {
            ApplyTestEffect();
        }
    }
    
    void RunDiagnostics() {
        Debug.Log("=== STATUS EFFECT SYSTEM DIAGNOSTICS ===");
        
        // Check PoolingSystem
        if (PoolingSystem.Instance != null) {
            Debug.Log("✓ PoolingSystem found");
        } else {
            Debug.LogError("✗ PoolingSystem NOT FOUND - Status effects will use Instantiate fallback");
        }
        
        // Check for enemies in scene
        var enemies = FindObjectsOfType<Enemy>();
        Debug.Log($"Found {enemies.Length} enemies in scene");
        
        foreach (var enemy in enemies) {
            Debug.Log($"  - Enemy: {enemy.name}");
            
            var statusController = enemy.GetComponent<StatusEffectController>();
            if (statusController != null) {
                Debug.Log($"    ✓ Has StatusEffectController");
            } else {
                Debug.LogError($"    ✗ MISSING StatusEffectController!");
            }
            
            var health = enemy.GetComponent<Health>();
            if (health != null) {
                Debug.Log($"    ✓ Has Health (HP: {health.CurrentHP}/{health.MaxHP})");
            } else {
                Debug.LogError($"    ✗ MISSING Health!");
            }
        }
        
        // Check status effect data
        if (testEffect != null) {
            Debug.Log($"Test Effect: {testEffect.EffectName}");
            if (testEffect.EffectPrefab != null) {
                Debug.Log($"  ✓ Has Effect Prefab: {testEffect.EffectPrefab.name}");
                
                var vfxComponent = testEffect.EffectPrefab.GetComponent<StatusEffectVFX>();
                if (vfxComponent != null) {
                    Debug.Log($"    ✓ VFX has StatusEffectVFX component");
                } else {
                    Debug.LogWarning($"    ⚠ VFX MISSING StatusEffectVFX component - will use parent fallback");
                }
                
                var particles = testEffect.EffectPrefab.GetComponentsInChildren<ParticleSystem>();
                Debug.Log($"    Found {particles.Length} particle systems");
            } else {
                Debug.LogError($"  ✗ NO Effect Prefab assigned!");
            }
        } else {
            Debug.LogWarning("No test effect assigned - assign one in inspector to test");
        }
        
        Debug.Log("=== END DIAGNOSTICS ===");
    }
    
    void ApplyTestEffect() {
        if (testEffect == null) {
            Debug.LogError("No test effect assigned!");
            return;
        }
        
        GameObject target = testTarget;
        if (target == null) {
            // Find first enemy
            var enemy = FindFirstObjectByType<Enemy>();
            if (enemy != null) {
                target = enemy.gameObject;
            }
        }
        
        if (target == null) {
            Debug.LogError("No target found! Drag an enemy to 'Test Target' or spawn an enemy first.");
            return;
        }
        
        var statusController = target.GetComponent<StatusEffectController>();
        if (statusController == null) {
            Debug.LogError($"{target.name} has no StatusEffectController component!");
            return;
        }
        
        Debug.Log($"Applying {testEffect.EffectName} to {target.name}...");
        bool success = statusController.ApplyEffect(testEffect);
        
        if (success) {
            Debug.Log($"✓ Effect applied successfully!");
        } else {
            Debug.LogError($"✗ Effect failed to apply (check immunity/resistance)");
        }
    }
}
