using UnityEngine;

public enum EnemyMovementType
{
    ChaseMovement,
    FlyingMovement,
    SwarmMovement
}

public enum EnemyAttackType
{
    Contact,
    Ranged
}

public enum EnemyType
{
    Roach,
    Ant,
    Spider,
    Fly,
    Scorpion
}


[CreateAssetMenu(menuName = "Exterminator/EnemyData")]
public class EnemyData : ScriptableObject {
    public string enemyName;
    public float maxHealth;
    public float moveSpeed;
    public int XPValue;
    public float attackDamage;
    public float attackCooldown = 1f;
    public GameObject prefab;
    public EnemyMovementType MovementType;
    public EnemyAttackType AttackType;
}

/*
Example EnemyData:
Health: 50
Speed: 3.5
Damage: 10
XP Value: 15
Movement Type: "Chase"
Attack Type: "Contact"
Attack Cooldown: 1.0s
Detection Range: 69
Model Prefab: RoachModel
*/