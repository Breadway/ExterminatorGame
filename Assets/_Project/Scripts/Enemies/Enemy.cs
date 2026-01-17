using UnityEditor.Callbacks;
using UnityEngine;
public class Enemy : MonoBehaviour {

    [Header("Components")]
    [SerializeField] protected Health health;
    IMovementBehavior movement;
    [SerializeField] Rigidbody rb;
    [SerializeField] UnityEngine.AI.NavMeshAgent agent;
    protected EnemyAttack attack;

    [Header("Data")]
    [SerializeField] private EnemyData data;

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
        attack = GetComponent<EnemyAttack>();

        if (attack != null && data != null) {
            attack.Initialize(data); // Make Initialize() public first
        }
        
        movement.Initialize(rb, agent);
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