using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Shows the shop. It only reads ShopRoom and calls Buy/Reroll/Close; it keeps no state.
/// Cards are rebuilt after every action so prices and sold states are always current.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class ShopScreenView : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private VisualElement shelf;
    private Label funds;
    private Button reroll;
    private Button leave;

    void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        GameEvents.OnShopToggled += HandleToggled;
        GameEvents.OnScrapChanged += HandleScrap;
    }

    void OnDisable()
    {
        GameEvents.OnShopToggled -= HandleToggled;
        GameEvents.OnScrapChanged -= HandleScrap;
        if (reroll != null) reroll.clicked -= Reroll;
        if (leave != null) leave.clicked -= Leave;
    }

    private bool Bind()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return false;
        root = uiDocument.rootVisualElement.Q<VisualElement>("shop-root");
        if (root == null) return false;
        shelf = root.Q<VisualElement>("shop-shelf");
        funds = root.Q<Label>("shop-funds");
        reroll = root.Q<Button>("shop-reroll");
        leave = root.Q<Button>("shop-leave");
        HazardTape.Fill(root.Q<VisualElement>("shop-tape-top"));
        HazardTape.Fill(root.Q<VisualElement>("shop-tape-bottom"));
        reroll.clicked -= Reroll; reroll.clicked += Reroll;
        leave.clicked -= Leave; leave.clicked += Leave;
        return shelf != null;
    }

    private void HandleToggled(bool open)
    {
        if (open)
        {
            if (!Bind()) return;
            Refresh();
            root.AddToClassList("shown");
            root.schedule.Execute(() => root.AddToClassList("visible")).StartingIn(30);
        }
        else if (root != null)
        {
            root.RemoveFromClassList("visible");
            var target = root;
            target.schedule.Execute(() => target.RemoveFromClassList("shown")).StartingIn(300);
        }
    }

    private void HandleScrap(int total, int delta)
    {
        if (ShopRoom.Instance != null && ShopRoom.Instance.IsOpen) Refresh();
    }

    private void Reroll() { if (ShopRoom.Instance != null && ShopRoom.Instance.Reroll()) Refresh(); }
    private void Leave() { GameEvents.PlaySound("ui_click"); ShopRoom.Instance?.Close(); }

    private void Refresh()
    {
        var shop = ShopRoom.Instance;
        if (shop == null || root == null) return;
        funds.text = shop.Funds.ToString();
        reroll.text = $"[ RESTOCK {shop.RerollPrice} ]";
        shelf.Clear();
        for (int i = 0; i < shop.Stock.Count; i++) shelf.Add(BuildCard(shop, shop.Stock[i], i));
    }

    private VisualElement BuildCard(ShopRoom shop, ShopRoom.Offer offer, int index)
    {
        var card = new VisualElement();
        card.AddToClassList("upgrade-card");
        if (offer.sold) card.AddToClassList("sold");

        var strip = new VisualElement();
        strip.AddToClassList("card-strip");
        card.Add(strip);

        if (offer.isRepair)
        {
            card.AddToClassList("repair-card");
            var head = new VisualElement();
            head.AddToClassList("card-head");
            head.Add(Label("SERVICE", "card-rarity"));
            card.Add(head);
            card.Add(Label("FIELD REPAIR", "card-name"));
            card.Add(Label("A medic you have never met patches you up. No questions. Do not ask any either.", "card-desc"));
            card.Add(Label("+", "repair-icon"));
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            card.Add(spacer);
        }
        else
        {
            var u = offer.upgrade;
            card.AddToClassList("rarity-" + u.rarity.ToString().ToLowerInvariant());
            var head = new VisualElement();
            head.AddToClassList("card-head");
            head.Add(Label(u.rarity.ToString().ToUpperInvariant(), "card-rarity"));
            var type = Label(u.type.ToString().ToUpperInvariant(), "card-type");
            type.AddToClassList("card-type-" + u.type.ToString().ToLowerInvariant());
            head.Add(type);
            card.Add(head);

            string title = string.IsNullOrWhiteSpace(u.upgradeName) ? u.name : u.upgradeName;
            card.Add(Label(title.ToUpperInvariant(), "card-name"));
            card.Add(Label(u.description, "card-desc"));
            var stats = new VisualElement();
            stats.AddToClassList("card-stats");
            foreach (var line in UpgradeStatText.Describe(u))
                stats.Add(Label(line.text, line.beneficial ? "stat-line stat-up" : "stat-line stat-down"));
            if (u.hasSpecialEffect && !string.IsNullOrWhiteSpace(u.specialEffectDescription))
                stats.Add(Label(u.specialEffectDescription, "stat-special"));
            card.Add(stats);
        }

        bool affordable = shop.CanAfford(offer.price);
        var tag = Label(offer.sold ? "SOLD" : $"{offer.price} SCRAP", "price-tag");
        if (!offer.sold && !affordable) tag.AddToClassList("cant-afford");
        card.Add(tag);

        int captured = index;
        card.RegisterCallback<ClickEvent>(_ => { if (shop.Buy(captured)) Refresh(); });
        card.RegisterCallback<PointerEnterEvent>(_ => GameEvents.PlaySound("ui_hover"));
        return card;
    }

    private static Label Label(string text, string classes)
    {
        var label = new Label(text);
        foreach (var c in classes.Split(' ')) label.AddToClassList(c);
        label.pickingMode = PickingMode.Ignore;
        return label;
    }
}
