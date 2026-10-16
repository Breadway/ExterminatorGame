using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// The branching map. Shows the next-sector options from RoomManager and reports the pick back
/// through RoomManager.ChooseMap. Owns no game state.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MapScreenView : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private VisualElement row;
    private Label route;
    private IVisualElementScheduledItem hideItem;

    void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        GameEvents.OnMapOffered += Show;
        GameEvents.OnGameStateChanged += HandleStateChanged;
        GameEvents.OnRunEnded += HandleRunEnded;
    }

    void OnDisable()
    {
        GameEvents.OnMapOffered -= Show;
        GameEvents.OnGameStateChanged -= HandleStateChanged;
        GameEvents.OnRunEnded -= HandleRunEnded;
        hideItem?.Pause();
    }

    void Update()
    {
        var manager = RoomManager.Instance;
        if (manager == null || !manager.IsChoosingMap || Keyboard.current == null) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame) manager.ChooseMap(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) manager.ChooseMap(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) manager.ChooseMap(2);
    }

    private bool Bind()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return false;
        root = uiDocument.rootVisualElement.Q<VisualElement>("map-root");
        if (root == null) return false;
        row = root.Q<VisualElement>("map-row");
        route = root.Q<Label>("map-route");
        HazardTape.Fill(root.Q<VisualElement>("map-tape-top"));
        HazardTape.Fill(root.Q<VisualElement>("map-tape-bottom"));
        return row != null;
    }

    private void Show(int nextRoom, RoomDefinition[] options)
    {
        if (options == null || options.Length == 0 || !Bind()) return;
        hideItem?.Pause();
        route.text = $"ROUTE PLANNING  /  SECTOR {nextRoom + 1} APPROACHES";
        row.Clear();
        for (int i = 0; i < options.Length; i++) row.Add(BuildCard(options[i], i));
        root.AddToClassList("shown");
        root.schedule.Execute(() => root.AddToClassList("visible")).StartingIn(30);
        GameEvents.PlaySound("room_start");
    }

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.MapChoice) Hide();
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

    private VisualElement BuildCard(RoomDefinition room, int index)
    {
        var card = new VisualElement();
        card.AddToClassList("map-card");
        card.Add(MakeLabel($"[{index + 1}]  {room.roomName}", "map-card-head"));
        var type = MakeLabel(TypeName(room.type), "map-card-type");
        type.AddToClassList("map-type-" + room.type.ToString().ToLowerInvariant());
        card.Add(type);
        card.Add(MakeLabel(Describe(room), "map-card-desc"));
        int captured = index;
        card.RegisterCallback<ClickEvent>(_ => RoomManager.Instance?.ChooseMap(captured));
        card.RegisterCallback<PointerEnterEvent>(_ => GameEvents.PlaySound("ui_hover"));
        return card;
    }

    private static string TypeName(RoomType type)
    {
        switch (type)
        {
            case RoomType.Elite: return "HIGH RISK";
            case RoomType.Unknown: return "UNCLASSIFIED";
            case RoomType.Shop: return "BREAK ROOM";
            case RoomType.Boss: return "CONTAINMENT BREACH";
            default: return "STANDARD SWEEP";
        }
    }

    private static string Describe(RoomDefinition room)
    {
        switch (room.type)
        {
            case RoomType.Elite: return "Hardened hostiles. +60% XP. Survivors are patched up.";
            case RoomType.Shop: return "No hostiles. Requisitions counter open. Recover and resupply.";
            case RoomType.Unknown:
                string ev = RoomEventDirector.Label(room.eventId);
                return string.IsNullOrEmpty(ev) ? "Unclassified conditions. Expect anything." : ev + ". Expect anything.";
            default:
                string mod = RoomModifiers.Label(room.modifier);
                return string.IsNullOrEmpty(mod) ? "Standard extermination sweep." : "Standard sweep. " + mod + ".";
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
