using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent progression: scrap earned each run, lobby purchases, character unlocks and run
/// records. Pure bookkeeping over SaveSystem; scene objects subscribe to its events instead of
/// reading the save directly.
/// </summary>
public class MetaProgression : MonoBehaviour
{
    public static MetaProgression Instance { get; private set; }

    [Tooltip("Optional override. Empty = load every asset in Resources/Meta and Resources/Characters.")]
    [SerializeField] private MetaUpgradeData[] upgrades;
    [SerializeField] private CharacterData[] characters;

    [Header("Scrap rewards")]
    [SerializeField] private int scrapPerRoom = 6;
    [SerializeField] private int scrapPerBoss = 40;
    [SerializeField] private int victoryBonus = 80;

    private int runScrap;
    private int runKills;
    private bool runRecorded;

    public int Scrap => SaveSystem.Data.scrap;
    public int RunScrap => runScrap;
    public IReadOnlyList<MetaUpgradeData> Upgrades => upgrades;
    public IReadOnlyList<CharacterData> Characters => characters;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (upgrades == null || upgrades.Length == 0) upgrades = Resources.LoadAll<MetaUpgradeData>("Meta");
        if (characters == null || characters.Length == 0) characters = Resources.LoadAll<CharacterData>("Characters");
        var list = new System.Collections.Generic.List<CharacterData>(characters);
        list.RemoveAll(c => c == null);
        list.Sort((a, b) => b.unlockedByDefault.CompareTo(a.unlockedByDefault) != 0
            ? b.unlockedByDefault.CompareTo(a.unlockedByDefault) : a.unlockCost.CompareTo(b.unlockCost));
        characters = list.ToArray();
        foreach (var c in characters)
            if (c.unlockedByDefault && !SaveSystem.Data.unlockedCharacters.Contains(c.characterId))
                SaveSystem.Data.unlockedCharacters.Add(c.characterId);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        GameEvents.OnEnemyKilled += HandleKill;
        GameEvents.OnRoomCleared += HandleRoomCleared;
        GameEvents.OnBossDefeated += HandleBoss;
        GameEvents.OnRunEnded += HandleRunEnded;
        GameEvents.OnRunStarted += ResetRunTotals;
        GameEvents.OnEndlessStarted += ResetRunTotals;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyKilled -= HandleKill;
        GameEvents.OnRoomCleared -= HandleRoomCleared;
        GameEvents.OnBossDefeated -= HandleBoss;
        GameEvents.OnRunEnded -= HandleRunEnded;
        GameEvents.OnRunStarted -= ResetRunTotals;
        GameEvents.OnEndlessStarted -= ResetRunTotals;
    }

    private void ResetRunTotals()
    {
        runScrap = 0;
        runKills = 0;
        runRecorded = false;
    }

    // ---- earning ----
    private void HandleKill(Enemy enemy)
    {
        runKills++;
        int value = enemy != null && enemy.EnemyData != null ? enemy.EnemyData.scrapValue : 1;
        Earn(Mathf.RoundToInt(value * ScrapMultiplier() * UpgradeEffects.ScrapMultiplier()));
    }

    private void HandleRoomCleared(int room) { Earn(scrapPerRoom); }
    private void HandleBoss(string name) { Earn(scrapPerBoss); }

    /// <summary>Spend scrap earned this run (shop). Fails without changing anything if short.</summary>
    public bool SpendRun(int amount)
    {
        if (amount <= 0) return true;
        if (runScrap < amount) return false;
        runScrap -= amount;
        GameEvents.ScrapChanged(runScrap, -amount);
        return true;
    }

    /// <summary>Scrap accumulates in the run total and is banked when the run ends.</summary>
    public void Earn(int amount)
    {
        if (amount <= 0) return;
        runScrap += amount;
        GameEvents.ScrapChanged(runScrap, amount);
    }

    private void HandleRunEnded(bool victory)
    {
        if (runRecorded) return;
        runRecorded = true;
        if (victory) runScrap += victoryBonus;

        var data = SaveSystem.Data;
        data.scrap += runScrap;
        data.lifetimeScrap += runScrap;
        data.totalRuns++;
        data.totalKills += runKills;
        if (victory) { data.victories++; data.endlessUnlocked = true; }
        int room = RoomManager.Instance != null ? RoomManager.Instance.CurrentRoom + 1 : 0;
        data.bestRoom = Mathf.Max(data.bestRoom, room);
        if (XPManager.Instance != null) data.bestLevel = Mathf.Max(data.bestLevel, XPManager.Instance.GetCurrentLevel());
        SaveSystem.Save();
    }

    // ---- spending ----
    public int LevelOf(MetaUpgradeData u) => SaveSystem.GetMetaLevel(u.upgradeId);

    public bool CanBuy(MetaUpgradeData u)
    {
        int level = LevelOf(u);
        return level < u.maxLevel && Scrap >= u.CostForLevel(level);
    }

    public bool Buy(MetaUpgradeData u)
    {
        if (u == null || !CanBuy(u)) return false;
        int level = LevelOf(u);
        SaveSystem.Data.scrap -= u.CostForLevel(level);
        SaveSystem.SetMetaLevel(u.upgradeId, level + 1);
        SaveSystem.Save();
        return true;
    }

    public bool IsUnlocked(CharacterData c) => SaveSystem.Data.unlockedCharacters.Contains(c.characterId);

    public bool Unlock(CharacterData c)
    {
        if (c == null || IsUnlocked(c) || Scrap < c.unlockCost) return false;
        SaveSystem.Data.scrap -= c.unlockCost;
        SaveSystem.Data.unlockedCharacters.Add(c.characterId);
        SaveSystem.Save();
        return true;
    }

    public void SelectCharacter(CharacterData c)
    {
        if (c == null || !IsUnlocked(c)) return;
        SaveSystem.Data.selectedCharacter = c.characterId;
        SaveSystem.Save();
    }

    public CharacterData SelectedCharacter()
    {
        if (characters.Length == 0) return null;
        foreach (var c in characters) if (c.characterId == SaveSystem.Data.selectedCharacter && IsUnlocked(c)) return c;
        foreach (var c in characters) if (IsUnlocked(c)) return c;
        return characters[0];
    }

    // ---- bonuses ----
    public float Bonus(MetaStat stat)
    {
        float total = 0f;
        foreach (var u in upgrades)
            if (u != null && u.stat == stat) total += u.perLevel * LevelOf(u);
        return total;
    }

    public float ScrapMultiplier() => 1f + Bonus(MetaStat.ScrapGain);
    public float XPMultiplier() => 1f + Bonus(MetaStat.XPGain);
}
