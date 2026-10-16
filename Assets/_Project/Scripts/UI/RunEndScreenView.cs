using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// View for the end-of-run report. Tallies kills, waves and upgrades from events,
/// then shows them when the run ends. Restarting raises GameEvents.StartRun, as GameOverUI does.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class RunEndScreenView : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private int kills;
    private int wavesCleared;
    private int upgradesTaken;
    private VisualElement root;
    private Button restart;
    private Button endless;
    private Label scrapLine;

    void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        GameEvents.OnEnemyKilled += HandleKill;
        GameEvents.OnWaveCleared += HandleWaveCleared;
        GameEvents.OnPlayerUpgradeApplied += HandleUpgrade;
        GameEvents.OnRunEnded += Show;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyKilled -= HandleKill;
        GameEvents.OnWaveCleared -= HandleWaveCleared;
        GameEvents.OnPlayerUpgradeApplied -= HandleUpgrade;
        GameEvents.OnRunEnded -= Show;
        if (restart != null) restart.clicked -= Restart;
        if (endless != null) endless.clicked -= GoEndless;
    }

    private void HandleKill(Enemy enemy) => kills++;
    private void HandleWaveCleared(int wave) => wavesCleared++;
    private void HandleUpgrade(UpgradeData upgrade) => upgradesTaken++;

    private void Show(bool victory)
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return;
        root = uiDocument.rootVisualElement.Q<VisualElement>("runend-root");
        if (root == null) return;

        HazardTape.Fill(root.Q<VisualElement>("runend-tape-top"));
        HazardTape.Fill(root.Q<VisualElement>("runend-tape-bottom"));

        var sheet = root.Q<VisualElement>("runend-sheet");
        sheet.EnableInClassList("victory", victory);
        root.Q<Label>("runend-form").text = victory ? "FORM 11-D  /  COMMENDATION" : "FORM 11-C  /  INCIDENT REPORT";
        root.Q<Label>("runend-title").text = victory ? "SECTOR SECURED" : "CONTRACT TERMINATED";
        string[] endings = {
            "CONTRACTOR #0447 UNRECOVERED.",
            "CONTRACTOR #0447 UNRECOVERED. EQUIPMENT UNRECOVERED.",
            "CONTRACTOR #0447 UNRECOVERED. NEXT CONTRACTOR BRIEFED.",
        };
        string[] glitches = {
            "CONTRACTOR #0446 UNRECOVERED. WAIT. THAT NUMBER IS TAKEN.",
            "CONTRACTOR #0447 RECOVERED. [CORRECTION] UNRECOVERED.",
            "THANK YOU FOR YOUR SERVICE. WE ARE SORRY. WE ARE SORRY. WE ARE SORRY.",
        };
        bool glitch = !victory && Random.value < 0.06f;
        root.Q<Label>("runend-subtitle").text = victory
            ? "MANAGEMENT HAS NOTED YOUR EFFORT. NO BONUS WILL BE ISSUED."
            : glitch ? glitches[Random.Range(0, glitches.Length)] : endings[Random.Range(0, endings.Length)];

        int level = XPManager.Instance != null ? XPManager.Instance.GetCurrentLevel() : 1;
        root.Q<Label>("report-level").text = level.ToString();
        root.Q<Label>("report-waves").text = wavesCleared.ToString();
        root.Q<Label>("report-kills").text = kills.ToString();
        root.Q<Label>("report-upgrades").text = upgradesTaken.ToString();

        restart = root.Q<Button>("runend-restart");
        restart.clicked -= Restart;
        restart.clicked += Restart;

        // Winning unlocks endless mode: keep playing past the last room for more scrap.
        endless = root.Q<Button>("runend-endless");
        endless.clicked -= GoEndless;
        endless.clicked += GoEndless;
        bool canContinue = victory && RoomManager.Instance != null && RoomManager.Instance.IsActive;
        endless.style.display = canContinue ? DisplayStyle.Flex : DisplayStyle.None;

        scrapLine = root.Q<Label>("report-scrap");
        int earned = MetaProgression.Instance != null ? MetaProgression.Instance.RunScrap : 0;
        scrapLine.text = earned.ToString();

        root.AddToClassList("shown");
        root.schedule.Execute(() => root.AddToClassList("visible")).StartingIn(30);
    }

    private void GoEndless()
    {
        if (RoomManager.Instance == null) return;
        root.RemoveFromClassList("visible");
        var target = root;
        target.schedule.Execute(() => target.RemoveFromClassList("shown")).StartingIn(300);
        RoomManager.Instance.ContinueEndless();
    }

    private void Restart()
    {
        GameEvents.StartRun();
    }
}
