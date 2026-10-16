using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Weapon behaviour tests that run in Edit Mode: they place real Enemy objects and call the weapon
/// directly. Physics2D queries work in Edit Mode once transforms are synced, so no Play Mode is needed.
/// </summary>
public class WeaponTests
{
    private readonly System.Collections.Generic.List<UnityEngine.Object> created = new System.Collections.Generic.List<UnityEngine.Object>();
    private int enemyLayer;

    private static Type T(string name)
    {
        return AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    }

    private X Track<X>(X obj) where X : UnityEngine.Object { created.Add(obj); return obj; }

    private static object Call(object target, string name, params object[] args)
    {
        return target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    }

    private static void Set(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    [SetUp]
    public void SetUp()
    {
        enemyLayer = LayerMask.NameToLayer("Enemy");
        Assert.That(enemyLayer, Is.GreaterThanOrEqualTo(0), "The 'Enemy' layer must exist.");
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var o in created) if (o != null) UnityEngine.Object.DestroyImmediate(o);
        created.Clear();
        T("GameEvents").GetMethod("ClearAllListeners").Invoke(null, null);
        Time.timeScale = 1f;
    }

    // A player with a fire point facing +Y (up) at the origin, and the weapon under test.
    private Component Weapon(string typeName, out GameObject player)
    {
        player = Track(new GameObject("Player"));
        player.AddComponent<Rigidbody2D>();
        player.AddComponent<BoxCollider2D>();
        RunAwake(player.AddComponent(T("Health")));
        var fire = new GameObject("FirePoint");
        fire.transform.SetParent(player.transform, false);
        var weapon = player.AddComponent(T(typeName));
        RunAwake(weapon);
        Set(weapon, "firePoint", fire.transform);
        Set(weapon, "enemyMask", (LayerMask)(1 << enemyLayer));
        return weapon;
    }

    // A bare enemy: health plus an Enemy component is enough for the weapons' queries.
    private Component Enemy(Vector2 position, float hp = 100f)
    {
        var go = Track(new GameObject("Enemy"));
        go.layer = enemyLayer;
        go.transform.position = position;
        go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<CircleCollider2D>().radius = 0.4f;
        go.AddComponent(T("EnemyAttack"));
        var health = go.AddComponent(T("Health"));
        RunAwake(health);
        Call(health, "SetMaxHP", hp);
        Call(health, "ResetHealth");
        var enemy = go.AddComponent(T("Enemy"));
        RunAwake(enemy);
        return enemy;
    }

    private static float Hp(Component enemy)
    {
        var health = enemy.GetComponent(T("Health"));
        return (float)T("Health").GetProperty("CurrentHP").GetValue(health);
    }

    private static void Sync() { Physics2D.SyncTransforms(); }

    // Edit Mode does not run Awake after AddComponent, and these classes build their state there.
    private static void RunAwake(Component component)
    {
        var awake = component.GetType().GetMethod("Awake", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        awake?.Invoke(component, null);
    }

    // ---------- Mop ----------

    [Test]
    public void MopHitsTheOuterArcForTripleDamage()
    {
        var mop = Weapon("MopAttack", out _);
        var inner = Enemy(new Vector2(0f, 2.0f));   // between the guard and the sweet spot
        var outer = Enemy(new Vector2(0f, 3.3f));   // outer third of the swing
        Sync();
        Call(mop, "Attack");
        float innerLoss = 100f - Hp(inner);
        float outerLoss = 100f - Hp(outer);
        Assert.That(innerLoss, Is.GreaterThan(0f), "Mid-range target should be hit.");
        Assert.That(outerLoss, Is.EqualTo(innerLoss * 3f).Within(0.01f), "Outer arc deals 3x.");
    }

    [Test]
    public void MopCannotHurtEnemiesInsideTheGuardRadius()
    {
        var mop = Weapon("MopAttack", out _);
        var close = Enemy(new Vector2(0f, 0.8f));
        var control = Enemy(new Vector2(0f, 2.2f)); // proves the swing itself works
        Sync();
        Call(mop, "Attack");
        Assert.That(Hp(control), Is.LessThan(100f), "Control target outside the guard must be hit.");
        Assert.That(Hp(close), Is.EqualTo(100f), "Safety Violation: no damage inside the guard.");
    }

    [Test]
    public void MopMissesEnemiesBehindThePlayer()
    {
        var mop = Weapon("MopAttack", out _);
        var behind = Enemy(new Vector2(0f, -3f));
        var front = Enemy(new Vector2(0f, 2.2f));
        Sync();
        Call(mop, "Attack");
        Assert.That(Hp(front), Is.LessThan(100f), "Control target in front must be hit.");
        Assert.That(Hp(behind), Is.EqualTo(100f));
    }

    [Test]
    public void MopRespectsItsSwingCooldown()
    {
        var mop = Weapon("MopAttack", out _);
        var target = Enemy(new Vector2(0f, 2.2f));
        Sync();
        Call(mop, "Attack");
        float afterFirst = Hp(target);
        Assert.That(afterFirst, Is.LessThan(100f), "The first swing must land.");
        Call(mop, "Attack");
        Assert.That(Hp(target), Is.EqualTo(afterFirst), "A second swing inside the cooldown does nothing.");
    }

    // ---------- Jumper cables ----------

    [Test]
    public void CablesChainToNeighboursWithGrowingDamage()
    {
        var cables = Weapon("JumperCablesAttack", out _);
        var first = Enemy(new Vector2(0f, 3f));
        var second = Enemy(new Vector2(0f, 5f));
        var third = Enemy(new Vector2(0f, 7f));
        Sync();
        Call(cables, "Attack");
        Call(cables, "Update"); // fires on the next frame while the button is held
        float l1 = 100f - Hp(first), l2 = 100f - Hp(second), l3 = 100f - Hp(third);
        Assert.That(l1, Is.GreaterThan(0f));
        Assert.That(l2, Is.GreaterThan(l1), "Each link hits 25% harder.");
        Assert.That(l3, Is.GreaterThan(l2));
    }

    [Test]
    public void CablesIgnoreEnemiesBehindTheAim()
    {
        var cables = Weapon("JumperCablesAttack", out _);
        var behind = Enemy(new Vector2(0f, -3f));
        var front = Enemy(new Vector2(0f, 3f));
        Sync();
        Call(cables, "Attack");
        Call(cables, "Update");
        Assert.That(Hp(front), Is.LessThan(100f), "Control target in front must be hit.");
        Assert.That(Hp(behind), Is.EqualTo(100f));
    }

    // ---------- Foam sprayer ----------

    [Test]
    public void FoamBuildsArmorOnlyWhenItHitsSomething()
    {
        var foam = Weapon("FoamSprayerAttack", out _);
        Sync();
        Call(foam, "Attack");
        Call(foam, "Update");
        Assert.That((float)T("FoamSprayerAttack").GetProperty("Foam").GetValue(foam), Is.EqualTo(0f), "Spraying air builds no foam.");

        var target = Enemy(new Vector2(0f, 2f));
        Sync();
        Set(foam, "nextTick", 0f);
        Call(foam, "Update");
        Assert.That(Hp(target), Is.LessThan(100f), "Spray damages what it hits.");
        Assert.That((float)T("FoamSprayerAttack").GetProperty("Foam").GetValue(foam), Is.GreaterThan(0f), "A hit builds foam.");
    }

    // ---------- Particle beam ----------

    [Test]
    public void FullChargeBeamPiercesEveryEnemyOnTheLine()
    {
        var beam = Weapon("ParticleBeamAttack", out _);
        var a = Enemy(new Vector2(0f, 3f));
        var b = Enemy(new Vector2(0f, 6f));
        var c = Enemy(new Vector2(0f, 9f));
        Sync();
        Call(beam, "BeginHold");
        Set(beam, "holdStart", Time.time - 10f); // held far past full charge
        Call(beam, "ReleaseHold");
        Assert.That(Hp(a), Is.LessThan(100f));
        Assert.That(Hp(b), Is.LessThan(100f));
        Assert.That(Hp(c), Is.LessThan(100f));
    }

    [Test]
    public void FullChargeOutdamagesATap()
    {
        var tapBeam = Weapon("ParticleBeamAttack", out _);
        var tapTarget = Enemy(new Vector2(0f, 3f));
        Sync();
        Call(tapBeam, "BeginHold");
        Call(tapBeam, "ReleaseHold");
        float tapLoss = 100f - Hp(tapTarget);

        var fullBeam = Weapon("ParticleBeamAttack", out var player2);
        player2.transform.position = new Vector3(50f, 0f, 0f);
        var fullTarget = Enemy(new Vector2(50f, 3f));
        Sync();
        Call(fullBeam, "BeginHold");
        Set(fullBeam, "holdStart", Time.time - 10f);
        Call(fullBeam, "ReleaseHold");
        float fullLoss = 100f - Hp(fullTarget);

        Assert.That(fullLoss, Is.GreaterThan(tapLoss * 5f), "A full charge is a different weapon from a tap.");
    }

    [Test]
    public void ReleasingWithoutHoldingDoesNothing()
    {
        var beam = Weapon("ParticleBeamAttack", out _);
        var target = Enemy(new Vector2(0f, 3f));
        Sync();
        Call(beam, "ReleaseHold");
        Assert.That(Hp(target), Is.EqualTo(100f));
    }

    // ---------- Loadout ----------

    [Test]
    public void EachWeaponReportsItsOwnId()
    {
        var ids = new[] { ("MopAttack", "mop"), ("FoamSprayerAttack", "foam"), ("JumperCablesAttack", "cables"), ("ParticleBeamAttack", "beam") };
        foreach (var (type, id) in ids)
        {
            var weapon = Weapon(type, out _);
            Assert.That(T(type).GetProperty("WeaponId").GetValue(weapon), Is.EqualTo(id));
        }
    }
}
