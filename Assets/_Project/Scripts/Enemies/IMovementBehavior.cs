using UnityEngine;

public interface IMovementBehavior{
    void Initialize(Enemy enemy, EnemyData enemyData);
    void UpdateMovement();
    void Stop();
}