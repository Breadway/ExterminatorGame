using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the mechanics pass: character-specific paths, endless boons, new upgrade behaviours,
/// branching map choice, room events and the lore system. Same reflection pattern as the other
/// test files, since runtime scripts live in the predefined assembly.
/// </summary>
public class MechanicsTests
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
        return target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    }

    private static object CallStatic(string type, string name, params object[] args)
    {
        return T(type).GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
    }

    private static void Set(object target, string name, object value)
    {
        var type = target.GetType();
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) { field.SetValue(target, value); return; }
        type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    private static object Get(object target, string name)
    {
        var type = target.GetType();
        var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null) return prop.GetValue(target);
        return type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }

    private static void ClearInstance(Type type)
    {
        // Awake does not run on AddComponent in this context and OnDestroy is not
        // reliable here, so singletons are assigned and cleared explicitly per test
        var prop = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        prop.SetValue(null, null);
    }

    [SetUp]
    public void SetUp()
    {
        tempSave = Path.Combine(Path.GetTempPath(), "exterminator_mech_" + Guid.NewGuid().ToString("N") + ".json");
        CallStatic("SaveSystem", "UsePath", tempSave);
        ClearInstance(T("PlayerController"));
        ClearInstance(T("RoomManager"));
        ClearInstance(T("UpgradeManager"));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var o in created) if (o != null) UnityEngine.Object.DestroyImmediate(o);
        created.Clear();
        ClearInstance(T("PlayerController"));
        ClearInstance(T("RoomManager"));
        ClearInstance(T("UpgradeManager"));
        if (File.Exists(tempSave)) File.Delete(tempSave);
        CallStatic("SaveSystem", "UsePath", new object[] { null });
        T("GameEvents").GetMethod("ClearAllListeners").Invoke(null, null);
        Time.timeScale = 1f;
    }

    // ---------- helpers ----------

    private object NewPlayer(string characterId)
    {
        var go = Track(new GameObject("Test player"));
        go.AddComponent<Rigidbody2D>();
        var testHealth = go.AddComponent(T("Health"));
        Call(testHealth, "ResetHealth");
        var player = go.AddComponent(T("PlayerController"));
        Set(player, "health", testHealth);
        Call(player, "SetCharacterId", characterId);
        // EditMode AddComponent does not run Awake here, so wire what Awake would have.
        // Always overwrite: a stale Instance from another test reads as non-null and would win.
        if (Get(player, "stats") == null) Set(player, "stats", NewStats());
        T("PlayerController").GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).SetValue(null, player);
        return player;
    }

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

    private object NewPath(int tier, string characterId)
    {
        var p = Track(ScriptableObject.CreateInstance(T("PathData")));
        Set(p, "tier", tier);
        Set(p, "characterId", characterId);
        return p;
    }

    private Component NewEnemy(float maxHP, bool isBoss)
    {
        var go = Track(new GameObject("Test enemy"));
        go.AddComponent<Rigidbody2D>();
        go.AddComponent(T("EnemyAttack"));
        var health = go.AddComponent(T("Health"));
        Call(health, "SetMaxHP", maxHP);
        Call(health, "ResetHealth");
        var data = ScriptableObject.CreateInstance(T("EnemyData"));
        Set(data, "isBoss", isBoss);
        Set(data, "maxHealth", maxHP);
        var enemy = go.AddComponent(T("Enemy"));
        Set(enemy, "health", health);
        T("Enemy").GetProperty("EnemyData").SetValue(enemy, data);
        return enemy;
    }

    private static float EnemyHp(Component enemy)
    {
        var health = enemy.GetComponent(T("Health"));
        return (float)T("Health").GetProperty("CurrentHP").GetValue(health);
    }

    private object NewEffects()
    {
        var go = Track(new GameObject("Test effects"));
        return go.AddComponent(T("UpgradeEffects"));
    }

    // ---------- character-specific paths ----------

    [Test]
    public void PathsFilterByCharacter()
    {
        var player = NewPlayer("custodian");
        var stats = Get(player, "stats");
        var manager = Track(new GameObject("Test upgrades")).AddComponent(T("UpgradeManager"));
        var mine = NewPath(1, "custodian");
        var theirs = NewPath(1, "firebug");
        var open = NewPath(1, "");
        var arr = Array.CreateInstance(T("PathData"), 3);
        arr.SetValue(mine, 0);
        arr.SetValue(theirs, 1);
        arr.SetValue(open, 2);
        Set(manager, "allPaths", arr);

        var choices = (Array)Call(manager, "PathChoicesForLevel", 10, stats);
        Assert.That(choices.Length, Is.EqualTo(2));
        Assert.That(choices.Cast<object>().Contains(mine));
        Assert.That(choices.Cast<object>().Contains(open));
    }

    [Test]
    public void TierTwoPathNeedsItsTierOneParent()
    {
        var player = NewPlayer("firebug");
        var stats = Get(player, "stats");
        var parent = NewPath(1, "firebug");
        var child = NewPath(2, "firebug");
        var prerequisites = Array.CreateInstance(T("UpgradeData"), 1);
        prerequisites.SetValue(parent, 0);
        Set(child, "requiredUpgrades", prerequisites);

        Assert.That(Call(child, "CanBeTaken", stats), Is.False);
        Call(stats, "ApplyUpgrade", parent);
        Assert.That(Call(child, "CanBeTaken", stats), Is.True);
    }

    [Test]
    public void EndlessBoonGrantsBuffPastLevelThirty()
    {
        var player = NewPlayer("scientist");
        var stats = Get(player, "stats");
        var manager = Track(new GameObject("Test upgrades")).AddComponent(T("UpgradeManager"));
        Call(manager, "ResumeAfterVictory");
        float before = (float)Get(stats, "DamageBuff");
        Call(manager, "QueueUpgrade", 31);
        Assert.That((float)Get(stats, "DamageBuff"), Is.GreaterThan(before));
        Assert.That((bool)Get(manager, "IsChoosing"), Is.False);
    }

    // ---------- new upgrade behaviours ----------

    [Test]
    public void ExecutionerExecutesNearDeadNonBoss()
    {
        var player = NewPlayer("firebug");
        Call(Get(player, "stats"), "ApplyUpgrade", EffectUpgrade("executioner"));
        var fx = NewEffects();
        var enemy = NewEnemy(100f, false);
        var health = enemy.GetComponent(T("Health"));
        Set(health, "currentHP", 8f);
        Call(fx, "HandleEnemyHit", enemy, 5f);
        Assert.That((bool)Get(health, "IsAlive"), Is.False);
    }

    [Test]
    public void ExecutionerBonusBelowThresholdButNoExecuteOnBoss()
    {
        var player = NewPlayer("firebug");
        Call(Get(player, "stats"), "ApplyUpgrade", EffectUpgrade("executioner"));
        var fx = NewEffects();
        var boss = NewEnemy(100f, true);
        var health = boss.GetComponent(T("Health"));
        Call(health, "TakeDamage", 95f, Vector2.zero, Vector2.zero);
        Call(fx, "HandleEnemyHit", boss, 5f);
        Assert.That((bool)Get(health, "IsAlive"), Is.True, "Bosses are never executed.");
    }

    [Test]
    public void ExecutionerBonusDamageOnWoundedTarget()
    {
        var player = NewPlayer("firebug");
        Call(Get(player, "stats"), "ApplyUpgrade", EffectUpgrade("executioner"));
        var fx = NewEffects();
        var enemy = NewEnemy(100f, false);
        var health = enemy.GetComponent(T("Health"));
        Set(health, "currentHP", 25f);
        Call(fx, "HandleEnemyHit", enemy, 10f);
        Assert.That(EnemyHp(enemy), Is.EqualTo(20f), "25 - 5 executioner bonus (the 10 base damage was already dealt by the weapon).");
    }

    [Test]
    public void BerserkerDamageGrowsAsHealthFalls()
    {
        var player = NewPlayer("custodian");
        var stats = Get(player, "stats");
        Call(stats, "ApplyUpgrade", EffectUpgrade("berserker_rage"));
        var fx = NewEffects();
        Call(fx, "HandleHealthChanged", 100f, 100f);
        Assert.That((float)Get(stats, "DamageBuff"), Is.EqualTo(1f));
        Call(fx, "HandleHealthChanged", 30f, 100f);
        Assert.That((float)Get(stats, "DamageBuff"), Is.EqualTo(1f + 0.7f * 0.6f).Within(0.001f));
    }

    [Test]
    public void MomentumSpeedsUpOnKills()
    {
        var player = NewPlayer("intern");
        var stats = Get(player, "stats");
        Call(stats, "ApplyUpgrade", EffectUpgrade("momentum"));
        var fx = NewEffects();
        var enemy = NewEnemy(100f, false);
        Call(fx, "HandleEnemyKilled", enemy);
        Assert.That((float)Get(stats, "SpeedBuff"), Is.EqualTo(1.08f).Within(0.001f));
    }

    [Test]
    public void EmergencyProtocolsHealsOncePerRoom()
    {
        var player = NewPlayer("firebug");
        var stats = Get(player, "stats");
        Call(stats, "ApplyUpgrade", EffectUpgrade("emergency_protocols"));
        var fx = NewEffects();
        var health = ((Component)player).GetComponent(T("Health"));
        Call(health, "TakeDamage", 80f, Vector2.zero, Vector2.zero);
        Call(fx, "HandleHealthChanged", 20f, 100f);
        Assert.That((float)Get(health, "CurrentHP"), Is.EqualTo(45f));
        // Same room, no second trigger
        Call(health, "TakeDamage", 10f, Vector2.zero, Vector2.zero);
        Call(fx, "HandleHealthChanged", 35f, 100f);
        Assert.That((float)Get(health, "CurrentHP"), Is.EqualTo(35f));
    }

    // ---------- branching map ----------

    private object NewRooms()
    {
        var go = Track(new GameObject("Test rooms"));
        var rooms = go.AddComponent(T("RoomManager"));
        var run = ScriptableObject.CreateInstance(T("RunData"));
        Set(run, "RoomCount", 25);
        Set(run, "BaseEnemies", 5);
        Set(run, "EnemiesPerRoom", 0.9f);
        Set(run, "MaxEnemies", 60);
        Set(run, "EliteChance", 0f);
        Set(run, "UnknownChance", 0f);
        Set(run, "ModifierChance", 0f);
        Set(run, "ShopChance", 0f);
        Set(rooms, "runData", run);
        T("RoomManager").GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).SetValue(null, rooms);
        return rooms;
    }

    [Test]
    public void MapOffersThreeDistinctChoices()
    {
        var rooms = NewRooms();
        Call(rooms, "OfferMap");
        Assert.That((bool)Get(rooms, "IsChoosingMap"), Is.True);
        var options = (System.Collections.IList)Get(rooms, "MapOptions");
        Assert.That(options.Count, Is.EqualTo(3));
        Time.timeScale = 1f;
    }

    [Test]
    public void MapChoiceContinuesTheRun()
    {
        var rooms = NewRooms();
        Call(rooms, "OfferMap");
        Assert.That(Call(rooms, "ChooseMap", 1), Is.True);
        Assert.That((bool)Get(rooms, "IsChoosingMap"), Is.False);
        Assert.That(Call(rooms, "ChooseMap", 7), Is.False, "Out-of-range choices are rejected.");
        Time.timeScale = 1f;
    }

    // ---------- room events ----------

    [Test]
    public void RoomEventLabelsCoverEveryId()
    {
        foreach (var id in new[] { "acid_rain", "web_nest", "shock_plates", "fuel_leak", "ambush", "supply_cache", "document_cache", "hidden_cache" })
            Assert.That((string)CallStatic("RoomEventDirector", "Label", id), Is.Not.Empty, id);
        Assert.That((string)CallStatic("RoomEventDirector", "Label", "nonsense"), Is.Empty);
    }

    [Test]
    public void RoomEventRollOnlyProducesKnownIds()
    {
        var known = new System.Collections.Generic.HashSet<string>
            { "acid_rain", "web_nest", "shock_plates", "fuel_leak", "ambush", "supply_cache", "document_cache", "hidden_cache" };
        for (int i = 0; i < 40; i++)
            Assert.That(known.Contains((string)CallStatic("RoomEventDirector", "RollEventId", 10)), Is.True);
    }

    [Test]
    public void EarlyRoomsOnlyGetFriendlyEvents()
    {
        for (int i = 0; i < 20; i++)
        {
            string id = (string)CallStatic("RoomEventDirector", "RollEventId", 1);
            Assert.That(id == "supply_cache" || id == "document_cache", Is.True);
        }
    }

    // ---------- lore ----------

    [Test]
    public void DocumentsDrawUnreadFirstThenRunOut()
    {
        var lore = Track(new GameObject("Test lore")).AddComponent(T("LoreSystem"));
        string first = (string)Call(lore, "DrawDocument", false);
        Assert.That(first, Is.Not.Empty);
        Call(lore, "UnlockDocument", first);
        string second = (string)Call(lore, "DrawDocument", false);
        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void BestiaryStartsLocked()
    {
        var lore = Track(new GameObject("Test lore")).AddComponent(T("LoreSystem"));
        var status = (System.Collections.IList)Call(lore, "Status");
        Assert.That(status.Count, Is.GreaterThan(10));
    }

    [Test]
    public void LoreNamesAndGlitchesAreNonEmpty()
    {
        Assert.That(((string)CallStatic("LoreSystem", "SectorName", 7, false)).Contains("7"));
        Assert.That((string)CallStatic("LoreSystem", "HiddenRoomName"), Is.Not.Empty);
        Assert.That((string)CallStatic("LoreSystem", "GlitchTitle", "SECTOR 7"), Is.Not.Empty);
        var lore = Track(new GameObject("Test lore")).AddComponent(T("LoreSystem"));
        Assert.That((string)Call(lore, "NextTip"), Is.Not.Empty);
    }
}
