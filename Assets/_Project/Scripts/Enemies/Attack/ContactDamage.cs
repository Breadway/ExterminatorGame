using UnityEngine;

public class ContactDamage : MonoBehaviour, IAttackBehaviour
{
    private EnemyData enemyData;
    private float nextAttackTime = 0f;
    private GameObject cachedTarget;
    private Health cachedHealth;
    
    // Cached layer to avoid expensive LayerMask.NameToLayer() calls every frame
    private static int playerLayer = -1;
    
    void Awake()
    {
        // Cache player layer once (static, shared across all instances)
        if (playerLayer == -1)
        {
            playerLayer = LayerMask.NameToLayer("Player");
        }
    }

    public void Initialize(EnemyData enemyData)
    {
        this.enemyData = enemyData;
    }

    public void Attack(GameObject target)
    {
        // Cache the health component to avoid repeated lookups while colliding.
        if (target != cachedTarget)
        {
            cachedTarget = target;
            cachedHealth = target.GetComponent<Health>();
        }

        if (cachedHealth != null)
        {
            Vector2 hitPoint = target.transform.position;
            Vector2 hitDirection = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
            cachedHealth.TakeDamage(enemyData.attackDamage, hitPoint, hitDirection);
        }
    }

    public void StopAttack()
    {
        // No continuous attack to stop for contact damage
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer != playerLayer)
        {
            return;
        }
        else if (nextAttackTime <= Time.time)
        {
            Attack(collision.gameObject);
            nextAttackTime = Time.time + enemyData.attackCooldown;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject == cachedTarget)
        {
            cachedTarget = null;
            cachedHealth = null;
        }
    }
}