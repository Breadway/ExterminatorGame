#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Connects the shipped content to the game scene: managers, run data, weapons, UI documents.
/// Idempotent. Run from Exterminator > Wire Game Scene after Build Content.
/// </summary>
public static class SceneWiring
{
    private const string Data = "Assets/_Project/Data";

    [MenuItem("Exterminator/Wire Game Scene")]
    public static void Wire()
    {
        var mgr = GameObject.Find("Managers");
        var player = GameObject.Find("Player");
        if (mgr == null || player == null)
        {
            Debug.LogError("Wire Game Scene: open GameScene first (needs Managers and Player objects).");
            return;
        }

        WireManagers(mgr);
        WireRoomBuilder(mgr);
        WireUpgradePaths(mgr);
        WireSpawner();
        WirePlayer(player);
        WireUi();
        RemovePrototypeObjects();
        // Anything outside the generated arena is a void; make it read as one, not as sky.
        var cam = Camera.main;
        if (cam != null) cam.backgroundColor = new Color(0.04f, 0.045f, 0.055f, 1f);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Game scene wired.");
    }

    private static T Ensure<T>(GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }

    private static Sprite EnvSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Project/Art/Environment/{name}.png");
    }

    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null) { Debug.LogWarning($"Missing field {field} on {target.GetType().Name}"); return; }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireManagers(GameObject mgr)
    {
        Ensure<MetaProgression>(mgr);
        Ensure<UpgradeEffects>(mgr);
        SetRef(mgr.GetComponent<UpgradeEffects>(), "poisonEffect",
            AssetDatabase.LoadAssetAtPath<StatusEffectData>($"{Data}/StatusEffects/Poison.asset"));
        Ensure<ScreenShake>(mgr);
        Ensure<HitStop>(mgr);
        Ensure<Fx>(mgr);
        Ensure<ShopRoom>(mgr);
        Ensure<HazardField>(mgr);
        Ensure<LoreSystem>(mgr);
        var director = Ensure<RoomEventDirector>(mgr);
        SetRef(director, "healPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Pickups/HealPickup.prefab"));
        SetRef(director, "scrapPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Pickups/ScrapPickup.prefab"));
        SetRef(director, "documentPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Pickups/DocumentPickup.prefab"));
        // Balance curve (tools/balance_model.py): XP 60 x1.22, 12% level heal
        var xp = Ensure<XPManager>(mgr);
        var xso = new SerializedObject(xp);
        xso.FindProperty("xpToNextLevel").intValue = 60;
        xso.FindProperty("xpMultiplier").floatValue = 1.22f;
        xso.FindProperty("healPercentOnLevelUp").floatValue = 0.12f;
        xso.ApplyModifiedPropertiesWithoutUndo();
        var pickups = Ensure<PickupSpawner>(mgr);
        SetRef(pickups, "healPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Pickups/HealPickup.prefab"));
        SetRef(mgr.GetComponent<RoomManager>(), "runData", AssetDatabase.LoadAssetAtPath<RunData>($"{Data}/Run_Standard.asset"));
    }

    private static void WireRoomBuilder(GameObject mgr)
    {
        var builder = Ensure<RoomLayoutBuilder>(mgr);
        SetRef(builder, "floorSprite", EnvSprite("Floor"));
        SetRef(builder, "wallSprite", EnvSprite("Wall"));
        SetRef(builder, "crateSprite", EnvSprite("Crate"));
        SetRef(builder, "pillarSprite", EnvSprite("Pillar"));
        SetRef(builder, "legacyGrid", GameObject.Find("Grid"));
        var so = new SerializedObject(builder);
        so.FindProperty("wallThickness").floatValue = 3f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireUpgradePaths(GameObject mgr)
    {
        var so = new SerializedObject(mgr.GetComponent<UpgradeManager>());
        var guids = AssetDatabase.FindAssets("t:PathData", new[] { $"{Data}/Paths" });
        var array = so.FindProperty("allPaths");
        array.arraySize = guids.Length;
        for (int i = 0; i < guids.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<PathData>(AssetDatabase.GUIDToAssetPath(guids[i]));
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireSpawner()
    {
        var spawner = Object.FindFirstObjectByType<WaveSpawner>();
        if (spawner == null) return;
        // A disabled spawner never subscribes to enemy deaths, so waves would never clear.
        spawner.enabled = true;
        var so = new SerializedObject(spawner);
        var table = so.FindProperty("defaultTable");
        string[] names = { "Roach", "Ant", "Fly", "Scorpion", "Spider", "Wasp", "Beetle", "Larva", "CinderRoach", "Webspinner", "StormWasp", "AcidBeetle", "SiegeBeetle" };
        table.arraySize = names.Length;
        for (int i = 0; i < names.Length; i++)
            table.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemyData>($"{Data}/Enemies/{names[i]}.asset");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WirePlayer(GameObject player)
    {
        var firePoint = player.transform.Find("FirePoint");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        AddWeapon<MopAttack>(player, firePoint, enemyLayer);
        AddWeapon<PheromoneDartAttack>(player, firePoint, enemyLayer);
        AddWeapon<FoamSprayerAttack>(player, firePoint, enemyLayer);
        AddWeapon<JumperCablesAttack>(player, firePoint, enemyLayer);
        AddWeapon<ParticleBeamAttack>(player, firePoint, enemyLayer);
        Ensure<CharacterLoadout>(player);
        // Weapon feel tuning (tools/balance_model.py)
        var mop = Ensure<MopAttack>(player);
        var mso = new SerializedObject(mop);
        mso.FindProperty("swingCooldown").floatValue = 0.36f;
        mso.FindProperty("arcDegrees").floatValue = 140f;
        mso.FindProperty("outerFraction").floatValue = 0.6f;
        mso.FindProperty("guardFraction").floatValue = 0.3f;
        mso.ApplyModifiedPropertiesWithoutUndo();
        var hiveTune = Ensure<BeeHive>(player);
        var hso = new SerializedObject(hiveTune);
        hso.FindProperty("startBees").intValue = 8;
        hso.ApplyModifiedPropertiesWithoutUndo();
        if (player.GetComponent<BackdraftConstraint>() == null) player.AddComponent<BackdraftConstraint>();
        Ensure<BeeHive>(player);
        Ensure<PheromoneDartAttack>(player);
        var hive = Ensure<BeeSprite>(player);
        var visual = Ensure<WeaponVisual>(player);
        var flameFrames = new System.Collections.Generic.List<Sprite>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Environment/FlameStrip.png"))
            if (asset is Sprite fs) flameFrames.Add(fs);
        flameFrames.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        var vso = new SerializedObject(visual);
        var farr = vso.FindProperty("flameFrames");
        farr.arraySize = flameFrames.Count;
        for (int i = 0; i < flameFrames.Count; i++) farr.GetArrayElementAtIndex(i).objectReferenceValue = flameFrames[i];
        vso.FindProperty("smokeSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Environment/Smoke.png");
        vso.FindProperty("firePoint").objectReferenceValue = firePoint;
        string[] heldNames = { "Flamethrower", "Mop", "Foam", "Cables", "Beam" };
        var harr = vso.FindProperty("heldSprites");
        harr.arraySize = heldNames.Length;
        for (int i = 0; i < heldNames.Length; i++)
        {
            string wp = $"Assets/_Project/Art/Player/Weapons/{heldNames[i]}.png";
            ConfigurePixelSprite(wp, 100f);
            harr.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(wp);
        }
        vso.ApplyModifiedPropertiesWithoutUndo();

        ConfigurePixelSprite("Assets/_Project/Art/Enemies/Bee.png", 32f);
        var bso = new SerializedObject(hive);
        var barr = bso.FindProperty("frames");
        barr.arraySize = 1;
        barr.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Enemies/Bee.png");
        bso.ApplyModifiedPropertiesWithoutUndo();

        // Player animation: the rebuilt Animator, if Build Animations has been run
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Art/Player/PlayerAnimator.controller");
        if (controller != null)
        {
            var animator = Ensure<Animator>(player);
            animator.runtimeAnimatorController = controller;
            var idle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Player/PlayerSheet.png");
            var sr = player.GetComponent<SpriteRenderer>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Player/PlayerSheet.png"))
                if (asset is Sprite s && s.name == "Player_00") { sr.sprite = s; break; }
            SetRef(player.GetComponent<PlayerAnimation>(), "animator", animator);
        }
    }

    private static void ConfigurePixelSprite(string path, float ppu)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.SaveAndReimport();
    }

    private static void AddWeapon<T>(GameObject player, Transform firePoint, int enemyLayer) where T : Behaviour
    {
        var weapon = Ensure<T>(player);
        var so = new SerializedObject(weapon);
        var fp = so.FindProperty("firePoint");
        if (fp != null) fp.objectReferenceValue = firePoint;
        var mask = so.FindProperty("enemyMask");
        if (mask != null && enemyLayer >= 0) mask.intValue = 1 << enemyLayer;
        so.ApplyModifiedPropertiesWithoutUndo();
        weapon.enabled = false; // CharacterLoadout enables the chosen one
    }

    private static void WireUi()
    {
        var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI Toolkit/PanelSettings.asset");
        EnsureDocument<LobbyScreenView>("Lobby Screen", "Assets/_Project/UI/LobbyScreen.uxml", panel, 30);
        EnsureDocument<UpgradeScreenView>("Upgrade Screen", "Assets/_Project/UI/UpgradeScreen.uxml", panel, 10);
        EnsureDocument<RunEndScreenView>("Run End Screen", "Assets/_Project/UI/RunEndScreen.uxml", panel, 20);
        EnsureDocument<ShopScreenView>("Shop Screen", "Assets/_Project/UI/ShopScreen.uxml", panel, 12);
        EnsureDocument<MapScreenView>("Map Screen", "Assets/_Project/UI/MapScreen.uxml", panel, 11);
        EnsureDocument<DocumentPopupView>("Document Popup", "Assets/_Project/UI/DocumentPopup.uxml", panel, 14);
    }

    private static void EnsureDocument<TView>(string name, string uxml, PanelSettings panel, int order) where TView : Component
    {
        var go = GameObject.Find(name) ?? new GameObject(name);
        var doc = Ensure<UIDocument>(go);
        doc.panelSettings = panel;
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxml);
        doc.sortingOrder = order;
        Ensure<TView>(go);
    }

    // Leftovers from prototyping that would fight the generated arenas and wave spawner
    private static void RemovePrototypeObjects()
    {
        var oldScorpion = GameObject.Find("Scorpion");
        if (oldScorpion != null && oldScorpion.GetComponent<Enemy>() != null) Object.DestroyImmediate(oldScorpion);
        var dummy = GameObject.Find("DebugDude");
        if (dummy != null) dummy.SetActive(false);
    }
}
#endif
