using UnityEngine;
using UnityEngine.AI;
public class GroundMovement : IMovementBehavior {
    public Transform target;
    private Rigidbody rb;
    private UnityEngine.AI.NavMeshAgent agent;
    
    public void Initialize(Enemy enemy, EnemyData enemyData) {
        
    }

    void Awake()
    {
        target = FindObjectOfType<PlayerController>().transform
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