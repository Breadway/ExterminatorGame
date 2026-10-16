using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The break-room shop. It sells a rolled stock of upgrades, a repair, and a reroll for the run's
/// scrap, then lets the player leave. It owns the stock and the purchase rules; ShopScreenView only
/// shows it. Spending uses the run's scrap pool (MetaProgression.SpendRun), so scrap earned this
/// run is what pays, and unspent scrap is still banked when the run ends.
/// </summary>
public class ShopRoom : MonoBehaviour
{
    public static ShopRoom Instance { get; private set; }

    public class Offer
    {
        public UpgradeData upgrade;   // null for the repair
        public bool isRepair;
        public int price;
        public bool sold;
    }

    [Header("Stock")]
    [SerializeField] private int upgradeSlots = 3;
    [SerializeField] private int basePrice = 24;
    [SerializeField] private float pricePerRoom = 1.5f;
    [SerializeField] private float rarePriceMultiplier = 1.8f;
    [SerializeField] private float epicPriceMultiplier = 3.2f;

    [Header("Repair and reroll")]
    [SerializeField] private float repairHealFraction = 0.5f;
    [SerializeField] private int repairBasePrice = 18;
    [SerializeField] private int rerollBasePrice = 10;

    public bool IsOpen { get; private set; }
    public IReadOnlyList<Offer> Stock => stock;
    public int RerollPrice => rerollBasePrice + rerolls * 6;
    public int Funds => MetaProgression.Instance != null ? MetaProgression.Instance.RunScrap : 0;

    private readonly List<Offer> stock = new List<Offer>();
    private int rerolls;
    private int roomNumber;
    private System.Action onClosed;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Open the shop for a room. onClosed runs when the player leaves.</summary>
    public void Open(int room, System.Action closed)
    {
        roomNumber = room;
        onClosed = closed;
        rerolls = 0;
        Roll();
        IsOpen = true;
        Time.timeScale = 0f;
        GameEvents.ChangeGameState(GameState.Shop);
        GameEvents.ShopToggled(true);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        Time.timeScale = 1f;
        GameEvents.ChangeGameState(GameState.Playing);
        GameEvents.ShopToggled(false);
        var callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    // ========================================
    // STOCK
    // ========================================
    private void Roll()
    {
        stock.Clear();
        var player = PlayerController.Instance;
        var manager = UpgradeManager.Instance;
        if (player != null && manager != null)
        {
            var pool = manager.GetAvailableUpgrades(player.stats);
            pool.RemoveAll(u => u is PathData); // paths belong to level-ups, not to a shop shelf
            for (int i = 0; i < upgradeSlots && pool.Count > 0; i++)
            {
                var pick = WeightedPick(pool);
                pool.Remove(pick);
                stock.Add(new Offer { upgrade = pick, price = PriceOf(pick) });
            }
        }
        stock.Add(new Offer { isRepair = true, price = repairBasePrice + Mathf.RoundToInt(roomNumber * 0.8f) });
    }

    private static UpgradeData WeightedPick(List<UpgradeData> pool)
    {
        float total = 0f;
        foreach (var u in pool) total += Weight(u);
        float roll = Random.value * total;
        foreach (var u in pool)
        {
            roll -= Weight(u);
            if (roll <= 0f) return u;
        }
        return pool[pool.Count - 1];
    }

    private static float Weight(UpgradeData u)
    {
        switch (u.rarity)
        {
            case UpgradeRarity.Epic: return 5f;
            case UpgradeRarity.Rare: return 25f;
            default: return 70f;
        }
    }

    public int PriceOf(UpgradeData upgrade)
    {
        float multiplier = upgrade.rarity == UpgradeRarity.Epic ? epicPriceMultiplier : upgrade.rarity == UpgradeRarity.Rare ? rarePriceMultiplier : 1f;
        return Mathf.Max(1, Mathf.RoundToInt((basePrice + roomNumber * pricePerRoom) * multiplier));
    }

    // ========================================
    // ACTIONS
    // ========================================
    public bool CanAfford(int price) => Funds >= price;

    public bool Buy(int index)
    {
        if (!IsOpen || index < 0 || index >= stock.Count) return false;
        var offer = stock[index];
        if (offer.sold) return false;
        var player = PlayerController.Instance;
        if (player == null) return false;

        if (offer.isRepair)
        {
            player.GetHealth(out var health);
            if (health == null || health.CurrentHP >= health.MaxHP) return Deny();
        }
        else if (!UpgradeManager.Instance.CanTakeUpgrade(offer.upgrade, player.stats)) return Deny();

        if (MetaProgression.Instance == null || !MetaProgression.Instance.SpendRun(offer.price)) return Deny();

        if (offer.isRepair)
        {
            player.GetHealth(out var health);
            health.Heal(health.MaxHP * repairHealFraction);
        }
        else
        {
            UpgradeManager.Instance.ApplyUpgrade(offer.upgrade);
        }
        offer.sold = true;
        GameEvents.PlaySound("ui_buy");
        return true;
    }

    public bool Reroll()
    {
        if (!IsOpen) return false;
        if (MetaProgression.Instance == null || !MetaProgression.Instance.SpendRun(RerollPrice)) return Deny();
        rerolls++;
        Roll();
        GameEvents.PlaySound("ui_click");
        return true;
    }

    private static bool Deny()
    {
        GameEvents.PlaySound("ui_deny");
        return false;
    }
}
