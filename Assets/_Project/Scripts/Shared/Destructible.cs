using UnityEngine;
public class Destructible : MonoBehaviour {
    [Header("References")]
    private Health health;
    
    [Header("Death Settings")]
    [SerializeField] private GameObject deathParticlePrefab;
    [SerializeField] private GameObject[] lootDrops; // optional items to spawn
    [SerializeField] private float destroyDelay = 0.5f;
    
    void Awake() {
        health = GetComponent<Health>();
    }
    
    void OnEnable() {
        if (health != null) {
            health.OnDied += HandleDeath;
        }
    }
    
    void OnDisable() {
        if (health != null) {
            health.OnDied -= HandleDeath;
        }
    }
    
    void HandleDeath() {
        // Spawn particles
        if (deathParticlePrefab != null) {
            Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
        }
        
        // Drop loot
        if (lootDrops.Length > 0) {
            GameObject loot = lootDrops[Random.Range(0, lootDrops.Length)];
            Instantiate(loot, transform.position, Quaternion.identity);
        }
        
        // Destroy self
        Destroy(gameObject, destroyDelay);
    }
}