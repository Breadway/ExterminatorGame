using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Electrician's jumper cables. Lightning arcs to the nearest enemy and chains onward, with
/// each link hitting 25% harder. Firing builds heat; at 100% the rig force-discharges, hurting
/// the Electrician and stunning them for a second ("Overload").
/// </summary>
public class JumperCablesAttack : WeaponBase, ICharacterConstraint
{
    [Header("Arc")]
    [SerializeField] private float range = 6f;
    [SerializeField] private float chainRange = 3.4f;
    [SerializeField] private int maxLinks = 5;
    [SerializeField] private float linkGrowth = 0.25f;
    [SerializeField] private float fireInterval = 0.22f;
    [SerializeField] private float baseDamage = 6f;

    [Header("Overload")]
    [SerializeField] private float heatPerShot = 6f;
    [SerializeField] private float coolPerSecond = 22f;
    [SerializeField] private float overloadSelfDamageFraction = 0.5f;
    [SerializeField] private float overloadStun = 1f;
    [SerializeField] private float overloadLockout = 1.8f;
    [SerializeField] private float explodeOnKillRadius = 2.2f;

    private float heat;
    private float nextShot;
    private float lockedUntil;
    private bool firing;
    private PlayerMovement movement;
    private LineRenderer line;
    private float lineHideAt;
    private readonly List<Enemy> chain = new List<Enemy>(8);

    public override string WeaponId => "cables";
    public string CharacterId => "electrician";
    public override float WeaponRange => range;
    public float Heat => heat;

    protected override void Awake()
    {
        base.Awake();
        movement = GetComponentInParent<PlayerMovement>();
        BuildLine();
    }

    void OnDisable()
    {
        heat = 0f;
        firing = false;
        if (line != null) line.enabled = false;
        GameEvents.WeaponMeter("HEAT", 0f, false);
    }

    private void BuildLine()
    {
        var go = new GameObject("Arc");
        go.transform.SetParent(transform, false);
        line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.widthMultiplier = 0.14f;
        line.numCapVertices = 2;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(0.7f, 0.9f, 1f, 1f);
        line.endColor = new Color(0.3f, 0.6f, 1f, 1f);
        line.sortingOrder = 20;
        line.enabled = false;
    }

    public override void Attack() { firing = true; }
    public override void StopAttack() { firing = false; }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        bool locked = Time.time < lockedUntil;
        if (firing && !locked && Time.time >= nextShot && firePoint != null) Fire();
        else if (!firing || locked) heat = Mathf.Max(0f, heat - coolPerSecond * Time.deltaTime);

        if (line != null && line.enabled && Time.time >= lineHideAt) line.enabled = false;
        GameEvents.WeaponMeter("HEAT", heat / 100f, heat >= 75f || locked);
    }

    private void Fire()
    {
        nextShot = Time.time + fireInterval;
        heat += heatPerShot;

        Vector2 origin = firePoint.position;
        Vector2 forward = firePoint.up;

        Enemy first = null;
        float best = float.MaxValue;
        foreach (var enemy in EnemiesInCircle(origin, range))
        {
            Vector2 to = (Vector2)enemy.transform.position - origin;
            if (Vector2.Dot(forward, to.normalized) < 0.55f) continue; // roughly where the player aims
            if (to.sqrMagnitude < best) { best = to.sqrMagnitude; first = enemy; }
        }

        if (first != null)
        {
            Chain(origin, first);
            GameEvents.WeaponFired(WeaponId, origin, forward, heat / 100f);
        }

        if (heat >= 100f) Overload();
    }

    private void Chain(Vector2 origin, Enemy first)
    {
        chain.Clear();
        chain.Add(first);
        var current = first;
        for (int link = 1; link < maxLinks; link++)
        {
            Enemy next = null;
            float best = float.MaxValue;
            foreach (var candidate in EnemiesInCircle(current.transform.position, chainRange))
            {
                if (chain.Contains(candidate)) continue;
                float d = ((Vector2)candidate.transform.position - (Vector2)current.transform.position).sqrMagnitude;
                if (d < best) { best = d; next = candidate; }
            }
            if (next == null) break;
            chain.Add(next);
            current = next;
        }

        float damage = RollDamage(baseDamage);
        line.positionCount = chain.Count + 1;
        line.SetPosition(0, origin);
        for (int i = 0; i < chain.Count; i++)
        {
            line.SetPosition(i + 1, chain[i].transform.position);
        }
        line.enabled = true;
        lineHideAt = Time.time + 0.08f;

        // Snapshot positions first: damage can return an enemy to the pool mid-loop.
        var targets = new List<Enemy>(chain);
        for (int i = 0; i < targets.Count; i++)
        {
            var e = targets[i];
            if (e == null || e.Health == null || !e.Health.IsAlive) continue;
            Vector2 pos = e.transform.position;
            e.Health.TakeDamage(damage * (1f + linkGrowth * i), pos, Vector2.up);
            if (!e.Health.IsAlive) ExplodeAt(pos, e);
        }
    }

    // A kill inside a chain detonates, hurting whatever stands next to it.
    private void ExplodeAt(Vector2 pos, Enemy dead)
    {
        float damage = RollDamage(baseDamage) * 1.2f;
        var near = new List<Enemy>(EnemiesInCircle(pos, explodeOnKillRadius));
        foreach (var e in near)
        {
            if (e == dead || e.Health == null || !e.Health.IsAlive) continue;
            e.Health.TakeDamage(damage, e.transform.position, Vector2.zero);
        }
    }

    private void Overload()
    {
        heat = 0f;
        lockedUntil = Time.time + overloadLockout;
        firing = false;
        if (ownerHealth != null)
        {
            float damage = ownerHealth.MaxHP * overloadSelfDamageFraction;
            // Never let our own malfunction be the killing blow on a nearly-full player at exactly 50%.
            ownerHealth.TakeDamage(damage, transform.position, Vector2.zero);
        }
        owner?.Stun(overloadStun);
        GameEvents.PlaySound("overload");
        GameEvents.HitStop(0.12f, 0.02f);
        GameEvents.ScreenShake(0.8f);
        GameEvents.ShowNotification("OVERLOAD");
    }
}
