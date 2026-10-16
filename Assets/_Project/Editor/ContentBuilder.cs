#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates or updates every ScriptableObject and prefab the game ships with, from the tables in
/// this file. Safe to run repeatedly: assets are found by path and overwritten in place, so
/// scene and prefab references to them keep working. Run from Exterminator > Build Content.
/// </summary>
public static class ContentBuilder
{
    private const string Root = "Assets/_Project";
    private const string EnemyDir = Root + "/Data/Enemies";
    private const string PathDir = Root + "/Data/Paths";
    private const string LayoutDir = Root + "/Data/Layouts";
    private const string CharDir = Root + "/Resources/Characters";
    private const string MetaDir = Root + "/Resources/Meta";
    private const string PrefabDir = Root + "/Prefabs";

    private static GameObject fireSpitPrefab, webSpitPrefab, shockSpitPrefab;

    [MenuItem("Exterminator/Build Content")]
    public static void BuildAll()
    {
        foreach (var dir in new[] { EnemyDir, PathDir, LayoutDir, CharDir, MetaDir, PrefabDir + "/Enemies", PrefabDir + "/Projectiles", PrefabDir + "/Pickups" })
            EnsureFolder(dir);

        var spit = BuildProjectile();
        BuildScrapPickup();
        BuildDocumentPickup();
        BuildStatusEffects();
        fireSpitPrefab = BuildStatusProjectile("FireSpit", new Color(1f, 0.45f, 0.1f), "Fire", 1f, false,
            HazardKind.Fire, 0.8f, 6f, 6f, 1f);
        webSpitPrefab = BuildStatusProjectile("WebSpit", new Color(0.92f, 0.92f, 0.96f), "Webbed", 1f, true,
            HazardKind.Web, 1.2f, 5f, 0f, 0.45f);
        shockSpitPrefab = BuildStatusProjectile("ShockSpit", new Color(0.45f, 0.85f, 1f), "Shocked", 1f, false,
            HazardKind.Shock, 0.8f, 3f, 0f, 1f);
        var heal = BuildHealPickup();
        var enemies = BuildEnemies(spit);
        BuildPaths();
        BuildUpgradeBehaviours();
        BuildLayouts();
        BuildCharacters();
        BuildMetaUpgrades();
        BuildRun(enemies);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Content built.");
    }

    // ========================================
    // HELPERS
    // ========================================

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    private static T Asset<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var created = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    private static Sprite SpriteAt(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/Art/Enemies/{name}.png");
    }

    // ========================================
    // PREFABS
    // ========================================

    private static GameObject BuildProjectile()
    {
        string path = PrefabDir + "/Projectiles/Spit.prefab";
        var go = new GameObject("Spit");
        go.layer = 0;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteAt("Spit");
        sr.sortingOrder = 3;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.14f;
        go.AddComponent<Projectile>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void SetEffect(StatusEffectData e, StatusEffectType type, string displayName, float duration,
        float tick, float dps, bool stack, int maxStacks, float moveMult, float takenMult)
    {
        var so = new SerializedObject(e);
        so.FindProperty("effectType").enumValueIndex = (int)type;
        so.FindProperty("effectName").stringValue = displayName;
        so.FindProperty("duration").floatValue = duration;
        so.FindProperty("tickInterval").floatValue = tick;
        so.FindProperty("damagePerTick").floatValue = dps;
        so.FindProperty("canStack").boolValue = stack;
        so.FindProperty("maxStacks").intValue = maxStacks;
        so.FindProperty("refreshDurationOnReapply").boolValue = true;
        so.FindProperty("moveSpeedMultiplier").floatValue = moveMult;
        so.FindProperty("damageTakenMultiplier").floatValue = takenMult;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(e);
    }

    private static void BuildStatusEffects()
    {
        var fire = Asset<StatusEffectData>($"{Root}/Data/StatusEffects/Fire.asset");
        SetEffect(fire, StatusEffectType.Fire, "Burning", 8f, 0.5f, 4f, false, 1, 1f, 1f);
        var spread = new SerializedObject(fire);
        spread.FindProperty("canSpread").boolValue = true;
        spread.ApplyModifiedPropertiesWithoutUndo();

        var poison = Asset<StatusEffectData>($"{Root}/Data/StatusEffects/Poison.asset");
        SetEffect(poison, StatusEffectType.Poison, "Envenomed", 4f, 0.5f, 2f, true, 3, 1f, 1f);

        var web = Asset<StatusEffectData>($"{Root}/Data/StatusEffects/Webbed.asset");
        SetEffect(web, StatusEffectType.Web, "Webbed", 2.5f, 0.5f, 0f, false, 1, 0.4f, 1f);

        var shock = Asset<StatusEffectData>($"{Root}/Data/StatusEffects/Shocked.asset");
        SetEffect(shock, StatusEffectType.Shock, "Shocked", 3f, 0.5f, 0f, false, 1, 0.9f, 1.25f);
    }

    private static GameObject BuildStatusProjectile(string name, Color tint, string effectAsset, float chance,
        bool leaveHazard, HazardKind kind, float radius, float lifetime, float dps, float slow)
    {
        string path = PrefabDir + "/Projectiles/" + name + ".prefab";
        var go = new GameObject(name);
        go.layer = 0;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteAt("Spit");
        sr.color = tint;
        sr.sortingOrder = 3;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.14f;
        var status = go.AddComponent<StatusProjectile>();
        var so = new SerializedObject(status);
        so.FindProperty("statusEffect").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<StatusEffectData>($"{Root}/Data/StatusEffects/" + effectAsset + ".asset");
        so.FindProperty("statusChance").floatValue = chance;
        so.FindProperty("leaveHazard").boolValue = leaveHazard;
        so.FindProperty("hazardKind").enumValueIndex = (int)kind;
        so.FindProperty("hazardRadius").floatValue = radius;
        so.FindProperty("hazardLifetime").floatValue = lifetime;
        so.FindProperty("hazardDps").floatValue = dps;
        so.FindProperty("hazardSlow").floatValue = slow;
        so.ApplyModifiedPropertiesWithoutUndo();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject BuildScrapPickup()
    {
        string path = PrefabDir + "/Pickups/ScrapPickup.prefab";
        var go = new GameObject("ScrapPickup");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Environment/ScrapPickup.png");
        sr.sortingOrder = 2;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.28f;
        var pickup = go.AddComponent<Pickup>();
        pickup.kind = PickupKind.Scrap;
        pickup.amount = 5f;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject BuildDocumentPickup()
    {
        string path = PrefabDir + "/Pickups/DocumentPickup.prefab";
        var go = new GameObject("DocumentPickup");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Environment/ScrapPickup.png");
        sr.color = new Color(0.5f, 0.9f, 1f);
        sr.sortingOrder = 2;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;
        go.AddComponent<DocumentPickup>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject BuildHealPickup()
    {
        string path = PrefabDir + "/Pickups/HealPickup.prefab";
        var go = new GameObject("HealPickup");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Environment/HealPickup.png");
        sr.sortingOrder = 2;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.28f;
        go.AddComponent<Pickup>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // Clone the existing scorpion prefab so every enemy shares its proven component setup.
    private static GameObject EnemyPrefab(string name, System.Action<GameObject> customize)
    {
        string path = $"{PrefabDir}/Enemies/{name}.prefab";
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Enemies/Scorpion.prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.name = name;
        customize?.Invoke(instance);
        if (instance.GetComponent<EnemyJuice>() == null) instance.AddComponent<EnemyJuice>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
        Object.DestroyImmediate(instance);
        return prefab;
    }

    private static void SwapMovement<TNew>(GameObject go) where TNew : Component
    {
        var chase = go.GetComponent<ChaseMovement>();
        if (chase != null) Object.DestroyImmediate(chase);
        if (go.GetComponent<TNew>() == null) go.AddComponent<TNew>();
        var visual = go.transform.Find("MovementVector");
        if (visual != null && typeof(TNew) == typeof(KeepDistanceMovement))
        {
            var so = new SerializedObject(go.GetComponent<TNew>());
            var prop = so.FindProperty("visual");
            if (prop != null) { prop.objectReferenceValue = visual; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
    }

    // ========================================
    // ENEMIES
    // ========================================

    private struct EnemySpec
    {
        public string name;
        public string sprite;
        public float health, speed, damage, cooldown;
        public int xp, scrap, minWave;
        public float weight, reduction, scale, radius;
        public EnemyMovementType movement;
        public EnemyAttackType attack;
        public string label;
        public GameObject projectile;
        public bool deathHazard;
        public HazardKind deathHazardKind;
        public float deathHazardRadius, deathHazardLifetime, deathHazardDps;
    }

    private static Dictionary<string, EnemyData> BuildEnemies(GameObject spit)
    {
        var result = new Dictionary<string, EnemyData>();

        var scorpionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Enemies/Scorpion.prefab");
        var generic = EnemyPrefab("Bug", go => { });
        var flyer = EnemyPrefab("Flyer", go => { SwapMovement<FlyingMovement>(go); });
        var ranged = EnemyPrefab("Spitter", go =>
        {
            SwapMovement<KeepDistanceMovement>(go);
            Object.DestroyImmediate(go.GetComponent<ContactDamage>());
            go.AddComponent<RangedAttack>();
        });
        var bossPrefab = EnemyPrefab("Boss", go => { go.AddComponent<BossController>(); });

        var specs = new List<(EnemySpec spec, GameObject prefab)>
        {
            (new EnemySpec { name = "Scorpion", sprite = "Scorpion", health = 25, speed = 7, damage = 8, cooldown = 1.2f, xp = 15, scrap = 1, minWave = 1, weight = 2f, scale = 1f, radius = 0.42f }, scorpionPrefab),
            (new EnemySpec { name = "Roach", sprite = "Roach", health = 14, speed = 9.5f, damage = 5, cooldown = 0.9f, xp = 8, scrap = 1, minWave = 1, weight = 3f, scale = 1f, radius = 0.36f }, generic),
            (new EnemySpec { name = "Spider", sprite = "Spider", health = 20, speed = 8f, damage = 7, cooldown = 1.0f, xp = 14, scrap = 1, minWave = 3, weight = 1.6f, scale = 1f, radius = 0.4f }, generic),
            (new EnemySpec { name = "Wasp", sprite = "Wasp", health = 16, speed = 5.5f, damage = 6, cooldown = 1.6f, xp = 16, scrap = 2, minWave = 4, weight = 1.4f, scale = 1f, radius = 0.36f, movement = EnemyMovementType.KeepDistance, attack = EnemyAttackType.Ranged }, ranged),
            (new EnemySpec { name = "Beetle", sprite = "Beetle", health = 70, speed = 4.4f, damage = 12, cooldown = 1.4f, xp = 30, scrap = 3, minWave = 6, weight = 1f, reduction = 0.5f, scale = 1f, radius = 0.5f }, generic),
            (new EnemySpec { name = "Larva", sprite = "Larva", health = 9, speed = 5f, damage = 4, cooldown = 1.0f, xp = 4, scrap = 0, minWave = 1, weight = 2f, scale = 1f, radius = 0.28f }, generic),
            (new EnemySpec { name = "Ant", sprite = "Roach", health = 8, speed = 11f, damage = 4, cooldown = 0.7f, xp = 5, scrap = 1, minWave = 2, weight = 2.5f, scale = 0.7f, radius = 0.25f }, generic),
            (new EnemySpec { name = "Fly", sprite = "Wasp", health = 10, speed = 12f, damage = 5, cooldown = 0.8f, xp = 7, scrap = 1, minWave = 2, weight = 2f, scale = 0.7f, radius = 0.26f, movement = EnemyMovementType.FlyingMovement }, flyer),
            (new EnemySpec { name = "CinderRoach", label = "Cinder Roach", sprite = "Roach", health = 22, speed = 7f, damage = 6, cooldown = 1.8f, xp = 18, scrap = 2, minWave = 5, weight = 1.2f, scale = 1.1f, radius = 0.38f, movement = EnemyMovementType.KeepDistance, attack = EnemyAttackType.Ranged, projectile = fireSpitPrefab }, ranged),
            (new EnemySpec { name = "Webspinner", label = "Webspinner", sprite = "Spider", health = 30, speed = 5f, damage = 5, cooldown = 2.0f, xp = 24, scrap = 2, minWave = 5, weight = 1.1f, scale = 1.1f, radius = 0.42f, movement = EnemyMovementType.KeepDistance, attack = EnemyAttackType.Ranged, projectile = webSpitPrefab }, ranged),
            (new EnemySpec { name = "StormWasp", label = "Storm Wasp", sprite = "Wasp", health = 20, speed = 7f, damage = 5, cooldown = 1.1f, xp = 22, scrap = 2, minWave = 7, weight = 1f, scale = 1.1f, radius = 0.38f, movement = EnemyMovementType.KeepDistance, attack = EnemyAttackType.Ranged, projectile = shockSpitPrefab }, ranged),
            (new EnemySpec { name = "AcidBeetle", label = "Acid Beetle", sprite = "Beetle", health = 90, speed = 4f, damage = 14, cooldown = 1.5f, xp = 40, scrap = 4, minWave = 7, weight = 0.8f, reduction = 0.4f, scale = 1.1f, radius = 0.52f, deathHazard = true, deathHazardKind = HazardKind.Acid, deathHazardRadius = 1.6f, deathHazardLifetime = 6f, deathHazardDps = 8f }, generic),
            (new EnemySpec { name = "SiegeBeetle", label = "Siege Beetle", sprite = "Beetle", health = 220, speed = 2.8f, damage = 22, cooldown = 1.8f, xp = 80, scrap = 8, minWave = 9, weight = 0.5f, reduction = 0.6f, scale = 1.8f, radius = 0.8f }, generic),
        };

        foreach (var (spec, prefab) in specs)
        {
            var data = Asset<EnemyData>($"{EnemyDir}/{spec.name}.asset");
            data.enemyName = string.IsNullOrEmpty(spec.label) ? spec.name : spec.label;
            data.sprite = SpriteAt(spec.sprite);
            data.prefab = spec.name == "Scorpion" ? null : prefab;
            data.maxHealth = spec.health;
            data.moveSpeed = spec.speed;
            data.XPValue = spec.xp;
            data.scrapValue = spec.scrap;
            data.attackDamage = spec.damage;
            data.attackCooldown = spec.cooldown;
            data.minWave = spec.minWave;
            data.spawnWeight = spec.weight;
            data.damageReduction = spec.reduction;
            data.scale = spec.scale;
            data.colliderRadius = spec.radius;
            data.MovementType = spec.movement;
            data.AttackType = spec.attack;
            data.isBoss = false;
            if (spec.attack == EnemyAttackType.Ranged)
            {
                data.projectilePrefab = spec.projectile != null ? spec.projectile : spit;
                data.preferredRange = 6f;
                data.attackRange = 9.5f;
                data.projectileSpeed = 8.5f;
                data.projectileLifetime = 2.4f;
            }
            data.spawnHazardOnDeath = spec.deathHazard;
            data.deathHazard = spec.deathHazardKind;
            data.deathHazardRadius = spec.deathHazardRadius > 0f ? spec.deathHazardRadius : 1.4f;
            data.deathHazardLifetime = spec.deathHazardLifetime > 0f ? spec.deathHazardLifetime : 6f;
            data.deathHazardDps = spec.deathHazardDps;
            EditorUtility.SetDirty(data);
            result[spec.name] = data;
        }

        // Larvae grow into roaches if left alone
        result["Larva"].evolvesInto = result["Roach"];
        result["Larva"].evolveAfterSeconds = 7f;
        EditorUtility.SetDirty(result["Larva"]);

        // Bosses
        var brood = Asset<EnemyData>($"{EnemyDir}/BroodMother.asset");
        brood.enemyName = "Brood Mother";
        brood.sprite = SpriteAt("BroodMother");
        brood.prefab = bossPrefab;
        brood.isBoss = true;
        brood.maxHealth = 520; brood.moveSpeed = 3.2f; brood.attackDamage = 16; brood.attackCooldown = 1.3f;
        brood.XPValue = 120; brood.scrapValue = 20; brood.damageReduction = 0.15f;
        brood.scale = 1f; brood.colliderRadius = 0.85f;
        brood.summonType = result["Larva"]; brood.summonInterval = 6f; brood.summonCount = 5;
        brood.enrageBelowHealth = 0.35f; brood.enrageSpeedMultiplier = 1.5f;
        EditorUtility.SetDirty(brood);
        result["BroodMother"] = brood;

        var queen = Asset<EnemyData>($"{EnemyDir}/Queen.asset");
        queen.enemyName = "The Queen";
        queen.sprite = SpriteAt("Queen");
        queen.prefab = bossPrefab;
        queen.isBoss = true;
        queen.maxHealth = 1500; queen.moveSpeed = 3.8f; queen.attackDamage = 20; queen.attackCooldown = 1.1f;
        queen.XPValue = 300; queen.scrapValue = 50; queen.damageReduction = 0.2f;
        queen.scale = 1f; queen.colliderRadius = 0.95f;
        queen.summonType = result["Roach"]; queen.summonInterval = 5f; queen.summonCount = 6;
        queen.projectilePrefab = spit; queen.projectileSpeed = 7.5f; queen.projectileLifetime = 3f;
        queen.volleyInterval = 4.5f; queen.volleyCount = 12;
        queen.enrageBelowHealth = 0.4f; queen.enrageSpeedMultiplier = 1.5f;
        EditorUtility.SetDirty(queen);
        result["Queen"] = queen;
        return result;
    }

    // ========================================
    // PATHS (tier 1 at level 10, tier 2 at 20, tier 3 at 30 in endless)
    // ========================================

    private static PathData Path(string file, string id, string name, string desc, int tier, UpgradeRarity rarity,
        System.Action<PathData> stats, params PathData[] requires)
    {
        var p = Asset<PathData>($"{PathDir}/{file}.asset");
        p.upgradeID = id; p.upgradeName = name; p.description = desc; p.tier = tier;
        p.rarity = rarity; p.type = UpgradeType.Power; p.maxStacks = 1;
        p.healthBonus = 0; p.healthMultiplier = 1; p.damageBonus = 0; p.damageMultiplier = 1;
        p.critChanceBonus = 0; p.critDamageBonus = 0; p.speedMultiplier = 1; p.attackSpeedMultiplier = 1;
        p.cooldownReduction = 0; p.armorBonus = 0; p.hasSpecialEffect = false; p.specialEffectID = ""; p.specialEffectDescription = "";
        p.requiredUpgrades = requires; p.conflictingUpgrades = new UpgradeData[0];
        stats(p);
        EditorUtility.SetDirty(p);
        return p;
    }

    private static void BuildPaths()
    {
        var blaze = Path("Path_Blaze", "path_blaze", "Pyromaniac", "Burn everything. Management will call it a fire drill.", 1, UpgradeRarity.Epic,
            p => { p.damageMultiplier = 1.35f; p.attackSpeedMultiplier = 1.1f; });
        var guard = Path("Path_Guardian", "path_guardian", "Safety Officer", "Compliance is armour. You are very compliant.", 1, UpgradeRarity.Epic,
            p => { p.healthMultiplier = 1.4f; p.armorBonus = 25; p.type = UpgradeType.Defense; });
        var ghost = Path("Path_Ghost", "path_ghost", "Contract Ghost", "Nobody remembers hiring you. Nobody can catch you.", 1, UpgradeRarity.Epic,
            p => { p.speedMultiplier = 1.25f; p.cooldownReduction = 0.3f; p.critChanceBonus = 0.1f; p.type = UpgradeType.Utility; });

        Path("Path_Blaze_Inferno", "path_blaze_inferno", "Controlled Burn", "The fire is not out of control. It is the control.", 2, UpgradeRarity.Epic,
            p => { p.damageMultiplier = 1.4f; p.critChanceBonus = 0.1f; p.hasSpecialEffect = true; p.specialEffectID = "explode_on_kill"; p.specialEffectDescription = "Kills detonate."; }, blaze);
        Path("Path_Blaze_Flash", "path_blaze_flash", "Flashover", "Everything burns faster, including you.", 2, UpgradeRarity.Epic,
            p => { p.attackSpeedMultiplier = 1.35f; p.damageMultiplier = 1.2f; p.healthBonus = -15; }, blaze);
        Path("Path_Guardian_Wall", "path_guardian_wall", "Hazmat Wall", "Standard issue. Rarely issued.", 2, UpgradeRarity.Epic,
            p => { p.armorBonus = 40; p.healthBonus = 30; p.type = UpgradeType.Defense; }, guard);
        Path("Path_Guardian_Recovery", "path_guardian_recovery", "Workers' Comp", "You will be compensated. Eventually.", 2, UpgradeRarity.Epic,
            p => { p.healthMultiplier = 1.25f; p.hasSpecialEffect = true; p.specialEffectID = "lifesteal"; p.specialEffectDescription = "Heal as you exterminate."; p.type = UpgradeType.Defense; }, guard);
        Path("Path_Ghost_Rush", "path_ghost_rush", "Off The Clock", "Technically you left an hour ago.", 2, UpgradeRarity.Epic,
            p => { p.speedMultiplier = 1.25f; p.attackSpeedMultiplier = 1.2f; p.type = UpgradeType.Utility; }, ghost);
        Path("Path_Ghost_Crit", "path_ghost_crit", "Plausible Deniability", "Surgical. Untraceable. Deniable.", 2, UpgradeRarity.Epic,
            p => { p.critChanceBonus = 0.2f; p.critDamageBonus = 0.6f; }, ghost);

        // Per-character tier 1 paths: one specialty per contractor
        var ember = CharPath("Path_Firebug_Flash", "path_firebug_flash", "Flash Point", "Your fuel burns hotter than regulation allows.", "firebug",
            p => { p.damageMultiplier = 1.25f; p.speedMultiplier = 1.05f; });
        var keeper = CharPath("Path_Apiarist_Royal", "path_apiarist_royal", "Royal Pheromones", "The hive knows its queen.", "apiarist",
            p => { p.damageMultiplier = 1.15f; p.critChanceBonus = 0.1f; });
        var sanitation = CharPath("Path_Custodian_Sanitation", "path_custodian_sanitation", "Sanitation Engineer", "Certified. Laminated. Lethal.", "custodian",
            p => { p.damageMultiplier = 1.2f; p.armorBonus = 10; p.type = UpgradeType.Defense; });
        var union = CharPath("Path_Intern_Union", "path_intern_union", "Union Rep", "You have read the handbook. All of it.", "intern",
            p => { p.healthMultiplier = 1.2f; p.armorBonus = 10; p.type = UpgradeType.Defense; });
        var live = CharPath("Path_Electrician_Live", "path_electrician_live", "Live Wire", "You stopped feeling the shocks years ago.", "electrician",
            p => { p.attackSpeedMultiplier = 1.25f; p.critChanceBonus = 0.05f; });
        var peer = CharPath("Path_Scientist_Peer", "path_scientist_peer", "Peer Reviewed", "Reproducible results. Every time.", "scientist",
            p => { p.damageMultiplier = 1.3f; p.healthBonus = -10; });

        // Per-character tier 2 paths, each requiring its tier 1 parent
        CharPath("Path_Firebug_Backdraft", "path_firebug_backdraft", "Backdraft Rider", "You surf your own fire now.", "firebug",
            p => { p.damageMultiplier = 1.3f; p.armorBonus = 10; }, ember);
        CharPath("Path_Apiarist_Tyrant", "path_apiarist_tyrant", "Swarm Tyrant", "Thirty thousand wings beat as one.", "apiarist",
            p => { p.damageMultiplier = 1.25f; p.attackSpeedMultiplier = 1.2f; }, keeper);
        CharPath("Path_Custodian_Deep", "path_custodian_deep", "Deep Clean", "Nothing survives inspection.", "custodian",
            p => { p.damageMultiplier = 1.3f; p.critChanceBonus = 0.1f; }, sanitation);
        CharPath("Path_Intern_Overtime", "path_intern_overtime", "Overtime Immunity", "You cannot be fired. You have tried.", "intern",
            p => { p.healthMultiplier = 1.3f; p.speedMultiplier = 1.1f; p.type = UpgradeType.Defense; }, union);
        CharPath("Path_Electrician_Grid", "path_electrician_grid", "Grid Runner", "You are the ground wire.", "electrician",
            p => { p.damageMultiplier = 1.25f; p.cooldownReduction = 0.15f; }, live);
        CharPath("Path_Scientist_Publish", "path_scientist_publish", "Publish or Perish", "Mostly perish. Theirs.", "scientist",
            p => { p.critDamageBonus = 0.5f; p.damageMultiplier = 1.2f; }, peer);

        // Tier 3: endless mode only
        Path("Path_Overtime", "path_overtime", "Perpetual Overtime", "There is no clock. There is only the work.", 3, UpgradeRarity.Epic,
            p => { p.damageMultiplier = 1.6f; p.attackSpeedMultiplier = 1.3f; p.speedMultiplier = 1.15f; });
        Path("Path_Immortal", "path_immortal", "Contractor Of The Month", "Your picture hangs in a building that no longer exists.", 3, UpgradeRarity.Epic,
            p => { p.healthMultiplier = 1.8f; p.armorBonus = 50; p.type = UpgradeType.Defense; });
    }

    // ========================================
    // LAYOUTS
    // ========================================

    private static RoomLayoutData Layout(string file, string name, float w, float h, bool boss, params ObstacleSpec[] obstacles)
    {
        var l = Asset<RoomLayoutData>($"{LayoutDir}/{file}.asset");
        l.displayName = name;
        l.size = new Vector2(w, h);
        l.bossArena = boss;
        l.playerStart = new Vector2(0f, -h * 0.5f + 3f);
        l.obstacles = new List<ObstacleSpec>(obstacles);
        EditorUtility.SetDirty(l);
        return l;
    }

    private static ObstacleSpec Crate(float x, float y, float s = 2f) => new ObstacleSpec { center = new Vector2(x, y), size = new Vector2(s, s), kind = ObstacleKind.Crate };
    private static ObstacleSpec Pillar(float x, float y, float s = 1.6f) => new ObstacleSpec { center = new Vector2(x, y), size = new Vector2(s, s), kind = ObstacleKind.Pillar };

    private static PathData CharPath(string file, string id, string name, string desc, string characterId,
        System.Action<PathData> stats, params PathData[] requires)
    {
        var p = Path(file, id, name, desc, file.Contains("_Backdraft") || file.Contains("_Tyrant") || file.Contains("_Deep")
            || file.Contains("_Overtime") || file.Contains("_Grid") || file.Contains("_Publish") ? 2 : 1,
            UpgradeRarity.Epic, stats, requires);
        p.characterId = characterId;
        EditorUtility.SetDirty(p);
        return p;
    }

    /// <summary>Give behaviour to the upgrades whose descriptions promise more than stats.</summary>
    private static void BuildUpgradeBehaviours()
    {
        EffectFlag("Executioner", "executioner", "Finish wounded prey. Enemies below 30% HP take +50% damage; non-bosses below 8% are executed.");
        EffectFlag("Berserker Rage", "berserker_rage", "The angrier you get, the harder you hit. Missing HP becomes bonus damage.");
        EffectFlag("Momentum", "momentum", "Keep moving and keep killing. Each kill fuels the next.");
        EffectFlag("Time Dilation Field", "time_dilation", "Everything near you moves through syrup.");
        EffectFlag("Venom Rounds", "venom_rounds", "Hits sometimes envenom. Poison stacks.");
        EffectFlag("Emergency Protocols", "emergency_protocols", "Damage control activates automatically when things get rough. Once per sector.");
    }

    private static void EffectFlag(string file, string id, string desc)
    {
        var u = Asset<UpgradeData>($"{Root}/Data/Upgrades/" + file + ".asset");
        u.hasSpecialEffect = true;
        u.specialEffectID = id;
        u.specialEffectDescription = desc;
        EditorUtility.SetDirty(u);
    }

    private static void BuildLayouts()
    {
        Layout("Layout_Warehouse", "Warehouse", 26, 18, false,
            Crate(-6, 2), Crate(6, 2), Crate(0, 5), Pillar(-9, -2), Pillar(9, -2), Crate(-3, -2, 1.6f), Crate(3, -2, 1.6f));
        Layout("Layout_Corridor", "Service Corridor", 34, 11, false,
            Crate(-8, 1.5f), Crate(8, -1.5f), Pillar(0, 0), Pillar(-13, -2), Pillar(13, 2));
        Layout("Layout_Pillars", "Server Hall", 24, 20, false,
            Pillar(-6, 5), Pillar(6, 5), Pillar(-6, -1), Pillar(6, -1), Pillar(0, 2, 2.2f), Crate(-10, 7, 1.6f), Crate(10, 7, 1.6f));
        Layout("Layout_OpenFloor", "Open Floor", 30, 20, false,
            Crate(-9, 4), Crate(9, 4), Crate(0, -1), Pillar(-4, 7), Pillar(4, 7));
        Layout("Layout_BossArena", "Containment Chamber", 30, 22, true,
            Pillar(-9, 5), Pillar(9, 5), Pillar(-9, -3), Pillar(9, -3));
    }

    // ========================================
    // CHARACTERS
    // ========================================

    private static void Char(string file, string id, string name, string tag, string weapon, string weaponName,
        string power, string powerText, string constraint, string constraintText,
        float hp, float dmg, float speed, float atk, Color tint, bool unlocked, int cost)
    {
        var c = Asset<CharacterData>($"{CharDir}/{file}.asset");
        c.characterId = id; c.displayName = name; c.tagline = tag; c.weaponId = weapon; c.weaponName = weaponName;
        c.powerName = power; c.powerText = powerText; c.constraintName = constraint; c.constraintText = constraintText;
        c.maxHP = hp; c.damage = dmg; c.speed = speed; c.attackSpeed = atk; c.tint = tint;
        c.unlockedByDefault = unlocked; c.unlockCost = cost;
        EditorUtility.SetDirty(c);
    }

    private static void Kit(string file, params string[] components)
    {
        var c = AssetDatabase.LoadAssetAtPath<CharacterData>($"{CharDir}/{file}.asset");
        c.extraComponents = components;
        EditorUtility.SetDirty(c);
    }

    private static void BuildCharacters()
    {
        Char("Apiarist", "apiarist", "The Apiarist", "Former artisanal beekeeper. Former.",
            "darts", "Pheromone Dart Gun", "The Hive", "Marked kills hatch into bees that fight for you and die for you.",
            "Shared Fate", "50 HP, never healed. Damage kills bees first, one bee per point.", 50, 2, 14, 1.2f, new Color(0.95f, 1f, 0.85f), false, 200);
        Kit("Apiarist", "BeeHive");
        Char("Firebug", "firebug", "The Firebug", "I love the smell of napalm in the morning. It smells like overtime.",
            "flamethrower", "Leak-Prone Flamethrower", "Inferno", "Fire spreads between enemies and leaves burning ground.",
            "Backdraft", "Your tank leaks. Stand still for a second and your own fuel ignites.", 100, 5, 15, 1.2f, Color.white, true, 0);
        Kit("Firebug", "BackdraftConstraint");
        Char("Custodian", "custodian", "The Custodian", "Clean up on Aisle 4. And 5. And everywhere.",
            "mop", "The Mop", "Sanitize", "Hits at the far end of the swing deal triple damage.",
            "Safety Violation", "Enemies inside your guard take no damage.", 150, 5, 13.5f, 1.2f, new Color(0.93f, 0.96f, 1f), false, 150);
        Char("Intern", "intern", "The Intern", "My back hurts and I'm not even getting paid.",
            "foam", "Industrial Foam Sprayer", "Foam Fortress", "Spraying builds armor and slows enemies.",
            "Overencumbered", "More armor means slower movement.", 150, 5, 16, 1.2f, new Color(1f, 0.93f, 0.86f), false, 200);
        Char("Electrician", "electrician", "The Electrician", "It's not a safety violation if it works.",
            "cables", "Jumper Cables", "The Circuit", "Lightning chains between enemies, hitting harder each link.",
            "Overload", "Heat builds. At 100% you shock yourself and are stunned.", 100, 5, 15, 1.2f, new Color(0.9f, 0.95f, 1f), false, 250);
        Char("Scientist", "scientist", "Dr. Overkill", "The pest control is experimental. The lawsuits are real.",
            "beam", "Prototype Particle Beam", "Molecular Destabilization", "A full charge pierces everything, walls included.",
            "Critical Instability", "Holding above 75% charge risks a failure that hurts and stuns you.", 85, 5, 15, 1.2f, new Color(0.92f, 1f, 0.95f), false, 300);
    }

    // ========================================
    // META UPGRADES
    // ========================================

    private static void Meta(string file, string id, string name, string desc, MetaStat stat, float per, int max, int cost)
    {
        var m = Asset<MetaUpgradeData>($"{MetaDir}/{file}.asset");
        m.upgradeId = id; m.displayName = name; m.description = desc; m.stat = stat;
        m.perLevel = per; m.maxLevel = max; m.baseCost = cost; m.costGrowth = 1.55f;
        EditorUtility.SetDirty(m);
    }

    private static void BuildMetaUpgrades()
    {
        Meta("Meta_Health", "meta_hp", "Better Insurance", "+8% max health per level.", MetaStat.MaxHP, 0.08f, 5, 40);
        Meta("Meta_Damage", "meta_damage", "Hazard Training", "+6% damage per level.", MetaStat.Damage, 0.06f, 5, 50);
        Meta("Meta_Speed", "meta_speed", "Steel Toe Boots", "+4% move speed per level.", MetaStat.MoveSpeed, 0.04f, 4, 45);
        Meta("Meta_XP", "meta_xp", "Mentorship Program", "+10% XP per level.", MetaStat.XPGain, 0.10f, 4, 60);
        Meta("Meta_Scrap", "meta_scrap", "Salvage Rights", "+10% scrap per level.", MetaStat.ScrapGain, 0.10f, 4, 70);
    }

    // ========================================
    // RUN
    // ========================================

    private static void BuildRun(Dictionary<string, EnemyData> enemies)
    {
        var run = Asset<RunData>($"{Root}/Data/Run_Standard.asset");
        run.RoomCount = 25;
        run.MidBossRoom = 12;
        run.BaseEnemies = 5;
        run.EnemiesPerRoom = 0.8f;
        run.MaxEnemies = 55;
        run.HealthPerRoom = 0.08f;
        run.MidBoss = enemies["BroodMother"];
        run.FinalBoss = enemies["Queen"];
        run.EnemyTable = new[] { enemies["Roach"], enemies["Ant"], enemies["Fly"], enemies["Scorpion"], enemies["Spider"], enemies["Wasp"], enemies["Beetle"], enemies["Larva"], enemies["CinderRoach"], enemies["Webspinner"], enemies["StormWasp"], enemies["AcidBeetle"], enemies["SiegeBeetle"] };
        run.Layouts = new[]
        {
            AssetDatabase.LoadAssetAtPath<RoomLayoutData>($"{LayoutDir}/Layout_Warehouse.asset"),
            AssetDatabase.LoadAssetAtPath<RoomLayoutData>($"{LayoutDir}/Layout_Corridor.asset"),
            AssetDatabase.LoadAssetAtPath<RoomLayoutData>($"{LayoutDir}/Layout_Pillars.asset"),
            AssetDatabase.LoadAssetAtPath<RoomLayoutData>($"{LayoutDir}/Layout_OpenFloor.asset"),
            AssetDatabase.LoadAssetAtPath<RoomLayoutData>($"{LayoutDir}/Layout_BossArena.asset"),
        };
        EditorUtility.SetDirty(run);
    }
}
#endif
