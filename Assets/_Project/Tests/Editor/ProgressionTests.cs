using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for rooms, persistence, meta progression, paths and upgrade effects. Like CoreGameplayTests
/// these use reflection because runtime scripts live in Unity's predefined assembly.
/// </summary>
public class ProgressionTests
{
    private readonly System.Collections.Generic.List<UnityEngine.Object> created = new System.Collections.Generic.List<UnityEngine.Object>();
    private string tempSave;

    private static Type T(string name)
    {
        return AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    }

    private X Track<X>(X obj) where X : UnityEngine.Object { created.Add(obj); return obj; }

    private static object Call(object target, string name, params object[] args)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        return target.GetType().GetMethod(name, flags).Invoke(target, args);
    }

    private static object CallStatic(string type, string name, params object[] args)
    {
        var m = T(type).GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        return m.Invoke(null, args);
    }

    private static void Set(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    private static object Get(object target, string name)
    {
        var type = target.GetType();
        var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null) return prop.GetValue(target);
        return type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }

    [SetUp]
    public void SetUp()
    {
        tempSave = Path.Combine(Path.GetTempPath(), "exterminator_test_" + Guid.NewGuid().ToString("N") + ".json");
        CallStatic("SaveSystem", "UsePath", tempSave);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var o in created) if (o != null) UnityEngine.Object.DestroyImmediate(o);
        created.Clear();
        if (File.Exists(tempSave)) File.Delete(tempSave);
        CallStatic("SaveSystem", "UsePath", new object[] { null });
        T("GameEvents").GetMethod("ClearAllListeners").Invoke(null, null);
        Time.timeScale = 1f;
    }

    // ---------- Room modifiers ----------

    private object NewSpec(int count)
    {
        var spec = Activator.CreateInstance(T("WaveSpec"));
        Set(spec, "enemyCount", count);
        return spec;
    }

    private void ApplyModifier(string modifier, object spec)
    {
        var enumType = T("RoomModifier");
        CallStatic("RoomModifiers", "Apply", Enum.Parse(enumType, modifier), spec);
    }

    [Test]
    public void SwarmAddsEnemiesButWeakensThem()
    {
        var spec = NewSpec(10);
        ApplyModifier("Swarm", spec);
        Assert.That((int)Get(spec, "enemyCount"), Is.GreaterThan(10));
        Assert.That((float)Get(spec, "healthMultiplier"), Is.LessThan(1f));
    }

    [Test]
    public void LeanRoomsNeverReachZeroEnemies()
    {
        var spec = NewSpec(1);
        ApplyModifier("Lean", spec);
        Assert.That((int)Get(spec, "enemyCount"), Is.GreaterThanOrEqualTo(1));
        Assert.That((float)Get(spec, "xpMultiplier"), Is.GreaterThan(1f));
    }

    [Test]
    public void NoModifierLeavesSpecUntouched()
    {
        var spec = NewSpec(8);
        ApplyModifier("None", spec);
        Assert.That((int)Get(spec, "enemyCount"), Is.EqualTo(8));
        Assert.That((float)Get(spec, "healthMultiplier"), Is.EqualTo(1f));
    }

    // ---------- Save system ----------

    [Test]
    public void SaveRoundTripsProgress()
    {
        var data = CallStatic("SaveSystem", "get_Data");
        Set(data, "scrap", 123);
        Set(data, "bestRoom", 9);
        CallStatic("SaveSystem", "SetMetaLevel", "hp", 3);
        CallStatic("SaveSystem", "Save");

        CallStatic("SaveSystem", "UsePath", tempSave); // drops the cache so the next read hits the disk
        var loaded = CallStatic("SaveSystem", "get_Data");
        Assert.That((int)Get(loaded, "scrap"), Is.EqualTo(123));
        Assert.That((int)Get(loaded, "bestRoom"), Is.EqualTo(9));
        Assert.That(CallStatic("SaveSystem", "GetMetaLevel", "hp"), Is.EqualTo(3));
    }

    [Test]
    public void CorruptSaveStartsFreshInsteadOfThrowing()
    {
        File.WriteAllText(tempSave, "{ this is not json");
        CallStatic("SaveSystem", "UsePath", tempSave);
        object loaded = null;
        Assert.DoesNotThrow(() => loaded = CallStatic("SaveSystem", "get_Data"));
        Assert.That((int)Get(loaded, "scrap"), Is.EqualTo(0));
    }

    [Test]
    public void MissingSaveStartsWithFirstCharacterUnlocked()
    {
        var data = CallStatic("SaveSystem", "get_Data");
        var unlocked = (System.Collections.IList)Get(data, "unlockedCharacters");
        Assert.That(unlocked, Does.Contain("firebug"));
    }

    // ---------- Meta progression ----------

    private object NewMeta()
    {
        var go = Track(new GameObject("Meta"));
        var meta = go.AddComponent(T("MetaProgression"));
        return meta;
    }

    private object NewMetaUpgrade(string id, string stat, float perLevel, int maxLevel, int baseCost)
    {
        var u = Track(ScriptableObject.CreateInstance(T("MetaUpgradeData")));
        Set(u, "upgradeId", id);
        Set(u, "stat", Enum.Parse(T("MetaStat"), stat));
        Set(u, "perLevel", perLevel);
        Set(u, "maxLevel", maxLevel);
        Set(u, "baseCost", baseCost);
        Set(u, "costGrowth", 2f);
        return u;
    }

    [Test]
    public void BuyingSpendsScrapAndRaisesLevel()
    {
        var meta = NewMeta();
        var upgrade = NewMetaUpgrade("hp", "MaxHP", 0.1f, 3, 50);
        Set(CallStatic("SaveSystem", "get_Data"), "scrap", 120);

        Assert.That(Call(meta, "Buy", upgrade), Is.True);
        Assert.That(Get(CallStatic("SaveSystem", "get_Data"), "scrap"), Is.EqualTo(70));
        Assert.That(Call(meta, "LevelOf", upgrade), Is.EqualTo(1));
    }

    [Test]
    public void CannotBuyWithoutEnoughScrap()
    {
        var meta = NewMeta();
        var upgrade = NewMetaUpgrade("hp", "MaxHP", 0.1f, 3, 50);
        Set(CallStatic("SaveSystem", "get_Data"), "scrap", 10);
        Assert.That(Call(meta, "Buy", upgrade), Is.False);
        Assert.That(Call(meta, "LevelOf", upgrade), Is.EqualTo(0));
    }

    [Test]
    public void CannotBuyPastMaxLevel()
    {
        var meta = NewMeta();
        var upgrade = NewMetaUpgrade("hp", "MaxHP", 0.1f, 1, 1);
        Set(CallStatic("SaveSystem", "get_Data"), "scrap", 9999);
        Assert.That(Call(meta, "Buy", upgrade), Is.True);
        Assert.That(Call(meta, "Buy", upgrade), Is.False);
    }

    [Test]
    public void CostGrowsWithEachLevel()
    {
        var upgrade = NewMetaUpgrade("hp", "MaxHP", 0.1f, 5, 50);
        int first = (int)Call(upgrade, "CostForLevel", 0);
        int second = (int)Call(upgrade, "CostForLevel", 1);
        Assert.That(second, Is.GreaterThan(first));
    }

    // ---------- Upgrade effects and paths ----------

    private object NewStats()
    {
        return Activator.CreateInstance(T("PlayerStats"));
    }

    private object EffectUpgrade(string id)
    {
        var u = Track(ScriptableObject.CreateInstance(T("UpgradeData")));
        Set(u, "hasSpecialEffect", true);
        Set(u, "specialEffectID", id);
        return u;
    }

    [Test]
    public void StatsCountSpecialEffectStacks()
    {
        var stats = NewStats();
        var upgrade = EffectUpgrade("lifesteal");
        Call(stats, "ApplyUpgrade", upgrade);
        Call(stats, "ApplyUpgrade", upgrade);
        Assert.That(Call(stats, "CountEffect", "lifesteal"), Is.EqualTo(2));
        Assert.That(Call(stats, "CountEffect", "second_wind"), Is.EqualTo(0));
    }

    [Test]
    public void SpecialEffectWithoutFlagIsNotCounted()
    {
        var stats = NewStats();
        var upgrade = Track(ScriptableObject.CreateInstance(T("UpgradeData")));
        Set(upgrade, "specialEffectID", "lifesteal");
        Call(stats, "ApplyUpgrade", upgrade);
        Assert.That(Call(stats, "CountEffect", "lifesteal"), Is.EqualTo(0));
    }

    [Test]
    public void PathIsAnUpgradeSoItCanBeTakenByTheSameRules()
    {
        var path = Track(ScriptableObject.CreateInstance(T("PathData")));
        Assert.That(typeof(ScriptableObject).IsAssignableFrom(path.GetType()));
        Assert.That(T("UpgradeData").IsAssignableFrom(path.GetType()));
        Assert.That(Call(path, "CanBeTaken", NewStats()), Is.True);
    }

    [Test]
    public void TierTwoPathNeedsItsTierOneParent()
    {
        var stats = NewStats();
        var parent = Track(ScriptableObject.CreateInstance(T("PathData")));
        var child = Track(ScriptableObject.CreateInstance(T("PathData")));
        Array prerequisites = Array.CreateInstance(T("UpgradeData"), 1);
        prerequisites.SetValue(parent, 0);
        Set(child, "requiredUpgrades", prerequisites);
        Assert.That(Call(child, "CanBeTaken", stats), Is.False);
        Call(stats, "ApplyUpgrade", parent);
        Assert.That(Call(child, "CanBeTaken", stats), Is.True);
    }

    [Test]
    public void FoamArmorReducesDamageAndClearsToZero()
    {
        var stats = NewStats();
        float before = (float)Call(stats, "CalculateDamageReduction");
        Call(stats, "SetFoamArmor", 100f);
        float withFoam = (float)Call(stats, "CalculateDamageReduction");
        Assert.That(withFoam, Is.GreaterThan(before));
        Call(stats, "SetFoamArmor", 0f);
        Assert.That((float)Call(stats, "CalculateDamageReduction"), Is.EqualTo(before).Within(0.0001f));
    }

    [Test]
    public void MetaBonusesRaiseStatsAndSurviveRecalculation()
    {
        var stats = NewStats();
        float baseHp = (float)Get(stats, "currentMaxHP");
        Call(stats, "SetMetaBonuses", 0.2f, 0f, 0f);
        Assert.That((float)Get(stats, "currentMaxHP"), Is.EqualTo(baseHp * 1.2f).Within(0.01f));
        var upgrade = Track(ScriptableObject.CreateInstance(T("UpgradeData")));
        Call(stats, "ApplyUpgrade", upgrade);
        Assert.That((float)Get(stats, "currentMaxHP"), Is.EqualTo(baseHp * 1.2f).Within(0.01f));
    }

    // ---------- Second wind via OnWouldDie ----------

    [Test]
    public void ReviveInWouldDieCancelsTheDeath()
    {
        var go = Track(new GameObject("Test health"));
        var health = go.AddComponent(T("Health"));
        Call(health, "ResetHealth");
        int deaths = 0;
        Action revive = () => Call(health, "Revive", 0.5f);
        Action counted = () => deaths++;
        health.GetType().GetEvent("OnWouldDie").AddEventHandler(health, revive);
        health.GetType().GetEvent("OnDied").AddEventHandler(health, counted);

        Call(health, "TakeDamage", 100000f, Vector2.zero, Vector2.zero);

        Assert.That(deaths, Is.EqualTo(0));
        Assert.That((bool)Get(health, "IsAlive"), Is.True);
        Assert.That((float)Get(health, "CurrentHP"), Is.GreaterThan(0f));
    }

    [Test]
    public void InvulnerabilityBlocksDamageUntilItExpires()
    {
        var go = Track(new GameObject("Test health"));
        var health = go.AddComponent(T("Health"));
        Call(health, "ResetHealth");
        Call(health, "GrantInvulnerability", 5f);
        float before = (float)Get(health, "CurrentHP");
        Call(health, "TakeDamage", 50f, Vector2.zero, Vector2.zero);
        Assert.That((float)Get(health, "CurrentHP"), Is.EqualTo(before));
    }

    [Test]
    public void DamageReductionScalesIncomingDamage()
    {
        var go = Track(new GameObject("Test health"));
        var health = go.AddComponent(T("Health"));
        Call(health, "ResetHealth");
        Call(health, "SetDamageReduction", 0.5f);
        float max = (float)Get(health, "MaxHP");
        Call(health, "TakeDamage", 20f, Vector2.zero, Vector2.zero);
        Assert.That((float)Get(health, "CurrentHP"), Is.EqualTo(max - 10f).Within(0.001f));
    }
}
