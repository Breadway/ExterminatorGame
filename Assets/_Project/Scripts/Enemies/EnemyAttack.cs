using UnityEngine;

public class EnemyAttack : MonoBehaviour {
    // Enemy attack logic
    EnemyData enemyData;
    IAttackBehaviour attackBehavior;
    public void Initialize(EnemyData enemyDatainit, IAttackBehaviour attackBehavior)
    {
        enemyData = enemyDatainit;
        this.attackBehavior = attackBehavior;
        if (attackBehavior != null)
        {
            attackBehavior.Initialize(enemyData);
        }
    }
}