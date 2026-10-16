using UnityEngine;

/// <summary>
/// Fires projectiles at the player while in range. Stats come from EnemyData (range, speed,
/// damage, cooldown, projectile prefab), so one component serves every ranged enemy.
/// </summary>
public class RangedAttack : MonoBehaviour, IAttackBehaviour
{
    private EnemyData data;
    private float nextShot;
    private Transform target;
    private LayerMask playerMask;
    private Health ownHealth;

    void Awake()
    {
        playerMask = 1 << LayerMask.NameToLayer("Player");
        ownHealth = GetComponent<Health>();
    }

    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        nextShot = Time.time + Random.Range(0.3f, 1.2f); // stagger a spawned group
    }

    public void Attack(GameObject targetObject)
    {
        if (data == null || data.projectilePrefab == null || targetObject == null) return;
        Vector2 origin = transform.position;
        Vector2 dir = (Vector2)targetObject.transform.position - origin;
        if (dir.sqrMagnitude < 0.01f) return;

        GameObject go = PoolingSystem.Instance != null
            ? PoolingSystem.Instance.Get(data.projectilePrefab, origin, Quaternion.identity)
            : Instantiate(data.projectilePrefab, origin, Quaternion.identity);
        if (go == null) return;
        var projectile = go.GetComponent<Projectile>();
        if (projectile != null)
        {
            float mult = GetComponent<Enemy>()?.DamageMultiplier ?? 1f;
            projectile.Fire(dir, data.projectileSpeed, data.attackDamage * mult, data.projectileLifetime, playerMask);
        }
    }

    public void StopAttack() { }

    void Update()
    {
        if (data == null || Time.timeScale <= 0f || (ownHealth != null && !ownHealth.IsAlive)) return;
        if (target == null)
        {
            if (PlayerController.Instance == null) return;
            target = PlayerController.Instance.transform;
        }
        if (Time.time < nextShot) return;

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance > data.attackRange) return;

        Attack(target.gameObject);
        nextShot = Time.time + Mathf.Max(0.2f, data.attackCooldown);
    }
}
