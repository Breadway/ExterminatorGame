using UnityEngine;

public interface IAttackBehaviour
{
    void Initialize(EnemyData enemyData);
    void Attack(GameObject target);
    void StopAttack();
}