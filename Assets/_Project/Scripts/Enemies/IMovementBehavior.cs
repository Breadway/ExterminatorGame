using UnityEngine;

public interface IMovementBehavior{
    void Initialize(Rigidbody rb, UnityEngine.AI.NavMeshAgent agent);
    void UpdateMovement();
    void Stop();
}