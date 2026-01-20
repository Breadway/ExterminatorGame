using UnityEngine;

public class ContactDamage : MonoBehaviour, IAttackBehaviour
{
    private EnemyData enemyData;
    private float nextAttackTime = 0f;
    private GameObject cachedTarget;
    private Health cachedHealth;

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
            Vector3 hitPoint = target.transform.position;
            Vector3 hitDirection = (target.transform.position - transform.position).normalized;
            cachedHealth.TakeDamage(enemyData.attackDamage, hitPoint, hitDirection);
        }
    }

    public void StopAttack()
    {
        // No continuous attack to stop for contact damage
    }

    private void OnCollisionStay(Collision collision)
    {
        if (nextAttackTime <= Time.time)
        {
            Attack(collision.gameObject);
            nextAttackTime = Time.time + enemyData.attackCooldown;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject == cachedTarget)
        {
            cachedTarget = null;
            cachedHealth = null;
        }
    }
}