using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the shop, audio recipes and event isolation added with the feel pass. Reflection is
/// used for the same reason as the other test files: runtime scripts live in the predefined assembly.
/// </summary>
public class FeelTests
{
    private readonly System.Collections.Generic.List<UnityEngine.Object> created = new System.Collections.Generic.List<UnityEngine.Object>();
    private string tempSave;

    private static Type T(string name) { return AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null); }
    private X Track<X>(X obj) where X : UnityEngine.Object { created.Add(obj); return obj; }

    private static object Call(object target, string name, params object[] args)
    {
        return target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);
    }

    private static object CallStatic(string type, string name, params object[] args)
    {
        return T(type).GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
    }

    private static object Get(object target, string name)
    {
        var type = target.GetType();
        var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop != null) return prop.GetValue(target);
        return type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }

    private static void Set(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    [SetUp]
    public void SetUp()
    {
        tempSave = Path.Combine(Path.GetTempPath(), "exterminator_feel_" + Guid.NewGuid().ToString("N") + ".json");
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

    // ---------- event isolation ----------

    [Test]
    public void OneThrowingListenerDoesNotStopTheOthers()
    {
        var events = T("GameEvents");
        int reached = 0;
        Action<int> thrower = _ => throw new InvalidOperationException("boom");
        Action<int> survivor = _ => reached++;
        var field = events.GetField("OnLevelUp", BindingFlags.Public | BindingFlags.Static);
        field.SetValue(null, (Action<int>)Delegate.Combine(thrower, survivor));

        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("threw"));
        events.GetMethod("LevelUp").Invoke(null, new object[] { 3 });
        Assert.That(reached, Is.EqualTo(1), "The listener after the throwing one must still run.");
    }

    // ---------- settings persistence ----------

    [Test]
    public void VolumeAndShakeSettingsSurviveASaveRoundTrip()
    {
        var data = CallStatic("SaveSystem", "get_Data");
        Set(data, "masterVolume", 0.33f);
        Set(data, "screenShake", false);
        CallStatic("SaveSystem", "Save");
        CallStatic("SaveSystem", "UsePath", tempSave);
        var loaded = CallStatic("SaveSystem", "get_Data");
        Assert.That((float)Get(loaded, "masterVolume"), Is.EqualTo(0.33f).Within(0.001f));
        Assert.That((bool)Get(loaded, "screenShake"), Is.False);
    }

    [Test]
    public void DefaultSettingsAreAudibleAndShakeIsOn()
    {
        var data = CallStatic("SaveSystem", "get_Data");
        Assert.That((float)Get(data, "masterVolume"), Is.GreaterThan(0f));
        Assert.That((float)Get(data, "musicVolume"), Is.GreaterThan(0f));
        Assert.That((bool)Get(data, "screenShake"), Is.True);
    }

    // ---------- shop ----------

    private object NewShop(out object meta)
    {
        var go = Track(new GameObject("Shop"));
        meta = go.AddComponent(T("MetaProgression"));
        return go.AddComponent(T("ShopRoom"));
    }

    [Test]
    public void ShopCannotBeBoughtFromWhileClosed()
    {
        var shop = NewShop(out _);
        Assert.That(Call(shop, "Buy", 0), Is.False);
        Assert.That(Call(shop, "Reroll"), Is.False);
    }

    [Test]
    public void SpendRunFailsWithoutChangingScrapWhenShort()
    {
        NewShop(out var meta);
        Call(meta, "Earn", 10);
        Assert.That(Call(meta, "SpendRun", 25), Is.False);
        Assert.That(Get(meta, "RunScrap"), Is.EqualTo(10));
    }

    [Test]
    public void SpendRunDeductsExactlyWhenAffordable()
    {
        NewShop(out var meta);
        Call(meta, "Earn", 40);
        Assert.That(Call(meta, "SpendRun", 25), Is.True);
        Assert.That(Get(meta, "RunScrap"), Is.EqualTo(15));
    }

    [Test]
    public void ShopPricesRiseWithRarity()
    {
        var shop = NewShop(out _);
        Set(shop, "roomNumber", 5);
        var common = Track(ScriptableObject.CreateInstance(T("UpgradeData")));
        var epic = Track(ScriptableObject.CreateInstance(T("UpgradeData")));
        Set(epic, "rarity", Enum.Parse(T("UpgradeRarity"), "Epic"));
        int c = (int)Call(shop, "PriceOf", common);
        int e = (int)Call(shop, "PriceOf", epic);
        Assert.That(e, Is.GreaterThan(c));
        Assert.That(c, Is.GreaterThan(0));
    }

    [Test]
    public void ShopPricesRiseWithRoomNumber()
    {
        var shop = NewShop(out _);
        var u = Track(ScriptableObject.CreateInstance(T("UpgradeData")));
        Set(shop, "roomNumber", 2);
        int early = (int)Call(shop, "PriceOf", u);
        Set(shop, "roomNumber", 20);
        int late = (int)Call(shop, "PriceOf", u);
        Assert.That(late, Is.GreaterThan(early));
    }

    [Test]
    public void RestockGetsMoreExpensiveEachTime()
    {
        var shop = NewShop(out _);
        int first = (int)Get(shop, "RerollPrice");
        Set(shop, "rerolls", 2);
        Assert.That((int)Get(shop, "RerollPrice"), Is.GreaterThan(first));
    }

    // ---------- audio manager ----------

    [Test]
    public void AudioManagerIgnoresUnknownSoundsInsteadOfThrowing()
    {
        var go = Track(new GameObject("Audio"));
        var audio = go.AddComponent(T("AudioManager"));
        var play = T("AudioManager").GetMethod("Play", new[] { typeof(string) });
        Assert.That(play, Is.Not.Null, "AudioManager.Play(string) overload should exist.");
        Assert.DoesNotThrow(() => play.Invoke(audio, new object[] { "this_sound_does_not_exist" }));
    }

    [Test]
    public void VolumeSettersClampToZeroOne()
    {
        var go = Track(new GameObject("Audio"));
        var audio = go.AddComponent(T("AudioManager"));
        T("AudioManager").GetProperty("MasterVolume").SetValue(audio, 5f);
        Assert.That((float)Get(audio, "MasterVolume"), Is.EqualTo(1f));
        T("AudioManager").GetProperty("MasterVolume").SetValue(audio, -2f);
        Assert.That((float)Get(audio, "MasterVolume"), Is.EqualTo(0f));
    }

    [Test]
    public void EveryClipTheAudioManagerReferencesExistsOnDisk()
    {
        var names = new[] { "hit_0", "hit_1", "hit_2", "die_0", "die_1", "die_2", "step_0", "step_1", "scrap", "zap", "spit", "spit_hit",
            "spawn", "mop_hit", "mop_swing", "player_hurt", "player_death", "level_up", "upgrade_pick", "boss_roar", "boss_warning",
            "explosion", "dash", "heal", "room_clear", "room_start", "flame_loop", "foam_loop", "beam_charge", "beam_fire", "beam_tap",
            "overload", "ui_hover", "ui_click", "ui_buy", "ui_deny", "music_lobby", "music_combat", "music_boss", "music_victory", "music_gameover" };
        var loaded = Resources.LoadAll<AudioClip>("Audio").Select(c => c.name).ToList();
        var missing = names.Where(n => !loaded.Contains(n)).ToList();
        Assert.That(missing, Is.Empty, "Missing clips: " + string.Join(", ", missing));
    }

    [Test]
    public void MusicLoopsAreLongEnoughToNotGrate()
    {
        foreach (var name in new[] { "music_lobby", "music_combat", "music_boss" })
        {
            var clip = Resources.LoadAll<AudioClip>("Audio").First(c => c.name == name);
            Assert.That(clip.length, Is.GreaterThan(15f), name + " should be a real loop, not a sting.");
        }
    }

    // ---------- animation ----------

    [Test]
    public void DirectionMappingFlipsLeftAndNeverFlipsUpOrDown()
    {
        var m = T("PlayerAnimation").GetMethod("VectorToDirection5", BindingFlags.Public | BindingFlags.Static);
        var left = m.Invoke(null, new object[] { Vector2.left });
        var up = m.Invoke(null, new object[] { Vector2.up });
        var down = m.Invoke(null, new object[] { Vector2.down });
        Assert.That(left.GetType().GetField("Item2").GetValue(left), Is.EqualTo(true));
        Assert.That(up.GetType().GetField("Item2").GetValue(up), Is.EqualTo(false));
        Assert.That(down.GetType().GetField("Item2").GetValue(down), Is.EqualTo(false));
    }

    [Test]
    public void PlayerAnimatorControllerHasEveryParameterTheScriptSets()
    {
#if UNITY_EDITOR
        var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/_Project/Art/Player/PlayerAnimator.controller");
        Assert.That(controller, Is.Not.Null, "Run Exterminator > Build Animations.");
        var names = controller.parameters.Select(p => p.name).ToList();
        foreach (var required in new[] { "IsMoving", "MoveX", "MoveY", "AimX", "AimY", "Hurt", "Dash", "Death" })
            Assert.That(names, Does.Contain(required));
#endif
    }
}
