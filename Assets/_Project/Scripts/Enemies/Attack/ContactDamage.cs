using UnityEngine;

/// <summary>
/// Melee damage on touch. It polls the gap between colliders instead of using OnCollisionStay2D:
/// Unity sends no stay callbacks once both bodies fall asleep, so an enemy standing still against
/// the player (which is exactly what a chasing enemy does) would stop hurting them.
/// </summary>
public class ContactDamage : MonoBehaviour, IAttackBehaviour
{
    private EnemyData enemyData;
    private float nextAttackTime = 0f;
    private Collider2D ownCollider;
    private Health targetHealth;
    private Collider2D targetCollider;
    private Health ownHealth;
    private const float TouchGap = 0.08f;

    void Awake()
    {
        ownCollider = GetComponent<Collider2D>();
        ownHealth = GetComponent<Health>();
    }

    public void Initialize(EnemyData enemyData)
    {
        this.enemyData = enemyData;
        // A short grace period so a freshly spawned enemy cannot hit on its first frame
        nextAttackTime = Time.time + 0.25f;
    }

    public void Attack(GameObject target)
    {
        if (target == null || enemyData == null || Time.timeScale <= 0f || Time.time < nextAttackTime) return;
        var health = target.GetComponent<Health>();
        if (health == null) return;

        Vector2 hitPoint = target.transform.position;
        Vector2 hitDirection = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        float mult = GetComponent<Enemy>()?.DamageMultiplier ?? 1f;
        health.TakeDamage(enemyData.attackDamage * mult, hitPoint, hitDirection);
        nextAttackTime = Time.time + Mathf.Max(0f, enemyData.attackCooldown);
    }

    public void StopAttack()
    {
        // No continuous attack to stop for contact damage
    }

    /// <summary>Push back the next attack (mop stagger). Never shortens an existing delay.</summary>
    public void DelayNextAttack(float seconds)
    {
        if (seconds <= 0f) return;
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + seconds);
    }

    void FixedUpdate()
    {
        if (enemyData == null || Time.time < nextAttackTime || Time.timeScale <= 0f) return;
        if (ownHealth != null && !ownHealth.IsAlive) return;

        var player = PlayerController.Instance;
        if (player == null) return;
        if (targetCollider == null || targetCollider.gameObject != player.gameObject)
        {
            targetCollider = player.GetComponent<Collider2D>();
            targetHealth = player.GetComponent<Health>();
        }
        if (ownCollider == null || targetCollider == null || targetHealth == null || !targetHealth.IsAlive) return;

        ColliderDistance2D gap = ownCollider.Distance(targetCollider);
        if (gap.isOverlapped || gap.distance <= TouchGap)
        {
            Attack(player.gameObject);
        }
    }
}
