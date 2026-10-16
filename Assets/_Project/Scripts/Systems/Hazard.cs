using System.Collections.Generic;
using UnityEngine;

public enum HazardKind
{
    Fuel,   // harmless puddle that ignites when lit
    Fire,   // burns anything standing in it
    Web,    // slows whatever walks through it
    Acid,   // burns and corrodes
    Shock   // electrified floor (room hazard)
}

/// <summary>
/// A ground patch that affects creatures inside it. One class serves every floor effect: fuel
/// puddles, surface fire, webs, acid pools and electrified plates. Patches are pooled by
/// HazardField, live for a fixed time, and can ignite each other (fire spreads through fuel).
/// Effects are applied on a timer rather than per frame so 100 patches stay cheap.
/// </summary>
public class Hazard : MonoBehaviour
{
    public HazardKind Kind { get; private set; }
    public float Radius { get; private set; }
    public bool HurtsPlayer { get; private set; }
    public bool HurtsEnemies { get; private set; }
    public bool Alive { get; private set; }

    private float dieAt;
    private float tickInterval = 0.3f;
    private float nextTick;
    private float damage;
    private float slow = 1f;
    private float igniteAt = -1f;
    private SpriteRenderer sr;
    private static readonly Collider2D[] hits = new Collider2D[32];
    private static int enemyMask = -1, playerMask = -1;
    private static Sprite disc;
    private System.Action<Hazard> onDie;

    public static Sprite Disc
    {
        get
        {
            if (disc == null) disc = MakeDisc();
            return disc;
        }
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (enemyMask < 0) enemyMask = 1 << LayerMask.NameToLayer("Enemy");
        if (playerMask < 0) playerMask = 1 << LayerMask.NameToLayer("Player");
    }

    public void Begin(HazardKind kind, Vector2 position, float radius, float lifetime, float damagePerSecond,
                      bool hurtsPlayer, bool hurtsEnemies, float slowMultiplier, System.Action<Hazard> dieCallback)
    {
        Kind = kind;
        Radius = radius;
        HurtsPlayer = hurtsPlayer;
        HurtsEnemies = hurtsEnemies;
        damage = damagePerSecond;
        slow = slowMultiplier;
        onDie = dieCallback;
        transform.position = position;
        dieAt = Time.time + lifetime;
        nextTick = Time.time + Random.Range(0f, tickInterval);
        igniteAt = -1f;
        Alive = true;
        gameObject.SetActive(true);
        Restyle();
    }

    /// <summary>Light a fuel puddle. It burns after a short fuse so a chain reaction spreads visibly.</summary>
    public void Ignite(float delay, float fireSeconds, float fireDps)
    {
        if (Kind != HazardKind.Fuel || igniteAt >= 0f) return;
        igniteAt = Time.time + delay;
        fireLife = fireSeconds;
        fireDamage = fireDps;
    }

    private float fireLife, fireDamage;

    void Update()
    {
        if (!Alive || Time.timeScale <= 0f) return;

        if (igniteAt >= 0f && Time.time >= igniteAt)
        {
            HazardField.Instance?.Convert(this, HazardKind.Fire, fireLife, fireDamage);
            return;
        }
        if (Time.time >= dieAt) { Kill(); return; }

        if (sr != null)
        {
            float remaining = dieAt - Time.time;
            var c = sr.color;
            c.a = Mathf.Clamp01(remaining / 1.2f) * BaseAlpha();
            if (Kind == HazardKind.Fire) c.a *= 0.8f + 0.2f * Mathf.Sin(Time.time * 18f + transform.position.x);
            sr.color = c;
        }

        if (Time.time >= nextTick)
        {
            nextTick = Time.time + tickInterval;
            Tick();
        }
    }

    private void Tick()
    {
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = (HurtsEnemies ? enemyMask : 0) | (HurtsPlayer ? playerMask : 0), useTriggers = true };
        int n = Physics2D.OverlapCircle(transform.position, Radius, filter, hits);
        for (int i = 0; i < n; i++)
        {
            var col = hits[i];
            if (col == null) continue;
            var health = col.GetComponentInParent<Health>();
            if (health == null || !health.IsAlive) continue;

            if (damage > 0f) health.TakeDamage(damage * tickInterval, col.transform.position, Vector2.zero);

            if (slow < 1f)
            {
                var player = col.GetComponentInParent<PlayerMovement>();
                if (player != null) player.ApplyTimedSlow(slow, tickInterval * 2.5f);
                var enemy = col.GetComponentInParent<Enemy>();
                if (enemy != null) enemy.ApplyHazardSlow(slow, tickInterval * 2.5f);
            }

            // Fire lights nearby fuel, so a trail burns end to end
            if (Kind == HazardKind.Fire) HazardField.Instance?.IgniteNear(transform.position, Radius + 0.4f);
        }
        if (Kind == HazardKind.Fire) HazardField.Instance?.IgniteNear(transform.position, Radius + 0.5f);
    }

    private void Kill()
    {
        if (!Alive) return;
        Alive = false;
        gameObject.SetActive(false);
        onDie?.Invoke(this);
    }

    public void Clear() { Kill(); }

    private float BaseAlpha()
    {
        switch (Kind)
        {
            case HazardKind.Fuel: return 0.55f;
            case HazardKind.Fire: return 0.9f;
            case HazardKind.Web: return 0.65f;
            case HazardKind.Acid: return 0.7f;
            default: return 0.6f;
        }
    }

    private void Restyle()
    {
        if (sr == null) return;
        sr.sprite = Disc;
        sr.sortingOrder = -10;
        sr.color = ColorFor(Kind);
        transform.localScale = Vector3.one * Radius * 2f;
    }

    public static Color ColorFor(HazardKind kind)
    {
        switch (kind)
        {
            case HazardKind.Fuel: return new Color(0.25f, 0.2f, 0.12f, 0.55f);
            case HazardKind.Fire: return new Color(1f, 0.5f, 0.1f, 0.9f);
            case HazardKind.Web: return new Color(0.9f, 0.9f, 0.95f, 0.65f);
            case HazardKind.Acid: return new Color(0.45f, 0.95f, 0.2f, 0.7f);
            default: return new Color(0.5f, 0.85f, 1f, 0.6f);
        }
    }

    private static Sprite MakeDisc()
    {
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                tex.SetPixel(x, y, d <= 1f ? new Color(1f, 1f, 1f, d > 0.82f ? 0.6f : 1f) : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }
}

/// <summary>
/// Spawns and pools hazards. Everything that wants a floor effect asks this, so counts are capped in
/// one place and a room change can clear them all.
/// </summary>
public class HazardField : MonoBehaviour
{
    public static HazardField Instance { get; private set; }

    [SerializeField] private int maxHazards = 120;

    private readonly List<Hazard> active = new List<Hazard>();
    private readonly Stack<Hazard> pool = new Stack<Hazard>();
    public int ActiveCount => active.Count;
    public IReadOnlyList<Hazard> Active => active;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable() { GameEvents.OnRoomCleared += HandleRoomCleared; GameEvents.OnRunEnded += HandleRunEnded; }
    void OnDisable() { GameEvents.OnRoomCleared -= HandleRoomCleared; GameEvents.OnRunEnded -= HandleRunEnded; }

    // Floor effects belong to a room; leaving one clears them so fire never carries into the next arena
    private void HandleRoomCleared(int room) { ClearAll(); }
    private void HandleRunEnded(bool victory) { ClearAll(); }

    public Hazard Spawn(HazardKind kind, Vector2 position, float radius, float lifetime, float dps, bool hurtsPlayer, bool hurtsEnemies, float slow = 1f)
    {
        if (active.Count >= maxHazards)
        {
            // Full: recycle the oldest so new fire always appears instead of silently failing
            active[0].Clear();
            if (active.Count >= maxHazards) return null;
        }
        Hazard h = pool.Count > 0 ? pool.Pop() : NewHazard();
        active.Add(h);
        h.Begin(kind, position, radius, lifetime, dps, hurtsPlayer, hurtsEnemies, slow, Recycle);
        return h;
    }

    private Hazard NewHazard()
    {
        var go = new GameObject("Hazard");
        go.transform.SetParent(transform, false);
        go.AddComponent<SpriteRenderer>();
        return go.AddComponent<Hazard>();
    }

    private void Recycle(Hazard h)
    {
        active.Remove(h);
        pool.Push(h);
    }

    /// <summary>Replace a patch with another kind in place (fuel becoming fire).</summary>
    public void Convert(Hazard source, HazardKind kind, float lifetime, float dps)
    {
        Vector2 pos = source.transform.position;
        float radius = source.Radius;
        bool player = source.HurtsPlayer, enemies = source.HurtsEnemies;
        source.Clear();
        Spawn(kind, pos, radius * 1.1f, lifetime, dps, player, enemies);
    }

    public void IgniteNear(Vector2 position, float radius)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var h = active[i];
            if (h.Kind != HazardKind.Fuel) continue;
            if (((Vector2)h.transform.position - position).sqrMagnitude <= (radius + h.Radius) * (radius + h.Radius))
                h.Ignite(0.12f, 8f, FireDps);
        }
    }

    public float FireDps = 7f;

    public void ClearAll()
    {
        for (int i = active.Count - 1; i >= 0; i--) active[i].Clear();
    }

    public int CountOf(HazardKind kind)
    {
        int n = 0;
        foreach (var h in active) if (h.Kind == kind) n++;
        return n;
    }
}
