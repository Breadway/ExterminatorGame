using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyAttack))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class Enemy : MonoBehaviour, IPoolable {

    [Header("Components")]
    [SerializeField] protected Health health;
    IMovementBehavior movement;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] UnityEngine.AI.NavMeshAgent agent;
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
    }

    private void CacheComponents()
    {
        if (isInitialized) return;
        
        health = GetComponent<Health>();
        attack = GetComponent<EnemyAttack>();
        rb = GetComponent<Rigidbody2D>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
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
    }
    
    void OnDisable() {
        health.OnDied -= HandleDeath;
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
        
        // Re-enable NavMeshAgent
        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }
        
        // Re-initialize with enemy data
        if (enemyData != null)
        {
            Initialize(enemyData);
        }
    }

    public void OnReturnToPool()
    {
        // Stop NavMeshAgent to prevent errors
        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        
        // Stop movement
        if (movement != null)
        {
            movement.Stop();
        }
    }
}