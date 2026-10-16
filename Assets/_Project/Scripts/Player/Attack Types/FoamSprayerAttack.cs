using UnityEngine;

/// <summary>
/// The Intern's foam sprayer. Short cone that damages and slows. Spraying builds foam armor
/// (absorbs hits) but every point of armor slows the Intern; taking damage strips it off.
/// </summary>
public class FoamSprayerAttack : WeaponBase, ICharacterConstraint
{
    [Header("Spray")]
    [SerializeField] private float range = 3.2f;
    [SerializeField] private float coneDegrees = 55f;
    [SerializeField] private float tickRate = 0.14f;
    [SerializeField] private float damagePerTick = 3.5f;
    [SerializeField] private float slowMultiplier = 0.55f;
    [SerializeField] private float slowSeconds = 1.2f;

    [Header("Foam armor")]
    [SerializeField] private float maxFoam = 200f;
    [SerializeField] private float foamPerTick = 4f;
    [SerializeField] private float foamLostPerHitFraction = 0.35f;
    [Tooltip("100 foam slows by this much (0.5 = 50%).")]
    [SerializeField] private float slowAt100 = 0.5f;

    private float nextTick;
    private bool spraying;
    private float foam;
    private PlayerMovement movement;
    private readonly System.Collections.Generic.Dictionary<Enemy, float> slowedUntil = new System.Collections.Generic.Dictionary<Enemy, float>();

    public override string WeaponId => "foam";
    public string CharacterId => "intern";
    public override float WeaponRange => range;
    public float Foam => foam;

    protected override void Awake()
    {
        base.Awake();
        movement = GetComponentInParent<PlayerMovement>();
    }

    void OnEnable()
    {
        if (ownerHealth != null) ownerHealth.OnDamaged += HandleDamaged;
    }

    void OnDisable()
    {
        if (ownerHealth != null) ownerHealth.OnDamaged -= HandleDamaged;
        movement?.SetSpeedMultiplier(1f);
        foam = 0f;
        spraying = false;
        PublishMeter();
    }

    public override void Attack()
    {
        if (!spraying) GameEvents.WeaponHeld(WeaponId, true);
        spraying = true;
    }

    public override void StopAttack()
    {
        if (spraying) GameEvents.WeaponHeld(WeaponId, false);
        spraying = false;
    }

    void Update()
    {
        if (!spraying || Time.timeScale <= 0f || Time.time < nextTick || firePoint == null) return;
        nextTick = Time.time + ScaledInterval(tickRate);

        Vector2 origin = firePoint.position;
        Vector2 forward = firePoint.up;
        float cosHalf = Mathf.Cos(coneDegrees * 0.5f * Mathf.Deg2Rad);
        float damage = RollDamage(damagePerTick) * (damagePerTick / 5f);
        bool hit = false;

        foreach (var enemy in EnemiesInCircle(origin, range))
        {
            Vector2 to = (Vector2)enemy.transform.position - origin;
            float distance = to.magnitude;
            if (distance < 0.01f || Vector2.Dot(forward, to / distance) < cosHalf) continue;
            enemy.Health.TakeDamage(damage, enemy.transform.position, to.normalized);
            enemy.SetSpeedMultiplier(slowMultiplier);
            slowedUntil[enemy] = Time.time + slowSeconds;
            hit = true;
        }

        if (hit)
        {
            foam = Mathf.Min(maxFoam, foam + foamPerTick);
            ApplyFoam();
        }
    }

    void LateUpdate()
    {
        // Release slows on enemies the spray has left alone
        if (slowedUntil.Count == 0) return;
        var expired = new System.Collections.Generic.List<Enemy>();
        foreach (var kv in slowedUntil)
        {
            if (kv.Key == null || Time.time >= kv.Value) expired.Add(kv.Key);
        }
        foreach (var e in expired)
        {
            slowedUntil.Remove(e);
            if (e != null) e.SetSpeedMultiplier(1f);
        }
    }

    // Foam soaks damage by raising the player's armor stat while it lasts
    private void HandleDamaged(float amount, Vector2 point, Vector2 dir)
    {
        if (foam <= 0f) return;
        foam = Mathf.Max(0f, foam - maxFoam * foamLostPerHitFraction);
        ApplyFoam();
    }

    private void ApplyFoam()
    {
        float slow = Mathf.Clamp01(foam / 100f) * slowAt100;
        movement?.SetSpeedMultiplier(Mathf.Max(0.2f, 1f - slow));
        if (stats != null) stats.SetFoamArmor(foam);
        PublishMeter();
    }

    private void PublishMeter()
    {
        GameEvents.WeaponMeter("FOAM", maxFoam > 0f ? foam / maxFoam : 0f, foam >= maxFoam * 0.9f);
    }
}
