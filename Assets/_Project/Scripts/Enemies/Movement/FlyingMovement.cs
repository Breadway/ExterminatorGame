using UnityEngine;
public class FlyingMovement : IMovementBehavior {
    private Rigidbody rb;
    private UnityEngine.AI.NavMeshAgent agent;
    public void Initialize(Enemy enemy, EnemyData enemyData) {
        rb = enemy.GetComponent<Rigidbody>();
        agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
    }
    
    public void UpdateMovement()
    {
        
    }

    public void Stop()
    {
        
    }
}  