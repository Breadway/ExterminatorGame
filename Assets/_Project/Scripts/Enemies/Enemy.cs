using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyAttack))]
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour, IPoolable {

    [Header("Components")]
    [SerializeField] protected Health health;
    IMovementBehavior movement;
    [SerializeField] Rigidbody2D rb;
    EnemyAttack attack;
    IAttackBehaviour attackBehavior;
    [Header("Data")]
    [SerializeField] private EnemyData enemyData;
    public EnemyData EnemyData { get => enemyData; set => enemyData = value; }

    // Cache for pooling
    private bool isInitialized = false;

    void Update()
    {
        if (movement != null)
        {
            movement.UpdateMovement();
        }
    }

    void Awake() {
        CacheComponents();
        ConfigureRigidbody();
    }

    private void ConfigureRigidbody()
    {
        if (rb != null)
        {
            // Make enemy kinematic to prevent physics-based pushing/vibration
            // Enemy movement is handled by IMovementBehavior, not physics forces
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true; // Still detect collisions for damage
        }
    }

    private void CacheComponents()
    {
        if (isInitialized) return;
        
        health = GetComponent<Health>();
        attack = GetComponent<EnemyAttack>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<IMovementBehavior>();
        attackBehavior = GetComponent<IAttackBehaviour>();
        
        isInitialized = true;
    }

    void Start()
    {
        if (enemyData != null)
        {
            Initialize(enemyData);
        }
        else
        {
            Debug.LogWarning("Enemy missing EnemyData. Assign it on the prefab or via spawner before Initialize().", this);
        }
    }

    public void Initialize(EnemyData enemyData)
    {
        this.enemyData = enemyData;

        if (attack == null)
        {
            Debug.LogWarning("Enemy missing EnemyAttack component.", this);
        }
        else if (attackBehavior == null)
        {
            Debug.LogWarning("Enemy missing IAttackBehaviour component.", this);
        }
        else
        {
            attack.Initialize(this.enemyData, attackBehavior);
        }

        if (movement == null)
        {
            Debug.LogWarning("Enemy missing IMovementBehavior component.", this);
        }
        else
        {
            movement.Initialize(this.enemyData);
        }
    }
    
    void OnEnable() {
        health.OnDied += HandleDeath;
        health.OnDamaged += HandleDamage;
    }
    
    void OnDisable() {
        health.OnDied -= HandleDeath;
        health.OnDamaged -= HandleDamage;
    }
    
    void HandleDamage(float amount, Vector2 hitPoint, Vector2 hitDirection) {
        // Fire GameEvent for damage tracking, VFX, audio, etc.
        GameEvents.EnemyDamaged(amount, hitPoint, hitDirection);
        GameEvents.DebugLog($"[Enemy] {gameObject.name} took {amount} damage. HP: {health.CurrentHP}/{health.MaxHP}");
    }
    
    void HandleDeath() {
        GameEvents.EnemyKilled(this);
        
        // Return to pool instead of destroying
        if (PoolingSystem.Instance != null)
        {
            PoolingSystem.Instance.Return(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // IPoolable implementation for object pooling
    public void OnSpawnFromPool()
    {
        CacheComponents();
        
        // Reset health
        if (health != null)
        {
            health.ResetHealth();
        }
        
        // Re-enable Rigidbody2D
        if (rb != null)
        {
            rb.simulated = true;
        }
        
        // Re-initialize with enemy data
        if (enemyData != null)
        {
            Initialize(enemyData);
        }
    }

    public void OnReturnToPool()
    {
        // Stop Rigidbody2D to prevent errors
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        // Stop movement
        if (movement != null)
        {
            movement.Stop();
        }
    }
}