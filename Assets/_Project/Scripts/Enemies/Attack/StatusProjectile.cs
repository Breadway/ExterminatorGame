using UnityEngine;

/// <summary>
/// A projectile that carries more than damage: on impact it can apply a status effect (burning,
/// poison, webs, shock) and leave a floor hazard behind (a web trap, an acid splash, scorched ground).
/// All behaviour is configured on the prefab, so new ammo types need no code.
/// </summary>
public class StatusProjectile : Projectile
{
    [Header("Status")]
    [Tooltip("Applied to whatever the projectile hits. Empty = damage only.")]
    [SerializeField] private StatusEffectData statusEffect;
    [SerializeField] [Range(0f, 1f)] private float statusChance = 1f;

    [Header("Impact hazard")]
    [Tooltip("Floor effect left where the projectile lands.")]
    [SerializeField] private bool leaveHazard;
    [SerializeField] private HazardKind hazardKind = HazardKind.Web;
    [SerializeField] private float hazardRadius = 1.1f;
    [SerializeField] private float hazardLifetime = 5f;
    [SerializeField] private float hazardDps;
    [SerializeField] private float hazardSlow = 0.5f;
    [SerializeField] private bool hazardHurtsPlayer = true;
    [SerializeField] private bool hazardHurtsEnemies;

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // Apply the payload first; the base method then deals damage and releases the projectile.
        if (statusEffect != null && Random.value < statusChance)
        {
            var controller = other.GetComponentInParent<StatusEffectController>();
            if (controller != null) controller.ApplyEffect(statusEffect);
        }
        if (leaveHazard && HazardField.Instance != null)
        {
            HazardField.Instance.Spawn(hazardKind, transform.position, hazardRadius, hazardLifetime, hazardDps, hazardHurtsPlayer, hazardHurtsEnemies, hazardSlow);
        }
        base.OnTriggerEnter2D(other);
    }
}
