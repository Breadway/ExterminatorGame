using UnityEngine;

/// <summary>
/// The Custodian's mop. A wide swing with long reach. The outer third of the arc deals triple
/// damage; targets inside the guard radius take nothing ("Safety Violation"), so spacing matters.
/// </summary>
public class MopAttack : WeaponBase
{
    [Header("Mop")]
    [SerializeField] private float reach = 3.6f;
    [SerializeField] private float arcDegrees = 140f;
    [SerializeField] private float swingCooldown = 0.36f;
    [SerializeField] [Range(0f, 1f)] private float guardFraction = 0.3f;
    [SerializeField] private float outerFraction = 0.6f;
    [SerializeField] private float sweetSpotMultiplier = 3f;
    [SerializeField] private float knockback = 6f;
    [SerializeField] private float baseDamage = 12f;
    [Header("Splat")]
    [Tooltip("Bonus damage when a sweet-spot hit slams an enemy into a wall.")]
    [SerializeField] private float splatMultiplier = 2f;
    [Tooltip("How far the enemy is thrown for the wall check.")]
    [SerializeField] private float splatThrowDistance = 1.2f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    private float nextSwing;

    public override string WeaponId => "mop";
    public override float WeaponRange => reach;
    public float GuardRadius => reach * guardFraction;

    public override void Attack()
    {
        if (Time.time < nextSwing || firePoint == null) return;
        nextSwing = Time.time + swingCooldown;

        Vector2 origin = firePoint.position;
        Vector2 forward = firePoint.up;
        GameEvents.WeaponFired(WeaponId, origin, forward, 0f);
        float halfArcCos = Mathf.Cos(arcDegrees * 0.5f * Mathf.Deg2Rad);
        float damage = RollDamage(baseDamage);
        bool hitAny = false;
        bool sweetHit = false;

        // Spacing shove: every swing pushes the whole crowd back a step (no damage), so nothing
        // can camp inside the guard forever. The guard still blocks all mop damage.
        foreach (var enemy in EnemiesInCircle(origin, reach + 0.6f))
        {
            if (enemy == null || enemy.Health == null || !enemy.Health.IsAlive) continue;
            Vector2 away = (Vector2)enemy.transform.position - origin;
            if (away.sqrMagnitude < 0.0001f) continue;
            var body = enemy.GetComponent<Rigidbody2D>();
            if (body != null) body.position += away.normalized * 0.35f;
        }

        bool breached = false;
        foreach (var enemy in EnemiesInCircle(origin, reach + 0.4f))
        {
            Vector2 to = (Vector2)enemy.transform.position - origin;
            float distance = to.magnitude;
            if (distance < 0.01f) continue;
            if (Vector2.Dot(forward, to / distance) < halfArcCos) continue;
            if (distance < GuardRadius)
            {
                // Inside the guard the mop cannot hurt it: flag the danger indicator instead
                if (enemy.Health != null && enemy.Health.IsAlive) breached = true;
                continue;
            }

            float factor = distance >= reach * outerFraction ? sweetSpotMultiplier : 1f;
            enemy.Health.TakeDamage(damage * factor, enemy.transform.position, to.normalized);
            // Getting smacked with a mop staggers: the victim's next attack comes later
            enemy.GetComponent<ContactDamage>()?.DelayNextAttack(0.45f);
            hitAny = true;
            if (factor > 1f) sweetHit = true;

            // Every hit shoves a little so enemies do not pile up inside the guard;
            // sweet-spot hits throw hard enough to splat them into walls.
            if (enemy != null && enemy.Health.IsAlive)
            {
                Vector2 dir = to.normalized;
                var rb = enemy.GetComponent<Rigidbody2D>();
                if (rb != null) rb.position += dir * (knockback * (factor > 1f ? 0.1f : 0.12f));
                // Splat: a sweet-spot hit that throws the enemy into a wall deals bonus damage
                if (factor > 1f)
                {
                    var wall = Physics2D.Raycast(enemy.transform.position, dir, splatThrowDistance, obstacleMask);
                    if (wall.collider != null && wall.collider.GetComponentInParent<Health>() == null)
                    {
                        enemy.Health.TakeDamage(damage * splatMultiplier, wall.point, dir);
                        GameEvents.PlaySound("mop_hit", wall.point);
                        GameEvents.ShowNotification("SPLAT");
                    }
                }
            }
        }
        GameEvents.GuardBreach(breached);
        if (hitAny)
        {
            GameEvents.ScreenShake(sweetHit ? 0.3f : 0.12f);
            GameEvents.HitStop(sweetHit ? 0.07f : 0.03f, 0.04f);
            if (sweetHit) { GameEvents.PlaySound("mop_hit", origin + forward * reach); GameEvents.ZoomPunch(0.4f); }
        }
    }
}
