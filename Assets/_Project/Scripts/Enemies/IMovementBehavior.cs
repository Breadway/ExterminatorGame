using UnityEngine;

public interface IMovementBehavior : IMovementModifiable{
    void Initialize(EnemyData enemyData);
    void UpdateMovement();
    void Stop();
}