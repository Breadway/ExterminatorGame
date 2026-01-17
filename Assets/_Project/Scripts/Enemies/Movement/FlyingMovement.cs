using UnityEditor.Callbacks;
using UnityEngine;
public class FlyingMovement : IMovementBehavior {
    private Rigidbody rb;
    private UnityEngine.AI.NavMeshAgent agent;
    public void Initialize(Rigidbody rb, UnityEngine.AI.NavMeshAgent agent) {
        this.rb = rb;
        this.agent = agent;
    }
    
    public void UpdateMovement()
    {
        
    }

    public void Stop()
    {
        
    }
}  