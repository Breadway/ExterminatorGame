using UnityEngine;

public class EnemyAttack : MonoBehaviour {
    // Enemy attack logic
    [SerializeField] EnemyData EnemyData;
    public void Initialize(EnemyData enemyDatainit)
    {
        EnemyData = enemyDatainit;
    }

    void Attack()
    {
        
    }
}