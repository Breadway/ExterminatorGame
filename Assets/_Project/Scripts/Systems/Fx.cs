using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pooled visual effects without any art assets: hit sparks, death bursts, floating damage numbers,
/// muzzle flashes and shockwave rings. Particles are tiny runtime-generated sprites drawn by a
/// single pooled ParticleSystem per kind, so 100 enemies dying costs a few draw calls.
/// Listens to GameEvents, so gameplay code never references it.
/// </summary>
public class Fx : MonoBehaviour
{
    public static Fx Instance { get; private set; }

    [Header("Damage numbers")]
    [SerializeField] private int numberPool = 24;
    [SerializeField] private float numberLife = 0.7f;

    [Header("Particles")]
    [SerializeField] private int maxParticles = 600;

    private ParticleSystem sparks, bits, rings;
    private readonly Queue<FloatingNumber> numbers = new Queue<FloatingNumber>();
    private Material particleMaterial;
    private Sprite dot, ring;
    private PlayerController player;

    // ---------------- lifecycle ----------------
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        BuildSprites();
        sparks = BuildSystem("Sparks", dot, 0.18f, 0.35f, 3f, 9f, 0.07f, 0.16f);
        bits = BuildSystem("Bits", dot, 0.35f, 0.7f, 1.5f, 6f, 0.08f, 0.2f);
        rings = BuildSystem("Rings", ring, 0.22f, 0.22f, 0f, 0f, 0.4f, 0.4f);
        for (int i = 0; i < numberPool; i++) numbers.Enqueue(MakeNumber());
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        GameEvents.OnEnemyHit += HandleEnemyHit;
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
        GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
        GameEvents.OnPlayerHealed += HandleHealed;
        GameEvents.OnPlayerDash += HandleDash;
        GameEvents.OnWeaponFired += HandleWeaponFired;
        GameEvents.OnLevelUp += HandleLevelUp;
        GameEvents.OnRoomCleared += HandleRoomCleared;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyHit -= HandleEnemyHit;
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
        GameEvents.OnPlayerHealed -= HandleHealed;
        GameEvents.OnPlayerDash -= HandleDash;
        GameEvents.OnWeaponFired -= HandleWeaponFired;
        GameEvents.OnLevelUp -= HandleLevelUp;
        GameEvents.OnRoomCleared -= HandleRoomCleared;
    }

    // ---------------- event handlers ----------------
    private void HandleEnemyHit(Enemy enemy, float amount)
    {
        if (enemy == null) return;
        Vector2 pos = enemy.transform.position;
        Burst(sparks, pos, 4, new Color(1f, 0.85f, 0.3f), 0.12f);
        if (SaveSystem.Data.damageNumbers) Number(pos + Vector2.up * 0.5f, amount, amount >= 18f ? new Color(1f, 0.55f, 0.1f) : Color.white, amount >= 18f ? 1.25f : 1f);
        if (enemy.IsBoss) GameEvents.ScreenShake(0.04f);
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        if (enemy == null) return;
        Vector2 pos = enemy.transform.position;
        Color blood = enemy.EnemyData != null && enemy.EnemyData.enemyName == "Wasp" ? new Color(0.9f, 0.8f, 0.2f) : new Color(0.45f, 0.85f, 0.25f);
        int count = enemy.IsBoss ? 40 : 14;
        Burst(bits, pos, count, blood, enemy.IsBoss ? 0.5f : 0.22f);
        Burst(sparks, pos, enemy.IsBoss ? 24 : 6, Color.white, 0.2f);
        Ring(pos, enemy.IsBoss ? 3.2f : 0.9f, new Color(1f, 1f, 1f, 0.7f));
        if (enemy.IsBoss)
        {
            GameEvents.ScreenShake(0.9f);
            GameEvents.HitStop(0.12f, 0.02f);
            GameEvents.ZoomPunch(1f);
        }
        else
        {
            GameEvents.HitStop(0.02f, 0.1f);
        }
    }

    private void HandlePlayerDamaged(float amount, float hp, float max)
    {
        var p = Player();
        if (p == null) return;
        Burst(bits, p.position, 10, new Color(0.9f, 0.2f, 0.15f), 0.25f);
        Ring(p.position, 1.4f, new Color(0.95f, 0.25f, 0.2f, 0.8f));
        GameEvents.ScreenShake(Mathf.Clamp01(0.25f + amount * 0.02f));
        GameEvents.HitStop(0.06f, 0.05f);
        GameEvents.ZoomPunch(0.5f);
    }

    private void HandleHealed(float amount)
    {
        var p = Player();
        if (p == null) return;
        Burst(sparks, p.position + Vector3.up * 0.3f, 10, new Color(0.35f, 1f, 0.5f), 0.4f);
        Number(p.position + Vector3.up * 1.2f, amount, new Color(0.4f, 1f, 0.55f), 1.1f, "+");
    }

    private void HandleDash(Vector2 dir)
    {
        var p = Player();
        if (p == null) return;
        Ring(p.position, 1.3f, new Color(1f, 1f, 1f, 0.5f));
        Burst(sparks, p.position - (Vector3)(dir * 0.4f), 8, new Color(1f, 0.95f, 0.7f), 0.15f);
    }

    private void HandleWeaponFired(string id, Vector2 origin, Vector2 dir, float power)
    {
        switch (id)
        {
            case "mop":
                Ring(origin + dir * 1.3f, 2.2f, new Color(1f, 1f, 1f, 0.45f));
                break;
            case "beam":
                if (power >= 0.99f)
                {
                    Burst(sparks, origin, 28, new Color(0.5f, 1f, 0.9f), 0.5f);
                    Ring(origin, 2.4f, new Color(0.5f, 1f, 0.9f, 0.8f));
                    GameEvents.ScreenShake(0.5f);
                    GameEvents.ZoomPunch(0.7f);
                }
                break;
            case "cables":
                Burst(sparks, origin + dir * 0.4f, 5, new Color(0.7f, 0.9f, 1f), 0.12f);
                break;
        }
    }

    private void HandleLevelUp(int level)
    {
        var p = Player();
        if (p == null) return;
        Ring(p.position, 3.6f, new Color(1f, 0.82f, 0.1f, 0.9f));
        Burst(sparks, p.position, 30, new Color(1f, 0.85f, 0.2f), 0.6f);
    }

    private void HandleRoomCleared(int room)
    {
        var p = Player();
        if (p == null) return;
        Ring(p.position, 5f, new Color(0.35f, 1f, 0.55f, 0.7f));
    }

    private Transform Player()
    {
        if (player == null) player = PlayerController.Instance;
        return player != null ? player.transform : null;
    }

    // ---------------- public helpers ----------------
    public void Spark(Vector2 position, int count, Color color, float spread = 0.15f) { Burst(sparks, position, count, color, spread); }
    public void Shockwave(Vector2 position, float size, Color color) { Ring(position, size, color); }

    // ---------------- particle plumbing ----------------
    private void BuildSprites()
    {
        dot = MakeSprite(8, false);
        ring = MakeSprite(48, true);
        var shader = Shader.Find("Sprites/Default");
        particleMaterial = new Material(shader);
    }

    private static Sprite MakeSprite(int size, bool hollow)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                bool on = hollow ? d <= 1f && d >= 0.82f : d <= 1f;
                tex.SetPixel(x, y, on ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private ParticleSystem BuildSystem(string name, Sprite sprite, float lifeMin, float lifeMax, float speedMin, float speedMax, float sizeMin, float sizeMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.gravityModifier = 0f;
        main.useUnscaledTime = true;

        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, name == "Rings" ? 3.2f : 0f)));
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                     new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0, 1) });
        col.color = grad;
        var vol = ps.velocityOverLifetime;
        vol.enabled = false;
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = name != "Rings";
        limit.dampen = 0.18f;
        limit.limit = 0.1f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMaterial;
        renderer.sortingOrder = 30;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        var tex = ps.textureSheetAnimation;
        tex.enabled = true;
        tex.mode = ParticleSystemAnimationMode.Sprites;
        tex.AddSprite(sprite);
        return ps;
    }

    private void Burst(ParticleSystem ps, Vector2 position, int count, Color color, float radius)
    {
        if (ps == null || count <= 0) return;
        var emit = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            emit.position = position + dir * Random.Range(0f, radius);
            emit.velocity = dir * Random.Range(ps.main.startSpeed.constantMin, ps.main.startSpeed.constantMax);
            emit.startColor = Color.Lerp(color, Color.white, Random.Range(0f, 0.35f));
            emit.startSize = Random.Range(ps.main.startSize.constantMin, ps.main.startSize.constantMax);
            emit.startLifetime = Random.Range(ps.main.startLifetime.constantMin, ps.main.startLifetime.constantMax);
            ps.Emit(emit, 1);
        }
    }

    private void Ring(Vector2 position, float size, Color color)
    {
        var emit = new ParticleSystem.EmitParams
        {
            position = position,
            velocity = Vector3.zero,
            startColor = color,
            startSize = Mathf.Max(0.1f, size * 0.3f),
            startLifetime = 0.28f,
        };
        rings.Emit(emit, 1);
    }

    // ---------------- floating numbers ----------------
    private class FloatingNumber
    {
        public GameObject go;
        public TextMesh text;
        public float born;
        public float life;
        public Vector2 velocity;
        public Color color;
        public float scale;
    }

    private readonly List<FloatingNumber> liveNumbers = new List<FloatingNumber>();

    private FloatingNumber MakeNumber()
    {
        var go = new GameObject("DamageNumber");
        go.transform.SetParent(transform, false);
        var tm = go.AddComponent<TextMesh>();
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 48;
        tm.characterSize = 0.07f;
        tm.fontStyle = FontStyle.Bold;
        go.GetComponent<MeshRenderer>().sortingOrder = 40;
        go.SetActive(false);
        return new FloatingNumber { go = go, text = tm };
    }

    private void Number(Vector2 position, float amount, Color color, float scale, string prefix = "")
    {
        var n = numbers.Count > 0 ? numbers.Dequeue() : null;
        if (n == null)
        {
            // Pool exhausted: recycle the oldest live number so a flurry never allocates
            if (liveNumbers.Count == 0) return;
            n = liveNumbers[0];
            liveNumbers.RemoveAt(0);
        }
        n.go.SetActive(true);
        n.go.transform.position = position + Random.insideUnitCircle * 0.25f;
        n.go.transform.localScale = Vector3.one * scale * 0.6f;
        n.text.text = prefix + Mathf.Max(1, Mathf.RoundToInt(amount));
        n.color = color;
        n.text.color = color;
        n.born = Time.unscaledTime;
        n.life = numberLife;
        n.scale = scale;
        n.velocity = new Vector2(Random.Range(-0.6f, 0.6f), 2.6f);
        liveNumbers.Add(n);
    }

    void Update()
    {
        for (int i = liveNumbers.Count - 1; i >= 0; i--)
        {
            var n = liveNumbers[i];
            float age = (Time.unscaledTime - n.born) / n.life;
            if (age >= 1f)
            {
                n.go.SetActive(false);
                liveNumbers.RemoveAt(i);
                numbers.Enqueue(n);
                continue;
            }
            n.go.transform.position += (Vector3)(n.velocity * Time.unscaledDeltaTime);
            n.velocity.y = Mathf.Max(0.4f, n.velocity.y - 5f * Time.unscaledDeltaTime);
            // Pop in, hold, then fade; the quick overshoot is what makes a number feel like an impact
            float pop = age < 0.12f ? Mathf.Lerp(0.5f, 1.35f, age / 0.12f) : Mathf.Lerp(1.35f, 1f, Mathf.Clamp01((age - 0.12f) / 0.15f));
            n.go.transform.localScale = Vector3.one * n.scale * pop;
            var c = n.color;
            c.a = age < 0.6f ? 1f : 1f - (age - 0.6f) / 0.4f;
            n.text.color = c;
        }
    }
}
