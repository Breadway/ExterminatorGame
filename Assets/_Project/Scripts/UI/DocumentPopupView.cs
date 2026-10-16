using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Shows a recovered lore document. Pausing is owned by LoreSystem (timeScale 0, Event state);
/// closing returns to Playing. Reads only from LoreSystem.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class DocumentPopupView : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private Button close;

    void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        GameEvents.OnDocumentShown += Show;
    }

    void OnDisable()
    {
        GameEvents.OnDocumentShown -= Show;
        if (close != null) close.clicked -= Close;
    }

    private bool Bind()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null) return false;
        root = uiDocument.rootVisualElement.Q<VisualElement>("document-root");
        if (root == null) return false;
        close = root.Q<Button>("document-close");
        HazardTape.Fill(root.Q<VisualElement>("document-tape-top"));
        HazardTape.Fill(root.Q<VisualElement>("document-tape-bottom"));
        close.clicked -= Close;
        close.clicked += Close;
        return true;
    }

    private void Show(string id)
    {
        var doc = LoreSystem.GetDocument(id);
        if (doc == null || !Bind()) { Close(); return; }
        root.Q<Label>("document-title").text = doc.Value.title;
        root.Q<Label>("document-text").text = doc.Value.body;
        GameEvents.PlaySound("ui_click");
        root.AddToClassList("shown");
        root.schedule.Execute(() => root.AddToClassList("visible")).StartingIn(30);
    }

    private void Close()
    {
        if (root != null)
        {
            root.RemoveFromClassList("visible");
            var target = root;
            target.schedule.Execute(() => target.RemoveFromClassList("shown")).StartingIn(250);
        }
        Time.timeScale = 1f;
        GameEvents.ChangeGameState(GameState.Playing);
        GameEvents.DocumentClosed();
    }
}
