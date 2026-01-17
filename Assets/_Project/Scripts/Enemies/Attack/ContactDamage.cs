using UnityEngine;

public class ContactDamage : MonoBehaviour, IAttackBehaviour
{
    private EnemyData enemyData;
    private float nextAttackTime = 0f;

    public void Initialize(EnemyData enemyData)
    {
        this.enemyData = enemyData;
    }

    public void Attack(GameObject target)
    {
        // Implement contact damage logic here
        Health health = target.GetComponent<Health>();
        if (health != null)
        {
            Vector3 hitPoint = target.transform.position;
            Vector3 hitDirection = (target.transform.position - transform.position).normalized;
            if (health != null && hitPoint != null && hitDirection != null)
            health.TakeDamage(enemyData.attackDamage, hitPoint, hitDirection);
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
}