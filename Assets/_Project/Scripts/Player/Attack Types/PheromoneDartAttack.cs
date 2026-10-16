using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Apiarist's dart gun. It barely scratches enemies itself; its job is to tag them. Anything
/// tagged radios your bees, which is also how the unmarked-kill rule is enforced: the swarm refuses
/// to touch an enemy you have never marked.
/// </summary>
public class PheromoneDartAttack : WeaponBase
{
    [Header("Dart")]
    [SerializeField] private float range = 9f;
    [SerializeField] private float markSeconds = 10f;
    [SerializeField] private float fireInterval = 0.34f;
    [SerializeField] private float directDamage = 1f;

    private float nextShot;
    private bool firing;

    public override string WeaponId => "darts";
    public override float WeaponRange => range;

    public override void Attack() { firing = true; }
    public override void StopAttack() { firing = false; }

    void Update()
    {
        if (!firing || Time.timeScale <= 0f || owner != null && owner.IsStunned) return;
        if (firePoint == null || Time.time < nextShot) return;
        nextShot = Time.time + ScaledInterval(fireInterval);

        Vector2 origin = firePoint.position;
        Vector2 forward = firePoint.up;
        Enemy best = null;
        float bestD = float.MaxValue;
        foreach (var enemy in EnemiesInCircle(origin, range))
        {
            Vector2 to = (Vector2)enemy.transform.position - origin;
            if (to.sqrMagnitude < 0.01f || Vector2.Dot(forward, to.normalized) < 0.5f) continue;
            if (to.sqrMagnitude < bestD) { bestD = to.sqrMagnitude; best = enemy; }
        }
        if (best == null) return;

        best.Health.TakeDamage(directDamage, best.transform.position, (best.transform.position - firePoint.position).normalized);
        var hive = GetComponent<BeeHive>();
        hive?.Mark(best, markSeconds);
        GameEvents.WeaponFired(WeaponId, origin, forward, 0.3f);
        if (Fx.Instance != null) Fx.Instance.Spark(best.transform.position, 3, new Color(1f, 0.85f, 0.3f), 0.1f);
    }
}
