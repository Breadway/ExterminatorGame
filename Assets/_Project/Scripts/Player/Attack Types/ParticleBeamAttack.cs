using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Scientist's prototype beam. Hold to charge, release to fire. A full charge pierces every
/// enemy (and walls) along a long line for huge damage; a tap is a weak snap shot. Holding above
/// 75% risks a critical failure ("Critical Instability"): self damage and a stun.
/// </summary>
public class ParticleBeamAttack : WeaponBase, IHoldWeapon, ICharacterConstraint
{
    [Header("Beam")]
    [SerializeField] private float beamLength = 14f;
    [SerializeField] private float beamWidth = 0.9f;
    [SerializeField] private float chargeSeconds = 1.6f;
    [SerializeField] private float tapDamage = 4f;
    [SerializeField] private float fullDamage = 70f;
    [SerializeField] private float tapCooldown = 0.18f;

    [Header("Critical Instability")]
    [SerializeField] [Range(0f, 1f)] private float instabilityThreshold = 0.75f;
    [SerializeField] private float failChancePerSecond = 0.05f;
    [SerializeField] private float failSelfDamageFraction = 0.3f;
    [SerializeField] private float failStun = 1f;

    private bool holding;
    private float holdStart;
    private float nextTap;
    private float chargeAtLastTick;
    private LineRenderer line;
    private float lineHideAt;

    public override string WeaponId => "beam";
    public string CharacterId => "scientist";
    public override float WeaponRange => beamLength;
    public float EffectiveChargeSeconds()
    {
        float rate = stats != null ? Mathf.Max(0.2f, stats.currentAttackSpeed) : 1.2f;
        return chargeSeconds * 1.2f / rate;
    }

    public float Charge => holding ? Mathf.Clamp01((Time.time - holdStart) / EffectiveChargeSeconds()) : 0f;

    protected override void Awake()
    {
        base.Awake();
        var go = new GameObject("Beam");
        go.transform.SetParent(transform, false);
        line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.sortingOrder = 20;
        line.enabled = false;
    }

    void OnDisable()
    {
        holding = false;
        if (line != null) line.enabled = false;
        GameEvents.WeaponMeter("CHARGE", 0f, false);
    }

    // PlayerAttack drives hold weapons with Begin/Release. Attack() is a no-op for them.
    public override void Attack() { }
    public override void StopAttack() { holding = false; }

    public void BeginHold()
    {
        if (Time.timeScale <= 0f) return;
        holding = true;
        holdStart = Time.time;
        chargeAtLastTick = 0f;
        GameEvents.WeaponHeld(WeaponId, true);
    }

    public void ReleaseHold()
    {
        if (!holding) return;
        float charge = Charge;
        holding = false;
        GameEvents.WeaponHeld(WeaponId, false);
        Fire(charge);
        GameEvents.WeaponMeter("CHARGE", 0f, false);
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (line != null && line.enabled && Time.time >= lineHideAt) line.enabled = false;
        if (!holding) return;

        float charge = Charge;
        GameEvents.WeaponMeter("CHARGE", charge, charge >= instabilityThreshold);

        // Failure is a per-second chance, evaluated over only the time spent above the threshold
        if (charge >= instabilityThreshold)
        {
            float from = Mathf.Max(chargeAtLastTick, instabilityThreshold);
            float seconds = (charge - from) * EffectiveChargeSeconds();
            if (seconds > 0f && Random.value < failChancePerSecond * seconds) CriticalFailure();
        }
        chargeAtLastTick = charge;
    }

    private void Fire(float charge)
    {
        if (firePoint == null) return;
        bool full = charge >= 0.99f;
        if (!full && Time.time < nextTap) return;
        if (!full) nextTap = Time.time + tapCooldown;

        Vector2 origin = firePoint.position;
        Vector2 dir = firePoint.up;
        float damage = Mathf.Lerp(tapDamage, fullDamage, charge * charge) * (stats != null ? stats.currentDamage / 5f : 1f);
        if (stats != null && Random.value < stats.currentCritChance) damage *= stats.currentCritMultiplier;

        float length = beamLength;
        // A partial charge stops at the first wall; a full charge pierces everything.
        if (!full)
        {
            var wall = Physics2D.Raycast(origin, dir, beamLength, ~enemyMask.value & ~(1 << gameObject.layer));
            if (wall.collider != null && wall.collider.GetComponentInParent<Health>() == null) length = wall.distance;
        }

        var hits = Physics2D.CircleCastAll(origin, beamWidth * 0.5f, dir, length, enemyMask);
        var seen = new HashSet<Enemy>();
        foreach (var hit in hits)
        {
            var enemy = hit.collider != null ? hit.collider.GetComponentInParent<Enemy>() : null;
            if (enemy == null || !seen.Add(enemy) || enemy.Health == null || !enemy.Health.IsAlive) continue;
            enemy.Health.TakeDamage(damage, hit.point, dir);
        }

        DrawBeam(origin, origin + dir * length, charge);
        GameEvents.WeaponFired(WeaponId, origin, dir, charge);
        if (full) GameEvents.HitStop(0.08f, 0.03f);
        if (charge > 0.5f) GameEvents.ScreenShake(Mathf.Lerp(0.1f, 0.5f, charge));
    }

    private void DrawBeam(Vector2 from, Vector2 to, float charge)
    {
        // Draw from the barrel tip, not the body centre. Damage still uses the fire point.
        Vector2 dir = (to - from).normalized;
        float barrel = 0.75f;
        if (Vector2.Distance(from, to) > barrel) from += dir * barrel;

        // A thin hot core reads as a beam; the old 0.7 wide slab hid the enemies it was hitting
        line.widthMultiplier = Mathf.Lerp(0.06f, 0.42f, charge);
        line.startColor = Color.Lerp(new Color(0.6f, 1f, 0.9f, 1f), Color.white, charge);
        line.endColor = new Color(0.2f, 0.9f, 0.7f, 0.15f);
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.enabled = true;
        lineHideAt = Time.time + Mathf.Lerp(0.06f, 0.16f, charge);
    }

    private void CriticalFailure()
    {
        holding = false;
        GameEvents.WeaponHeld(WeaponId, false);
        GameEvents.PlaySound("overload");
        GameEvents.WeaponMeter("CHARGE", 0f, false);
        if (ownerHealth != null)
            ownerHealth.TakeDamage(ownerHealth.MaxHP * failSelfDamageFraction, transform.position, Vector2.zero);
        owner?.Stun(failStun);
        GameEvents.ScreenShake(0.8f);
        GameEvents.ShowNotification("CRITICAL INSTABILITY");
    }
}
