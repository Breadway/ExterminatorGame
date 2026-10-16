using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// The between-runs hub. Shows when the scene loads (the run does not start until the player
/// clocks in) and lets them pick a character, unlock new ones and buy permanent upgrades.
/// Owns no persistent state; every action goes through MetaProgression.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class LobbyScreenView : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [Tooltip("Show the lobby when the scene starts.")]
    [SerializeField] private bool showOnStart = true;

    private VisualElement root;
    private VisualElement characterList;
    private VisualElement metaList;
    private VisualElement bestiaryList;
    private Label scrapLabel;
    private Label records;
    private Button start;
    private bool open;

    void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        GameEvents.OnRunEnded += HandleRunEnded;
    }

    void OnDisable()
    {
        GameEvents.OnRunEnded -= HandleRunEnded;
        if (start != null) start.clicked -= ClockIn;
    }

    void Start()
    {
        if (showOnStart) Open();
    }

    private bool Bind()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return false;
        root = uiDocument.rootVisualElement.Q<VisualElement>("lobby-root");
        if (root == null) return false;
        characterList = root.Q<VisualElement>("character-list");
        metaList = root.Q<VisualElement>("meta-list");
        bestiaryList = root.Q<VisualElement>("bestiary-list");
        scrapLabel = root.Q<Label>("lobby-scrap");
        records = root.Q<Label>("lobby-records");
        start = root.Q<Button>("lobby-start");
        HazardTape.Fill(root.Q<VisualElement>("lobby-tape-top"));
        HazardTape.Fill(root.Q<VisualElement>("lobby-tape-bottom"));
        start.clicked -= ClockIn;
        start.clicked += ClockIn;
        return true;
    }

    public void Open()
    {
        if (!Bind()) return;
        open = true;
        Time.timeScale = 0f;
        RoomManager.Instance?.SetWaitingForPlayerChoice(true);
        Refresh();
        root.AddToClassList("shown");
        root.schedule.Execute(() => root.AddToClassList("visible")).StartingIn(30);
    }

    private void ClockIn()
    {
        if (!open) return;
        open = false;
        root.RemoveFromClassList("visible");
        root.schedule.Execute(() => root.RemoveFromClassList("shown")).StartingIn(300);

        // Weapons, stats and bonuses are picked from the current selection
        PlayerController.Instance?.GetComponent<CharacterLoadout>()?.ApplyLoadout();
        // One loading tip per shift, read it or don't
        if (LoreSystem.Instance != null) GameEvents.ShowNotification(LoreSystem.Instance.NextTip());
        Time.timeScale = 1f;
        RoomManager.Instance?.SetWaitingForPlayerChoice(false);
        GameEvents.ChangeGameState(GameState.Playing);
    }

    private void HandleRunEnded(bool victory)
    {
        // The run-end screen owns the screen first; the lobby is reached from its restart button.
        open = false;
    }

    private void Refresh()
    {
        var meta = MetaProgression.Instance;
        if (meta == null) return;
        scrapLabel.text = meta.Scrap.ToString();
        var save = SaveSystem.Data;
        records.text = $"BEST SECTOR {save.bestRoom}   -   BEST LEVEL {save.bestLevel}   -   SHIFTS WORKED {save.totalRuns}   -   VICTORIES {save.victories}";
        BuildCharacters(meta);
        BuildUpgrades(meta);
        BuildBestiary();
        // Rarely, the terminal shows its age
        if (Random.value < 0.05f) GlitchOnce();
    }

    private void GlitchOnce()
    {
        var header = root.Q<Label>(className: "section-title");
        if (header == null) return;
        string real = header.text;
        header.text = "DO NOT ENTER";
        header.schedule.Execute(() => header.text = real).StartingIn(2500);
    }

    private void BuildBestiary()
    {
        if (bestiaryList == null || LoreSystem.Instance == null) return;
        bestiaryList.Clear();
        int owned = 0;
        foreach (var entry in LoreSystem.Instance.Status())
        {
            if (entry.unlocked) owned++;
            var row = new VisualElement();
            row.AddToClassList("lobby-row");
            if (!entry.unlocked) row.AddToClassList("locked");
            var top = new VisualElement();
            top.AddToClassList("row-top");
            top.Add(MakeLabel(entry.unlocked ? entry.name.ToUpperInvariant() : "NO DATA", "row-name"));
            var tag = MakeLabel(entry.unlocked ? $"EXTERMINATED x{entry.kills}" : "EXTERMINATE SPECIMEN", "row-tag");
            tag.AddToClassList(entry.unlocked ? "owned" : "cant-afford");
            top.Add(tag);
            row.Add(top);
            row.Add(MakeLabel(entry.unlocked ? entry.text : "Field records sealed pending first confirmed kill.", "row-text"));
            bestiaryList.Add(row);
        }
        if (owned == 0) bestiaryList.Add(MakeLabel("NO CONFIRMED KILLS THIS QUARTER.", "row-text"));
    }

    private void BuildCharacters(MetaProgression meta)
    {
        characterList.Clear();
        var selected = meta.SelectedCharacter();
        foreach (var c in meta.Characters)
        {
            bool unlocked = meta.IsUnlocked(c);
            var row = new VisualElement();
            row.AddToClassList("lobby-row");
            if (!unlocked) row.AddToClassList("locked");
            if (selected == c) row.AddToClassList("selected");

            var top = new VisualElement();
            top.AddToClassList("row-top");
            top.Add(MakeLabel(c.displayName.ToUpperInvariant(), "row-name"));
            string tag = unlocked ? (selected == c ? "ASSIGNED" : "SELECT") : $"HIRE {c.unlockCost}";
            var tagLabel = MakeLabel(tag, "row-tag");
            if (unlocked) tagLabel.AddToClassList("owned");
            else tagLabel.AddToClassList(meta.Scrap >= c.unlockCost ? "cost" : "cant-afford");
            top.Add(tagLabel);
            row.Add(top);

            row.Add(MakeLabel(c.weaponName, "row-text"));
            row.Add(MakeLabel($"POWER - {c.powerName}: {c.powerText}", "row-power"));
            row.Add(MakeLabel($"CONSTRAINT - {c.constraintName}: {c.constraintText}", "row-constraint"));

            var captured = c;
            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (meta.IsUnlocked(captured)) meta.SelectCharacter(captured);
                else meta.Unlock(captured);
                Refresh();
            });
            characterList.Add(row);
        }
    }

    private void BuildUpgrades(MetaProgression meta)
    {
        metaList.Clear();
        foreach (var u in meta.Upgrades)
        {
            if (u == null) continue;
            int level = meta.LevelOf(u);
            bool maxed = level >= u.maxLevel;

            var row = new VisualElement();
            row.AddToClassList("lobby-row");

            var top = new VisualElement();
            top.AddToClassList("row-top");
            top.Add(MakeLabel(u.displayName.ToUpperInvariant(), "row-name"));
            string tag;
            string style;
            if (maxed) { tag = "MAXED"; style = "maxed"; }
            else
            {
                int cost = u.CostForLevel(level);
                tag = $"BUY {cost}";
                style = meta.Scrap >= cost ? "cost" : "cant-afford";
            }
            var tagLabel = MakeLabel(tag, "row-tag");
            tagLabel.AddToClassList(style);
            top.Add(tagLabel);
            row.Add(top);

            row.Add(MakeLabel(u.description, "row-text"));
            var pips = new VisualElement();
            pips.AddToClassList("level-pips");
            for (int i = 0; i < u.maxLevel; i++)
            {
                var pip = new VisualElement();
                pip.AddToClassList("pip");
                if (i < level) pip.AddToClassList("filled");
                pips.Add(pip);
            }
            row.Add(pips);

            var captured = u;
            row.RegisterCallback<ClickEvent>(_ =>
            {
                meta.Buy(captured);
                Refresh();
            });
            metaList.Add(row);
        }
    }

    private static Label MakeLabel(string text, string cls)
    {
        var label = new Label(text);
        label.AddToClassList(cls);
        label.pickingMode = PickingMode.Ignore;
        return label;
    }
}
