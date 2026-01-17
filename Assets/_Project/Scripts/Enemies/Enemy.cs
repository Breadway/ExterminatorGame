using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyAttack))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class Enemy : MonoBehaviour {

    [Header("Components")]
    [SerializeField] protected Health health;
    IMovementBehavior movement;
    [SerializeField] Rigidbody rb;
    [SerializeField] UnityEngine.AI.NavMeshAgent agent;
    EnemyAttack attack;
    IAttackBehaviour attackBehavior;
    [Header("Data")]
    [SerializeField] private EnemyData enemyData;
    public EnemyData EnemyData { get => enemyData; set => enemyData = value; }

    void Update()
    {
        if (movement != null)
        {
            movement.UpdateMovement();
        }
    }

    void Awake() {
        health = GetComponent<Health>();
        attack = GetComponent<EnemyAttack>();
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        movement = GetComponent<IMovementBehavior>();
        attackBehavior = GetComponent<IAttackBehaviour>();

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
        Destroy(gameObject);
        // Play Death Animation, drop loot, etc.
    }
}