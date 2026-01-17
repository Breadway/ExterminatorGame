using UnityEngine;

public interface IMovementBehavior{
    void Initialize(EnemyData enemyData);
    void UpdateMovement();
    void Stop();
}