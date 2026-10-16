using UnityEngine;

public enum EnemyMovementType
{
    ChaseMovement,
    FlyingMovement,
    SwarmMovement,
    KeepDistance
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
    [Header("Identity")]
    public string enemyName;
    public GameObject prefab;
    [Tooltip("Optional. Lets one prefab serve several enemy types by swapping art.")]
    public Sprite sprite;
    [Tooltip("Size relative to the prefab. Prefer 1 and size the sprite instead.")]
    public float scale = 1f;
    [Tooltip("Hit circle radius in world units (before scale). Keeps big sprites hittable.")]
    public float colliderRadius = 0.42f;

    [Header("Core Stats")]
    public float maxHealth;
    public float moveSpeed;
    public int XPValue;
    public float attackDamage;
    public float attackCooldown = 1f;
    public EnemyMovementType MovementType;
    public EnemyAttackType AttackType;

    [Header("Spawning")]
    [Tooltip("First wave this enemy can appear in.")]
    public int minWave = 1;
    [Tooltip("Relative chance to be picked once unlocked.")]
    public float spawnWeight = 1f;

    [Header("Defense")]
    [Tooltip("Fraction of incoming damage ignored. 0.5 halves every hit, which blunts damage-over-time hardest.")]
    [Range(0f, 0.9f)] public float damageReduction = 0f;

    [Header("Ranged (AttackType = Ranged)")]
    public GameObject projectilePrefab;
    [Tooltip("Distance the enemy tries to hold from the player.")]
    public float preferredRange = 6f;
    [Tooltip("Farthest distance at which it will fire.")]
    public float attackRange = 9f;
    public float projectileSpeed = 9f;
    public float projectileLifetime = 2.5f;

    [Header("Evolution")]
    [Tooltip("If set, this enemy turns into the target after living this long.")]
    public EnemyData evolvesInto;
    public float evolveAfterSeconds = 8f;

    [Header("Scrap")]
    [Tooltip("Meta-progression currency dropped on death.")]
    public int scrapValue = 1;

    [Header("Death hazard")]
    [Tooltip("Leave a floor hazard behind on death (acid beetles, volatile pests).")]
    public bool spawnHazardOnDeath;
    public HazardKind deathHazard = HazardKind.Acid;
    public float deathHazardRadius = 1.4f;
    public float deathHazardLifetime = 6f;
    public float deathHazardDps = 6f;

    [Header("Boss")]
    public bool isBoss;
    [Tooltip("Enemy type summoned during the fight.")]
    public EnemyData summonType;
    public float summonInterval = 0f;
    public int summonCount = 3;
    [Tooltip("Seconds between projectile rings (needs projectilePrefab). 0 = none.")]
    public float volleyInterval = 0f;
    public int volleyCount = 10;
    [Tooltip("Below this health fraction the boss enrages and acts faster.")]
    [Range(0f, 1f)] public float enrageBelowHealth = 0.35f;
    public float enrageSpeedMultiplier = 1.4f;
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
Model Prefab: RoachModel
*/
