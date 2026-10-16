using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Runtime scripts still use Unity's predefined Assembly-CSharp assembly.
/// Reflection keeps tests isolated without moving runtime scripts into an asmdef,
/// which would change assembly-qualified references in existing assets.
/// </summary>
public class CoreGameplayTests {
    private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

    private static Type RuntimeType(string name) {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(name)).First(t => t != null);
    }

    private T Track<T>(T obj) where T : UnityEngine.Object {
        created.Add(obj);
        return obj;
    }

    private object Upgrade() {
        return Track(ScriptableObject.CreateInstance(RuntimeType("UpgradeData")));
    }

    private static object Call(object target, string name, params object[] args) {
        return target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(target, args);
    }

    private static void Set(object target, string name, object value) {
        target.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(target, value);
    }

    private static T Get<T>(object target, string name) {
        return (T)target.GetType().GetProperty(name).GetValue(target);
    }

    private Component Health() {
        var go = Track(new GameObject("Test health"));
        var health = go.AddComponent(RuntimeType("Health"));
        Call(health, "ResetHealth");
        return health;
    }

    [TearDown]
    public void TearDown() {
        foreach (var obj in created) {
            if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        }
        created.Clear();
        RuntimeType("GameEvents").GetMethod("ClearAllListeners").Invoke(null, null);
        Time.timeScale = 1f;
    }

    [Test]
    public void UpgradeWithUnsetRestrictionsIsValid() {
        var stats = Activator.CreateInstance(RuntimeType("PlayerStats"));
        Assert.That(Call(Upgrade(), "CanBeTaken", stats), Is.True);
    }

    [Test]
    public void UpgradeStackLimitIsEnforcedAtStatsBoundary() {
        var stats = Activator.CreateInstance(RuntimeType("PlayerStats"));
        var upgrade = Upgrade();
        Set(upgrade, "maxStacks", 1);
        Call(stats, "ApplyUpgrade", upgrade);
        Call(stats, "ApplyUpgrade", upgrade);
        Assert.That(Call(stats, "GetUpgradeCount"), Is.EqualTo(1));
        Assert.That(Call(upgrade, "CanBeTaken", stats), Is.False);
    }

    [Test]
    public void UnlimitedUpgradeCanBeTakenRepeatedly() {
        var stats = Activator.CreateInstance(RuntimeType("PlayerStats"));
        var upgrade = Upgrade();
        for (int i = 0; i < 5; i++) Call(stats, "ApplyUpgrade", upgrade);
        Assert.That(Call(stats, "GetUpgradeCount"), Is.EqualTo(5));
    }

    [Test]
    public void ConflictsAreSymmetric() {
        var stats = Activator.CreateInstance(RuntimeType("PlayerStats"));
        var first = Upgrade();
        var second = Upgrade();
        Array conflicts = Array.CreateInstance(RuntimeType("UpgradeData"), 1);
        conflicts.SetValue(second, 0);
        Set(first, "conflictingUpgrades", conflicts);
        Call(stats, "ApplyUpgrade", first);
        Assert.That(Call(second, "CanBeTaken", stats), Is.False);
    }

    [Test]
    public void PrerequisiteBlocksUpgradeUntilAcquired() {
        var stats = Activator.CreateInstance(RuntimeType("PlayerStats"));
        var prerequisite = Upgrade();
        var upgrade = Upgrade();
        Array prerequisites = Array.CreateInstance(RuntimeType("UpgradeData"), 1);
        prerequisites.SetValue(prerequisite, 0);
        Set(upgrade, "requiredUpgrades", prerequisites);
        Assert.That(Call(upgrade, "CanBeTaken", stats), Is.False);
        Call(stats, "ApplyUpgrade", prerequisite);
        Assert.That(Call(upgrade, "CanBeTaken", stats), Is.True);
    }

    [Test]
    public void NegativeDamageAndHealingCannotChangeHealth() {
        var health = Health();
        Call(health, "TakeDamage", -10f, Vector2.zero, Vector2.zero);
        Call(health, "Heal", -10f);
        Assert.That(Get<float>(health, "CurrentHP"), Is.EqualTo(Get<float>(health, "MaxHP")));
    }

    [Test]
    public void InvalidNumbersCannotPoisonHealth() {
        var health = Health();
        Call(health, "TakeDamage", float.NaN, Vector2.zero, Vector2.zero);
        Call(health, "Heal", float.PositiveInfinity);
        Call(health, "SetMaxHP", float.NaN);
        Assert.That(Get<float>(health, "CurrentHP"), Is.EqualTo(Get<float>(health, "MaxHP")));
    }

    [Test]
    public void DeadHealthCannotBeHealedButCanBeResetForPooling() {
        var health = Health();
        Call(health, "TakeDamage", 10000f, Vector2.zero, Vector2.zero);
        Call(health, "Heal", 100f);
        Assert.That(Get<bool>(health, "IsAlive"), Is.False);
        Call(health, "ResetHealth");
        Assert.That(Get<bool>(health, "IsAlive"), Is.True);
    }

    [Test]
    public void ZeroMaxHealthIsClampedAndPercentageIsFinite() {
        var health = Health();
        Call(health, "SetMaxHP", 0f);
        Assert.That(Get<float>(health, "MaxHP"), Is.EqualTo(1f));
        Assert.That(float.IsNaN(Get<float>(health, "HealthPercent")), Is.False);
    }

    [Test]
    public void ArmorDoesNotTurnZeroDamageIntoDamage() {
        var stats = Activator.CreateInstance(RuntimeType("PlayerStats"));
        Assert.That(Call(stats, "ApplyArmorToDamage", 0f), Is.EqualTo(0f));
        Assert.That(Call(stats, "ApplyArmorToDamage", -1f), Is.EqualTo(0f));
    }

    [Test]
    public void UpgradeOffersContainDistinctAssetsEvenWhenPoolContainsDuplicates() {
        var go = Track(new GameObject("Test upgrade manager"));
        var manager = go.AddComponent(RuntimeType("UpgradeManager"));
        var upgrade = Upgrade();
        Array pool = Array.CreateInstance(RuntimeType("UpgradeData"), 2);
        pool.SetValue(upgrade, 0);
        pool.SetValue(upgrade, 1);
        Set(manager, "allUpgrades", pool);
        var stats = Activator.CreateInstance(RuntimeType("PlayerStats"));
        var choices = (Array)Call(manager, "GetRandomUpgradeChoices", stats);
        Assert.That(choices.Length, Is.EqualTo(1));
    }

    [Test]
    public void InvalidUpgradeSelectionDoesNotApplyAnything() {
        var go = Track(new GameObject("Test upgrade manager"));
        var manager = go.AddComponent(RuntimeType("UpgradeManager"));
        Assert.That(Call(manager, "ChooseUpgrade", -1), Is.False);
        Assert.That(Call(manager, "ChooseUpgrade", 0), Is.False);
    }

    [Test]
    public void XPOverflowAwardsEveryLevelAndKeepsRemainder() {
        var go = Track(new GameObject("Test XP manager"));
        var manager = go.AddComponent(RuntimeType("XPManager"));
        Set(manager, "xpToNextLevel", 100);
        Set(manager, "xpMultiplier", 1.5f);
        Call(manager, "AddXP", 275);
        Assert.That(Call(manager, "GetCurrentLevel"), Is.EqualTo(3));
        Assert.That(Call(manager, "GetCurrentXP"), Is.EqualTo(25));
        Assert.That(Call(manager, "GetXPToNextLevel"), Is.EqualTo(225));
    }

    [Test]
    public void NegativeXPIsIgnored() {
        var go = Track(new GameObject("Test XP manager"));
        var manager = go.AddComponent(RuntimeType("XPManager"));
        Call(manager, "AddXP", -100);
        Assert.That(Call(manager, "GetCurrentXP"), Is.EqualTo(0));
    }

    [Test]
    public void UnrelatedEnemyDeathDoesNotChangeWaveCount() {
        var go = Track(new GameObject("Test wave spawner"));
        var spawner = go.AddComponent(RuntimeType("WaveSpawner"));
        Set(spawner, "enemiesAlive", 3);
        Call(spawner, "OnEnemyDied", new object[] { null });
        Assert.That(Get<int>(spawner, "EnemiesAlive"), Is.EqualTo(3));
    }

    [Test]
    public void FlatAndMultiplicativeBonusesAreIndependentOfAcquisitionOrder() {
        var firstStats = Activator.CreateInstance(RuntimeType("PlayerStats"));
        var secondStats = Activator.CreateInstance(RuntimeType("PlayerStats"));
        var flat = Upgrade();
        var multiplier = Upgrade();
        Set(flat, "healthBonus", 20f);
        Set(flat, "damageBonus", 5f);
        Set(multiplier, "healthMultiplier", 2f);
        Set(multiplier, "damageMultiplier", 2f);
        Call(firstStats, "ApplyUpgrade", flat);
        Call(firstStats, "ApplyUpgrade", multiplier);
        Call(secondStats, "ApplyUpgrade", multiplier);
        Call(secondStats, "ApplyUpgrade", flat);
        Assert.That(Get<float>(firstStats, "currentMaxHP"), Is.EqualTo(240f));
        Assert.That(Get<float>(secondStats, "currentMaxHP"), Is.EqualTo(240f));
        Assert.That(Get<float>(firstStats, "currentDamage"), Is.EqualTo(Get<float>(secondStats, "currentDamage")));
    }

    [Test]
    public void DeathIsRaisedOncePerLifeAndResetAllowsAnotherDeath() {
        var health = Health();
        int deaths = 0;
        Action handler = () => deaths++;
        health.GetType().GetEvent("OnDied").AddEventHandler(health, handler);
        Call(health, "TakeDamage", 10000f, Vector2.zero, Vector2.zero);
        Call(health, "TakeDamage", 10000f, Vector2.zero, Vector2.zero);
        Assert.That(deaths, Is.EqualTo(1));
        Call(health, "ResetHealth");
        Call(health, "TakeDamage", 10000f, Vector2.zero, Vector2.zero);
        Assert.That(deaths, Is.EqualTo(2));
    }
}
