using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the current arena: tiled floor, boundary walls and cover. Rebuilt for every room and
/// owns nothing outside its own "Arena" object, so tearing it down is just destroying that root.
/// Sprites are assigned in the scene; missing ones fall back to flat colour so a room always works.
/// </summary>
public class RoomLayoutBuilder : MonoBehaviour
{
    [Header("Art")]
    [SerializeField] private Sprite floorSprite;
    [SerializeField] private Sprite wallSprite;
    [SerializeField] private Sprite crateSprite;
    [SerializeField] private Sprite pillarSprite;

    [Header("Scene")]
    [Tooltip("The authored tilemap grid, hidden while generated arenas are in use.")]
    [SerializeField] private GameObject legacyGrid;
    [SerializeField] private float wallThickness = 3f;

    private static RoomLayoutData defaultLayout;
    private Transform arenaRoot;
    private readonly List<GameObject> built = new List<GameObject>();

    public static RoomLayoutData DefaultLayout
    {
        get
        {
            if (defaultLayout == null)
            {
                defaultLayout = ScriptableObject.CreateInstance<RoomLayoutData>();
                defaultLayout.displayName = "Warehouse";
                defaultLayout.size = new Vector2(26f, 18f);
            }
            return defaultLayout;
        }
    }

    public Rect CurrentBounds { get; private set; }

    public void Build(RoomLayoutData layout, Transform player)
    {
        if (layout == null) layout = DefaultLayout;
        Clear();
        if (legacyGrid == null) legacyGrid = GameObject.Find("Grid");
        if (legacyGrid != null) legacyGrid.SetActive(false);

        arenaRoot = new GameObject("Arena").transform;
        CurrentBounds = layout.Bounds;

        BuildFloor(layout);
        BuildWalls(layout);

        var obstacles = layout.obstacles;
        if (obstacles == null || obstacles.Count == 0)
        {
            obstacles = ScatterCover(layout);
        }
        foreach (var o in obstacles) BuildObstacle(o);

        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Move the body, not just the transform: with interpolation the physics
                // state would otherwise pull the player back to its old position.
                rb.position = layout.playerStart;
                rb.linearVelocity = Vector2.zero;
            }
            player.position = layout.playerStart;
            Physics2D.SyncTransforms();

            var clamp = player.GetComponent<ArenaClamp>();
            if (clamp == null) clamp = player.gameObject.AddComponent<ArenaClamp>();
            clamp.SetBounds(layout.Bounds);
        }
    }

    public void Clear()
    {
        if (arenaRoot != null) Destroy(arenaRoot.gameObject);
        arenaRoot = null;
        built.Clear();
    }

    void OnDestroy()
    {
        Clear();
    }

    // ========================================
    // PIECES
    // ========================================

    private void BuildFloor(RoomLayoutData layout)
    {
        var go = new GameObject("Floor");
        go.transform.SetParent(arenaRoot, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = floorSprite != null ? floorSprite : FlatSprite();
        if (floorSprite == null) sr.color = new Color(0.36f, 0.35f, 0.34f);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = layout.size;
        sr.sortingOrder = -20;
        go.transform.position = Vector3.zero;
    }

    private void BuildWalls(RoomLayoutData layout)
    {
        float hw = layout.size.x * 0.5f, hh = layout.size.y * 0.5f, t = wallThickness;
        MakeWall("Wall N", new Vector2(0f, hh + t * 0.5f), new Vector2(layout.size.x + t * 2f, t));
        MakeWall("Wall S", new Vector2(0f, -hh - t * 0.5f), new Vector2(layout.size.x + t * 2f, t));
        MakeWall("Wall E", new Vector2(hw + t * 0.5f, 0f), new Vector2(t, layout.size.y));
        MakeWall("Wall W", new Vector2(-hw - t * 0.5f, 0f), new Vector2(t, layout.size.y));
    }

    private void MakeWall(string name, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(arenaRoot, false);
        go.transform.position = center;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = wallSprite != null ? wallSprite : FlatSprite();
        if (wallSprite == null) sr.color = new Color(0.22f, 0.22f, 0.24f);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.sortingOrder = -5;
    }

    private void BuildObstacle(ObstacleSpec spec)
    {
        var go = new GameObject(spec.kind == ObstacleKind.Pillar ? "Pillar" : "Crate");
        go.transform.SetParent(arenaRoot, false);
        go.transform.position = spec.center;
        var sprite = spec.kind == ObstacleKind.Pillar ? pillarSprite : crateSprite;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : FlatSprite();
        if (sprite == null) sr.color = new Color(0.5f, 0.35f, 0.2f);
        // Scale a simple sprite to the footprint (no 9-slice borders on these tiles).
        var bounds = sr.sprite.bounds.size;
        go.transform.localScale = new Vector3(spec.size.x / bounds.x, spec.size.y / bounds.y, 1f);
        sr.sortingOrder = 0;
        // Colliders are sized in sprite-local space, so undo the scale applied above.
        if (spec.kind == ObstacleKind.Pillar)
        {
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = bounds.x * 0.5f;
        }
        else
        {
            go.AddComponent<BoxCollider2D>().size = bounds;
        }
    }

    /// <summary>Random cover that keeps the player's start and the arena centre lanes clear.</summary>
    private List<ObstacleSpec> ScatterCover(RoomLayoutData layout)
    {
        var list = new List<ObstacleSpec>();
        int count = layout.bossArena ? 4 : Random.Range(4, 8);
        float margin = 3f;
        for (int i = 0; i < count; i++)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                var size = Random.value < 0.35f ? new Vector2(1.6f, 1.6f) : new Vector2(2f, 2f);
                var p = new Vector2(
                    Random.Range(-layout.size.x * 0.5f + margin, layout.size.x * 0.5f - margin),
                    Random.Range(-layout.size.y * 0.5f + margin, layout.size.y * 0.5f - margin));
                if ((p - layout.playerStart).magnitude < 3.5f) continue;
                bool overlaps = false;
                foreach (var other in list)
                {
                    if ((other.center - p).magnitude < 3.2f) { overlaps = true; break; }
                }
                if (overlaps) continue;
                list.Add(new ObstacleSpec
                {
                    center = p,
                    size = size,
                    kind = size.x < 1.8f ? ObstacleKind.Pillar : ObstacleKind.Crate,
                });
                break;
            }
        }
        return list;
    }

    private static Sprite flat;
    private static Sprite FlatSprite()
    {
        if (flat == null)
        {
            var tex = new Texture2D(2, 2) { filterMode = FilterMode.Point };
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            flat = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        }
        return flat;
    }
}
