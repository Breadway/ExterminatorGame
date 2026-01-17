using UnityEditor.Callbacks;
using UnityEngine;
public class Enemy : MonoBehaviour {

    [Header("Components")]
    [SerializeField] protected Health health;
    [SerializeField] IMovementBehavior movement;
    [SerializeField] Rigidbody rb;
    [SerializeField] UnityEngine.AI.NavMeshAgent agent;
    protected EnemyAttack attack;
    EnemyData enemyData;

    [Header("Data")]
    [SerializeField] protected EnemyData data;

    void Update()
    {
        movement.UpdateMovement();
    }

    void Awake() {
        health = GetComponent<Health>();
        movement = GetComponent<IMovementBehavior>();
        attack = GetComponent<EnemyAttack>();
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        enemyData = null;
        movement.Initialize(rb, agent);
    }
    void OnEnable() {
        health.OnDied += HandleDeath;
    }
    void OnDisable() {
        health.OnDied -= HandleDeath;
    }
    void HandleDeath() {
        Destroy(gameObject);
        // Play Death Animation, drop loot, etc.
    }
}