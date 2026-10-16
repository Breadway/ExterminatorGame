using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runs a full run: a chain of rooms (combat, elite, shop, unknown, bosses), each one a wave
/// fought inside an arena. Assign RunData to enable it; without RunData the original endless
/// wave behaviour is left untouched. After the final boss the player may continue into endless mode.
/// </summary>
public class RoomManager : MonoBehaviour {
    public static RoomManager Instance { get; private set; }

    [SerializeField] private RunData runData;
    [SerializeField] private float breatherSeconds = 2.5f;

    public RunData Run => runData;
    public int CurrentRoom { get; private set; } = -1;
    public bool IsComplete { get; private set; }
    public bool IsEndless { get; private set; }
    public RoomDefinition CurrentDefinition { get; private set; }
    public RoomLayoutData CurrentLayout { get; private set; }
    public bool IsActive => runData != null;

    private bool roomInProgress;
    private bool runOver;
    private bool waitingForPlayerChoice;
    private bool mapPending;
    private RoomDefinition[] mapOptions = System.Array.Empty<RoomDefinition>();
    private int roomsCleared;
    private Coroutine flow;
    private RoomLayoutBuilder builder;
    private readonly List<RoomDefinition> plan = new List<RoomDefinition>();

    public bool IsChoosingMap => mapOptions.Length > 0;
    public System.Collections.Generic.IReadOnlyList<RoomDefinition> MapOptions =>
        System.Array.AsReadOnly(mapOptions);

    void Awake() {
        if (Instance == null) Instance = this;
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    void OnEnable() {
        GameEvents.OnWaveCleared += HandleWaveCleared;
        GameEvents.OnRunEnded += HandleRunEnded;
    }

    void OnDisable() {
        GameEvents.OnWaveCleared -= HandleWaveCleared;
        GameEvents.OnRunEnded -= HandleRunEnded;
    }

    void Start() {
        if (runData == null) return;
        builder = GetComponent<RoomLayoutBuilder>();
        if (builder == null) builder = gameObject.AddComponent<RoomLayoutBuilder>();

        var spawner = WaveSpawner.Instance != null ? WaveSpawner.Instance : FindFirstObjectByType<WaveSpawner>();
        if (spawner != null) spawner.ExternalControl = true;

        BuildPlan();
        flow = StartCoroutine(BeginRoomAfter(1.5f));
    }

    // ========================================
    // PLAN
    // ========================================

    /// <summary>Decide the whole run up front so the HUD can show what is coming and tests can inspect it.</summary>
    public IReadOnlyList<RoomDefinition> Plan => plan;

    private void BuildPlan() {
        plan.Clear();
        int total = Mathf.Max(1, runData.RoomCount);
        for (int i = 0; i < total; i++) plan.Add(MakeRoom(i, false));
    }

    private RoomDefinition MakeRoom(int index, bool endless) {
        int number = index + 1;
        var room = new RoomDefinition { roomName = LoreSystem.SectorName(number, true) };
        room.enemyCount = Mathf.Clamp(Mathf.RoundToInt(runData.BaseEnemies + index * runData.EnemiesPerRoom), 1, runData.MaxEnemies);
        room.enemyTable = runData.EnemyTable;

        bool isFinal = !endless && number == runData.RoomCount;
        bool isMid = !endless && runData.MidBossRoom > 0 && number == runData.MidBossRoom && runData.MidBoss != null;
        bool endlessBoss = endless && runData.EndlessBossEvery > 0 && number % runData.EndlessBossEvery == 0;

        if (isFinal || isMid || endlessBoss) {
            room.type = RoomType.Boss;
            room.boss = isMid ? runData.MidBoss : (runData.FinalBoss != null ? runData.FinalBoss : runData.MidBoss);
            if (endlessBoss && runData.MidBoss != null && runData.FinalBoss != null)
                room.boss = (number / runData.EndlessBossEvery) % 2 == 0 ? runData.MidBoss : runData.FinalBoss;
            room.enemyCount = Mathf.Max(2, room.enemyCount / 3);
            room.roomName = $"SECTOR {number} - CONTAINMENT BREACH";
            return room;
        }

        float roll = Random.value;
        bool previousWasShop = plan.Count > 0 && plan[plan.Count - 1].type == RoomType.Shop;
        bool nearBoss = runData.MidBossRoom > 0 && Mathf.Abs(number - runData.MidBossRoom) <= 1;
        bool nearEnd = !endless && number >= runData.RoomCount - 1;
        if (number > 3 && !previousWasShop && !nearBoss && !nearEnd && Random.value < runData.ShopChance) {
            room.type = RoomType.Shop;
            room.enemyCount = 0;
            room.roomName = $"SECTOR {number} - BREAK ROOM";
            return room;
        }
        if (roll < runData.EliteChance && number > 2) {
            room.type = RoomType.Elite;
            room.roomName = $"SECTOR {number} - HIGH RISK";
            room.modifier = RoomModifier.Hardened;
            room.xpMultiplier = 1.6f;
        } else if (roll < runData.EliteChance + runData.UnknownChance) {
            room.type = RoomType.Unknown;
            room.modifier = number <= 2 ? RoomModifier.None : RandomModifier();
            room.roomName += " - UNCLASSIFIED";
            room.eventId = RoomEventDirector.RollEventId(number);
        } else if (Random.value < runData.ModifierChance && number > 2) {
            room.modifier = RandomModifier();
        }
        return room;
    }

    private static RoomModifier RandomModifier() {
        var values = (RoomModifier[])System.Enum.GetValues(typeof(RoomModifier));
        return values[Random.Range(1, values.Length)];
    }

    // ========================================
    // FLOW
    // ========================================

    private IEnumerator BeginRoomAfter(float delay) {
        yield return new WaitForSeconds(delay);
        while (waitingForPlayerChoice || Time.timeScale <= 0f) yield return null;
        BeginRoom();
    }

    private void BeginRoom() {
        if (runOver) return;
        int index = roomsCleared;
        CurrentRoom = index;
        while (plan.Count <= index) plan.Add(MakeRoom(plan.Count, IsEndless));
        CurrentDefinition = plan[index];

        CurrentLayout = PickLayout(CurrentDefinition);
        var player = PlayerController.Instance;
        builder.Build(CurrentLayout, player != null ? player.transform : null);

        if (CurrentDefinition.type == RoomType.Shop) {
            RunShopRoom(index);
            return;
        }

        var spawner = WaveSpawner.Instance;
        if (spawner == null) return;
        spawner.SetArenaBounds(CurrentLayout.Bounds);

        var spec = new WaveSpec {
            enemyCount = CurrentDefinition.enemyCount,
            table = CurrentDefinition.enemyTable != null ? new List<EnemyData>(CurrentDefinition.enemyTable) : null,
            boss = CurrentDefinition.boss,
            xpMultiplier = CurrentDefinition.xpMultiplier * (1f + index * 0.1f),
            healthMultiplier = 1f + index * runData.HealthPerRoom,
            damageMultiplier = 1f + index * 0.035f,
            speedMultiplier = 1f + Mathf.Min(runData.MaxSpeedBonus, index * runData.SpeedPerRoom),
        };
        RoomModifiers.Apply(CurrentDefinition.modifier, spec);
        RoomEventDirector.Instance?.Apply(CurrentDefinition, spec, CurrentLayout.Bounds);

        roomInProgress = true;
        GameEvents.EnterRoom(index);
        GameEvents.RoomStarted(index, CurrentDefinition);
        if (CurrentDefinition.type == RoomType.Boss) GameEvents.BossRoomEntered();

        // Spawn with the wave counter lined up to the room number
        spawner.DebugSetWave(index);
        if (!spawner.StartWave(spec)) {
            roomInProgress = false;
            flow = StartCoroutine(BeginRoomAfter(1f));
        }
    }

    // A break room: no enemies. The shop screen pauses the game; leaving it moves on to the next sector.
    private void RunShopRoom(int index) {
        GameEvents.EnterRoom(index);
        GameEvents.RoomStarted(index, CurrentDefinition);
        var shop = ShopRoom.Instance;
        if (shop == null) {
            // No shop in the scene: fall back to a free heal so the room is never a dead end
            GrantReward(runData.ShopHealFraction);
            FinishShopRoom(index);
            return;
        }
        shop.Open(index + 1, () => FinishShopRoom(index));
    }

    private void FinishShopRoom(int index) {
        GameEvents.RoomCleared(index);
        roomsCleared++;
        flow = StartCoroutine(BeginRoomAfter(breatherSeconds * 0.6f));
    }

    private RoomLayoutData PickLayout(RoomDefinition room) {
        var layouts = runData.Layouts;
        if (layouts == null || layouts.Length == 0) return RoomLayoutBuilder.DefaultLayout;
        var candidates = new List<RoomLayoutData>();
        foreach (var l in layouts) {
            if (l != null && l.bossArena == (room.type == RoomType.Boss)) candidates.Add(l);
        }
        if (candidates.Count == 0) return layouts[0] != null ? layouts[0] : RoomLayoutBuilder.DefaultLayout;
        return candidates[Random.Range(0, candidates.Count)];
    }

    private void HandleWaveCleared(int wave) {
        if (runData == null || runOver || !roomInProgress) return;
        roomInProgress = false;
        roomsCleared++;
        GameEvents.RoomCleared(CurrentRoom);

        bool wasFinalRoom = !IsEndless && roomsCleared >= Mathf.Max(1, runData.RoomCount);
        if (wasFinalRoom) {
            IsComplete = true;
            GameEvents.EndRun(true);
            return;
        }

        if (CurrentDefinition != null && CurrentDefinition.type == RoomType.Elite) GrantReward(0.25f);

        // Branching map: the next sector is the player's call, unless it is a fixed boss room.
        // Level-up choices resolve first so two pause screens never overlap.
        if (IsBossRoomNumber(roomsCleared + 1)) {
            flow = StartCoroutine(BeginRoomAfter(breatherSeconds));
            return;
        }
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsChoosing) {
            mapPending = true;
            return;
        }
        OfferMap();
    }

    void Update() {
        if (mapPending && !runOver && !roomInProgress) {
            bool upgradesOpen = UpgradeManager.Instance != null && UpgradeManager.Instance.IsChoosing;
            bool paused = Time.timeScale <= 0f;
            if (!upgradesOpen && !paused) {
                mapPending = false;
                OfferMap();
            }
        }
    }

    private bool IsBossRoomNumber(int number) {
        if (runData == null) return false;
        if (!IsEndless && runData.MidBossRoom > 0 && number == runData.MidBossRoom) return true;
        if (!IsEndless && number == Mathf.Max(1, runData.RoomCount)) return true;
        if (IsEndless && runData.EndlessBossEvery > 0 && number % runData.EndlessBossEvery == 0) return true;
        return false;
    }

    /// <summary>Build 2-3 distinct next-sector options and pause for the player's pick.</summary>
    public void OfferMap() {
        if (runData == null || runOver || roomInProgress || IsChoosingMap) return;
        mapOptions = MakeMapOptions(roomsCleared);
        if (mapOptions.Length == 0) {
            flow = StartCoroutine(BeginRoomAfter(breatherSeconds));
            return;
        }
        waitingForPlayerChoice = true;
        Time.timeScale = 0f;
        GameEvents.ChangeGameState(GameState.MapChoice);
        GameEvents.MapOffered(roomsCleared, (RoomDefinition[])mapOptions.Clone());
    }

    /// <summary>Take the chosen next sector and continue the run. Rejects stale or unknown choices.</summary>
    public bool ChooseMap(int index) {
        if (!IsChoosingMap || index < 0 || index >= mapOptions.Length) return false;
        while (plan.Count <= roomsCleared) plan.Add(MakeRoom(plan.Count, IsEndless));
        plan[roomsCleared] = mapOptions[index];
        mapOptions = System.Array.Empty<RoomDefinition>();
        waitingForPlayerChoice = false;
        Time.timeScale = 1f;
        GameEvents.ChangeGameState(GameState.Playing);
        flow = StartCoroutine(BeginRoomAfter(breatherSeconds * 0.5f));
        return true;
    }

    private RoomDefinition[] MakeMapOptions(int index) {
        var options = new List<RoomDefinition>();
        var types = new List<RoomType> { RoomType.Combat, RoomType.Elite, RoomType.Unknown };
        // Shops appear as a choice when the pacing rules allow one
        bool previousWasShop = plan.Count > 0 && plan[plan.Count - 1].type == RoomType.Shop;
        bool nearBoss = runData.MidBossRoom > 0 && Mathf.Abs((index + 1) - runData.MidBossRoom) <= 1;
        bool nearEnd = !IsEndless && (index + 1) >= runData.RoomCount - 1;
        if (index + 1 > 3 && !previousWasShop && !nearBoss && !nearEnd && Random.value < 0.65f) {
            types[Random.Range(0, types.Count)] = RoomType.Shop;
        }
        foreach (var type in types) {
            var room = MakeRoomShell(index);
            room.type = type;
            FinishMapOption(room, type, index);
            options.Add(room);
        }
        return options.ToArray();
    }

    private RoomDefinition MakeRoomShell(int index) {
        int number = index + 1;
        return new RoomDefinition {
            roomName = LoreSystem.SectorName(number, true),
            enemyCount = Mathf.Clamp(Mathf.RoundToInt(runData.BaseEnemies + index * runData.EnemiesPerRoom), 1, runData.MaxEnemies),
            enemyTable = runData.EnemyTable,
        };
    }

    private void FinishMapOption(RoomDefinition room, RoomType type, int index) {
        switch (type) {
            case RoomType.Elite:
                room.roomName += " - HIGH RISK";
                room.modifier = RoomModifier.Hardened;
                room.xpMultiplier = 1.6f;
                break;
            case RoomType.Unknown:
                room.roomName += " - UNCLASSIFIED";
                room.modifier = index + 1 <= 2 ? RoomModifier.None : RandomModifier();
                room.eventId = RoomEventDirector.RollEventId(index + 1);
                break;
            case RoomType.Shop:
                room.roomName += " - BREAK ROOM";
                room.enemyCount = 0;
                break;
        }
    }

    // Elite rooms heal a little; shop-like rest is handled by ShopHeal.
    private void GrantReward(float healFraction) {
        var player = PlayerController.Instance;
        if (player == null) return;
        player.GetHealth(out var health);
        if (health != null) health.Heal(health.MaxHP * healFraction);
    }

    private void HandleRunEnded(bool victory) {
        roomInProgress = false;
        mapPending = false;
        mapOptions = System.Array.Empty<RoomDefinition>();
        if (!victory) {
            runOver = true;
            IsComplete = true;
        }
    }

    /// <summary>Continue past the final room in endless mode. Called by the run-end screen.</summary>
    public void ContinueEndless() {
        if (runData == null || !IsComplete || runOver) return;
        IsEndless = true;
        IsComplete = false;
        GameEvents.EndlessStarted();
        Time.timeScale = 1f;
        GameEvents.ChangeGameState(GameState.Playing);
        var spawner = WaveSpawner.Instance;
        if (spawner != null) spawner.ResumeAfterVictory();
        UpgradeManager.Instance?.ResumeAfterVictory();
        flow = StartCoroutine(BeginRoomAfter(1.5f));
    }

    public void SetWaitingForPlayerChoice(bool waiting) {
        waitingForPlayerChoice = waiting;
    }
}
