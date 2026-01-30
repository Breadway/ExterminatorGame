using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyAttack))]
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour, IPoolable, IMovementModifiable {

    [Header("Components")]
    [SerializeField] protected Health health;
    IMovementBehavior movement;
    [SerializeField] Rigidbody2D rb;
    EnemyAttack attack;
    IAttackBehaviour attackBehavior;
    [Header("Data")]
    [SerializeField] private EnemyData enemyData;
    public EnemyData EnemyData { get => enemyData; set => enemyData = value; }
    bool debugMode = false;

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
        ApplyEnemyData();
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
    
    void ApplyEnemyData()
    {
        if (enemyData != null && health != null)
        {
            health.SetMaxHP(enemyData.maxHealth);
        }
    }

    void Start()
    {
        if (enemyData != null)
        {
            Initialize(enemyData);
        }
        else
        {
            GameEvents.DebugLog("Enemy has no EnemyData assigned.", DebugCategory.EnemyAI);
        }
    }

    public void Initialize(EnemyData enemyData)
    {
        this.enemyData = enemyData;

        if (attack == null)
        {
            GameEvents.DebugLog("Enemy missing EnemyAttack component.", DebugCategory.EnemyAI);
        }
        else if (attackBehavior == null)
        {
            GameEvents.DebugLog("Enemy missing IAttackBehaviour component.", DebugCategory.EnemyAI);
        }
        else
        {
            attack.Initialize(this.enemyData, attackBehavior);
        }

        if (movement == null)
        {
            GameEvents.DebugLog("Enemy missing IMovementBehavior component.", DebugCategory.EnemyAI);
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
        GameEvents.DebugLog($"[Enemy] {gameObject.name} took {amount} damage. HP: {health.CurrentHP}/{health.MaxHP}", DebugCategory.EnemyAI);
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

    public void SetSpeedMultiplier(float multiplier)
    {
        if (movement != null)
        {
            movement.SetSpeedMultiplier(multiplier);
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