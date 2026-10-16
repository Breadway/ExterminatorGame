using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// View for the upgrade choice screen. Listens for offers from UpgradeManager and
/// reports the player's pick back through UpgradeManager.ChooseUpgrade. Owns no game state.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UpgradeScreenView : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private VisualElement cardRow;
    private Label subtitle;
    private IVisualElementScheduledItem hideItem;

    void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        GameEvents.OnUpgradeOffered += Show;
        GameEvents.OnGameStateChanged += HandleStateChanged;
        GameEvents.OnRunEnded += HandleRunEnded;

        // An offer may have been raised before this view was enabled.
        var manager = UpgradeManager.Instance;
        if (manager != null && manager.IsChoosing)
        {
            var current = new List<UpgradeData>(manager.CurrentChoices);
            Show(XPManager.Instance != null ? XPManager.Instance.GetCurrentLevel() : 1, current.ToArray());
        }
    }

    void OnDisable()
    {
        GameEvents.OnUpgradeOffered -= Show;
        GameEvents.OnGameStateChanged -= HandleStateChanged;
        GameEvents.OnRunEnded -= HandleRunEnded;
        hideItem?.Pause();
    }

    // The UIDocument rebuilds its tree when toggled, so look elements up each time.
    private bool Bind()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return false;
        root = uiDocument.rootVisualElement.Q<VisualElement>("upgrade-root");
        if (root == null) return false;
        cardRow = root.Q<VisualElement>("card-row");
        subtitle = root.Q<Label>("upgrade-subtitle");
        HazardTape.Fill(root.Q<VisualElement>("upgrade-tape-top"));
        HazardTape.Fill(root.Q<VisualElement>("upgrade-tape-bottom"));
        return cardRow != null;
    }

    private void Show(int level, UpgradeData[] choices)
    {
        if (choices == null || choices.Length == 0 || !Bind()) return;
        hideItem?.Pause();

        subtitle.text = $"CLEARANCE LEVEL {level}  -  SELECT ONE EQUIPMENT REQUISITION";
        cardRow.Clear();
        for (int i = 0; i < choices.Length; i++)
        {
            cardRow.Add(BuildCard(choices[i], i));
        }

        root.AddToClassList("shown");
        // Add "visible" a moment later so the fade/slide transitions run. The scheduler
        // uses real time, so this still works while Time.timeScale is 0.
        root.schedule.Execute(() => root.AddToClassList("visible")).StartingIn(30);
    }

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.UpgradeChoice) Hide();
    }

    private void HandleRunEnded(bool victory)
    {
        Hide();
    }

    private void Hide()
    {
        if (root == null) return;
        root.RemoveFromClassList("visible");
        var target = root;
        hideItem = target.schedule.Execute(() => target.RemoveFromClassList("shown")).StartingIn(300);
    }

    // ========================================
    // CARD CONSTRUCTION
    // ========================================

    private VisualElement BuildCard(UpgradeData upgrade, int index)
    {
        var card = new VisualElement();
        card.AddToClassList("upgrade-card");
        card.AddToClassList("rarity-" + upgrade.rarity.ToString().ToLowerInvariant());

        var strip = new VisualElement();
        strip.AddToClassList("card-strip");
        card.Add(strip);

        var head = new VisualElement();
        head.AddToClassList("card-head");
        head.Add(MakeLabel(upgrade.rarity.ToString().ToUpperInvariant(), "card-rarity"));
        var type = MakeLabel(upgrade.type.ToString().ToUpperInvariant(), "card-type");
        type.AddToClassList("card-type-" + upgrade.type.ToString().ToLowerInvariant());
        head.Add(type);
        card.Add(head);

        // Some assets leave the display name blank; fall back to the asset name.
        string title = string.IsNullOrWhiteSpace(upgrade.upgradeName) ? upgrade.name : upgrade.upgradeName;
        card.Add(MakeLabel(title.ToUpperInvariant(), "card-name"));
        card.Add(MakeLabel(upgrade.description, "card-desc"));

        var stats = new VisualElement();
        stats.AddToClassList("card-stats");
        foreach (var line in UpgradeStatText.Describe(upgrade))
        {
            stats.Add(MakeLabel(line.text, "stat-line", line.beneficial ? "stat-up" : "stat-down"));
        }
        if (upgrade.hasSpecialEffect && !string.IsNullOrWhiteSpace(upgrade.specialEffectDescription))
        {
            stats.Add(MakeLabel(upgrade.specialEffectDescription, "stat-special"));
        }
        card.Add(stats);

        card.Add(MakeLabel($"[{index + 1}]  SIGN OFF", "card-foot"));

        int captured = index;
        card.RegisterCallback<ClickEvent>(_ => UpgradeManager.Instance?.ChooseUpgrade(captured));
        return card;
    }

    private static Label MakeLabel(string text, params string[] classes)
    {
        var label = new Label(text);
        foreach (var c in classes) label.AddToClassList(c);
        label.pickingMode = PickingMode.Ignore;
        return label;
    }
}

/// <summary>
/// Turns an upgrade's numbers into display lines. Unlike UpgradeData.GetTooltipText,
/// this includes drawbacks (negative values), so trade-off upgrades are shown honestly.
/// </summary>
public static class UpgradeStatText
{
    public struct Line
    {
        public string text;
        public bool beneficial;
    }

    public static List<Line> Describe(UpgradeData u)
    {
        var lines = new List<Line>();
        Flat(lines, u.healthBonus, "MAX HP");
        Percent(lines, u.healthMultiplier, "MAX HP");
        Flat(lines, u.damageBonus, "DAMAGE");
        Percent(lines, u.damageMultiplier, "DAMAGE");
        Points(lines, u.critChanceBonus, "CRIT CHANCE");
        Flat(lines, u.critDamageBonus, "CRIT MULTIPLIER", "x");
        Percent(lines, u.speedMultiplier, "MOVE SPEED");
        Percent(lines, u.attackSpeedMultiplier, "ATTACK SPEED");
        if (u.cooldownReduction != 0f)
        {
            // A cooldown reduction is a benefit when positive, the reverse of other stats.
            lines.Add(new Line { text = $"{Sign(-u.cooldownReduction * 100f)}% COOLDOWNS", beneficial = u.cooldownReduction > 0f });
        }
        Flat(lines, u.armorBonus, "ARMOR");
        return lines;
    }

    private static void Flat(List<Line> lines, float value, string label, string suffix = "")
    {
        if (Mathf.Approximately(value, 0f)) return;
        lines.Add(new Line { text = $"{Sign(value)}{suffix} {label}", beneficial = value > 0f });
    }

    private static void Percent(List<Line> lines, float multiplier, string label)
    {
        if (Mathf.Approximately(multiplier, 1f)) return;
        float pct = (multiplier - 1f) * 100f;
        lines.Add(new Line { text = $"{Sign(pct)}% {label}", beneficial = pct > 0f });
    }

    private static void Points(List<Line> lines, float fraction, string label)
    {
        if (Mathf.Approximately(fraction, 0f)) return;
        float pct = fraction * 100f;
        lines.Add(new Line { text = $"{Sign(pct)}% {label}", beneficial = pct > 0f });
    }

    private static string Sign(float v)
    {
        string number = Mathf.Abs(v).ToString("0.##", CultureInfo.InvariantCulture);
        return (v < 0f ? "-" : "+") + number;
    }
}
