using UnityEngine;
using UnityEngine.AI;
public class GroundMovement : MonoBehaviour, IMovementBehavior {
    public Transform target;
    private Rigidbody rb;
    private NavMeshAgent agent;
    private Enemy enemy;
    private EnemyData enemyData;
    
    public void Initialize(Rigidbody rb, NavMeshAgent agent) {
        this.rb = rb;
        this.agent = agent;
    }

    void Awake()
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player != null) {
            target = player.transform;
        } else {
            Debug.LogWarning("PlayerController not found in the scene.");
            target = null;
        }
        agent = enemy.GetComponent<NavMeshAgent>();

    }
    
    public void UpdateMovement()
    {
        agent.destination = target.position;
    }

    public void Stop()
    {
        
    }
}