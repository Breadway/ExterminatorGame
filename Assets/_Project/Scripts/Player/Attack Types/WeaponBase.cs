using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared plumbing for the non-flamethrower weapons: aim origin, damage lookup, target filtering
/// and cheap enemy queries. Subclasses only decide WHAT gets hit and WHEN.
/// </summary>
public abstract class WeaponBase : MonoBehaviour, IWeaponAttack
{
    [Header("Weapon")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected LayerMask enemyMask;

    protected PlayerStats stats;
    protected Health ownerHealth;
    protected PlayerController owner;
    private static readonly Collider2D[] buffer = new Collider2D[64];

    public abstract string WeaponId { get; }
    public abstract float WeaponRange { get; }
    public abstract void Attack();
    public virtual void StopAttack() { }

    protected virtual void Awake()
    {
        owner = GetComponentInParent<PlayerController>();
        ownerHealth = GetComponentInParent<Health>();
        if (firePoint == null)
        {
            var named = owner != null ? owner.transform.Find("FirePoint") : null;
            firePoint = named != null ? named : transform;
        }
        if (enemyMask.value == 0)
        {
            int layer = LayerMask.NameToLayer("Enemy");
            if (layer >= 0) enemyMask = 1 << layer;
        }
    }

    public virtual void Initialize(PlayerStats playerStats)
    {
        stats = playerStats;
    }

    protected float RollDamage(float fallback)
    {
        return stats != null ? stats.CalculateDamage() : fallback;
    }

    /// <summary>Scale a fixed weapon timer by attack speed, so attack-speed upgrades matter for
    /// every weapon. 1.2 is the base attacks/sec all characters start with.</summary>
    protected float ScaledInterval(float baseSeconds)
    {
        float rate = stats != null ? Mathf.Max(0.2f, stats.currentAttackSpeed) : 1.2f;
        return baseSeconds * 1.2f / rate;
    }

    /// <summary>Enemies within radius of a point. The returned list is reused; do not hold it.</summary>
    protected List<Enemy> EnemiesInCircle(Vector2 center, float radius)
    {
        results.Clear();
        int hits = Physics2D.OverlapCircle(center, radius, new ContactFilter2D { useLayerMask = true, layerMask = enemyMask, useTriggers = true }, buffer);
        for (int i = 0; i < hits; i++)
        {
            var enemy = buffer[i] != null ? buffer[i].GetComponentInParent<Enemy>() : null;
            if (enemy != null && enemy.Health != null && enemy.Health.IsAlive && !results.Contains(enemy)) results.Add(enemy);
        }
        return results;
    }

    private readonly List<Enemy> results = new List<Enemy>(32);
}
