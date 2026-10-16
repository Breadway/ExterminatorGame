using UnityEngine;

/// <summary>
/// The visible half of weapon feel. Listens for weapon events and draws what each weapon does:
/// a flame jet with smoke, a foam spray, a mop swing arc, charge glow and a muzzle flash.
/// Built from runtime particle systems on the player's fire point, so it follows the aim exactly
/// and needs no per-weapon prefabs. Sprites are optional; flat dots are used if they are not assigned.
/// </summary>
public class WeaponVisual : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private Sprite[] flameFrames;
    [SerializeField] private Sprite smokeSprite;
    [Tooltip("Held weapon sprites, matched by position to the ids below.")]
    [SerializeField] private Sprite[] heldSprites;
    [SerializeField] private string[] heldIds = { "flamethrower", "mop", "foam", "cables", "beam" };
    [Tooltip("World-unit distance from the body centre at which the weapon is held.")]
    [SerializeField] private float holdRadius = 0.62f;

    private ParticleSystem flame, smoke, foam, glow;
    private SpriteRenderer arc;
    private SpriteRenderer held_;
    private float arcUntil;
    private Material mat;
    private Sprite dot;
    private string held = "";

    void Awake()
    {
        if (firePoint == null)
        {
            var p = transform.Find("FirePoint");
            firePoint = p != null ? p : transform;
        }
        mat = new Material(Shader.Find("Sprites/Default"));
        dot = MakeDot();
        flame = Build("FlameJet", flameFrames != null && flameFrames.Length > 0 ? flameFrames : new[] { dot }, 0.35f, 0.55f, 4.5f, 7.5f, 0.9f, 1.5f, 14f, true);
        smoke = Build("FlameSmoke", new[] { smokeSprite != null ? smokeSprite : dot }, 0.7f, 1.1f, 1.2f, 2.6f, 0.8f, 1.5f, 4f, false);
        foam = Build("FoamSpray", new[] { dot }, 0.25f, 0.45f, 5f, 8f, 0.18f, 0.4f, 70f, true);
        glow = Build("ChargeGlow", new[] { dot }, 0.25f, 0.35f, 0f, 0.4f, 0.3f, 0.5f, 0f, false);
        BuildArc();
        BuildHeld();
        SetEmit(flame, false);
        SetEmit(smoke, false);
        SetEmit(foam, false);
        SetEmit(glow, false);
    }

    void OnEnable()
    {
        GameEvents.OnWeaponHeld += HandleHeld;
        GameEvents.OnWeaponFired += HandleFired;
        GameEvents.OnWeaponEquipped += HandleEquipped;
    }

    void OnDisable()
    {
        GameEvents.OnWeaponHeld -= HandleHeld;
        GameEvents.OnWeaponFired -= HandleFired;
        GameEvents.OnWeaponEquipped -= HandleEquipped;
        SetEmit(flame, false);
        SetEmit(smoke, false);
        SetEmit(foam, false);
        SetEmit(glow, false);
    }

    // ---------------- events ----------------
    private void HandleHeld(string id, bool isHeld)
    {
        held = isHeld ? id : (held == id ? "" : held);
        switch (id)
        {
            case "flamethrower":
                SetEmit(flame, isHeld);
                SetEmit(smoke, isHeld);
                break;
            case "foam":
                SetEmit(foam, isHeld);
                break;
            case "beam":
                SetEmit(glow, isHeld);
                break;
        }
    }

    private void HandleFired(string id, Vector2 origin, Vector2 dir, float power)
    {
        if (id == "mop") ShowArc();
        else if (id == "cables" || id == "beam") Flash(origin, dir, id == "beam" ? new Color(0.5f, 1f, 0.9f) : new Color(0.7f, 0.9f, 1f), Mathf.Max(0.5f, power));
    }

    // Tip of the held weapon in world space, so effects come from the barrel rather than the body
    private Vector3 MuzzlePosition()
    {
        Vector3 aim = firePoint.up;
        float reach = holdRadius;
        if (held_ != null && held_.enabled && held_.sprite != null)
            reach += held_.sprite.bounds.size.y * Mathf.Abs(transform.lossyScale.y) * 0.5f;
        return firePoint.position + aim * reach;
    }

    private void BuildHeld()
    {
        var go = new GameObject("HeldWeapon");
        go.transform.SetParent(transform, false);
        held_ = go.AddComponent<SpriteRenderer>();
        held_.sortingOrder = 8;
        held_.enabled = false;
    }

    private void HandleEquipped(string id)
    {
        if (held_ == null || heldSprites == null) return;
        int i = System.Array.IndexOf(heldIds, id);
        held_.sprite = i >= 0 && i < heldSprites.Length ? heldSprites[i] : null;
        held_.enabled = held_.sprite != null;
    }

    // The weapon orbits the body toward the aim. The player is scaled up, so positions are set in
    // world units and converted, instead of trusting local offsets under a scaled parent.
    private void PlaceHeld()
    {
        if (held_ == null || !held_.enabled || firePoint == null) return;
        Vector3 aim = firePoint.up;
        var parentScale = transform.lossyScale;
        Vector3 world = aim * holdRadius;
        held_.transform.localPosition = new Vector3(world.x / Mathf.Max(0.0001f, parentScale.x), world.y / Mathf.Max(0.0001f, parentScale.y), 0f);
        held_.transform.rotation = firePoint.rotation;
        // The player sprite flips toward the aim; keep the weapon the right way round
        held_.flipX = false;
    }

    // ---------------- per frame ----------------
    void LateUpdate()
    {
        if (firePoint == null) return;
        PlaceHeld();
        // Particles are emitted in world space from the fire point, aimed along its up vector
        float angle = firePoint.eulerAngles.z;
        Vector3 muzzle = MuzzlePosition();
        foreach (var ps in new[] { flame, smoke, foam })
        {
            ps.transform.position = muzzle;
            ps.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        glow.transform.position = MuzzlePosition();

        if (held == "beam")
        {
            // The glow swells and brightens with the charge, the same value that drives the HUD meter
            var beam = GetComponent<ParticleBeamAttack>();
            float charge = beam != null ? beam.Charge : 0f;
            var main = glow.main;
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * (10f + charge * 22f));
            main.startSize = Mathf.Lerp(0.12f, 0.34f, charge) * pulse;
            // Cool teal while charging, shifting toward white-hot only in the last stretch
            var tint = Color.Lerp(new Color(0.25f, 0.85f, 0.8f, 0.45f), new Color(0.85f, 1f, 0.95f, 0.7f), charge * charge);
            main.startColor = tint;
            var emission = glow.emission;
            emission.rateOverTime = Mathf.Lerp(6f, 30f, charge);
        }

        if (arc != null && arc.enabled)
        {
            float k = 1f - (arcUntil - Time.unscaledTime) / 0.16f;
            var c = arc.color;
            c.a = Mathf.Clamp01(1f - k) * 0.7f;
            arc.color = c;
            arc.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.15f, k);
            if (Time.unscaledTime >= arcUntil) arc.enabled = false;
        }
    }

    // ---------------- helpers ----------------
    private static void SetEmit(ParticleSystem ps, bool on)
    {
        if (ps == null) return;
        var e = ps.emission;
        e.enabled = on;
        if (on && !ps.isPlaying) ps.Play();
    }

    private void Flash(Vector2 origin, Vector2 dir, Color color, float strength)
    {
        if (Fx.Instance != null) Fx.Instance.Spark(origin + dir * 0.5f, Mathf.RoundToInt(4 + strength * 8), color, 0.1f);
    }

    private void ShowArc()
    {
        if (arc == null) return;
        arc.enabled = true;
        arcUntil = Time.unscaledTime + 0.16f;
        arc.color = new Color(1f, 1f, 1f, 0.7f);
    }

    private void BuildArc()
    {
        // A pale crescent in front of the fire point, flashed during each mop swing
        var go = new GameObject("SwingArc");
        go.transform.SetParent(firePoint, false);
        go.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        arc = go.AddComponent<SpriteRenderer>();
        arc.sprite = MakeCrescent();
        arc.sortingOrder = 25;
        arc.enabled = false;
    }

    private static Sprite MakeDot()
    {
        const int n = 8;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                tex.SetPixel(x, y, Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) <= c ? Color.white : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }

    private static Sprite MakeCrescent()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = x - n * 0.5f, dy = y - n * 0.15f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                bool on = d > n * 0.34f && d < n * 0.46f && ang > 35f && ang < 145f;
                tex.SetPixel(x, y, on ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.15f), 32f);
    }

    private ParticleSystem Build(string name, Sprite[] sprites, float lifeMin, float lifeMax, float speedMin, float speedMax,
                                 float sizeMin, float sizeMax, float rate, bool cone)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.maxParticles = 160;
        main.startRotation = 0f;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = cone ? 13f : 28f;
        shape.radius = 0.05f;
        shape.rotation = new Vector3(0f, 0f, 0f);
        // Cone points along +Z by default; aim it along +Y in 2D
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.7f), new Keyframe(0.3f, 1f), new Keyframe(1, name == "FlameSmoke" ? 1.8f : 0.2f)));

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.sortingOrder = 22;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        var sheet = ps.textureSheetAnimation;
        sheet.enabled = true;
        sheet.mode = ParticleSystemAnimationMode.Sprites;
        foreach (var s in sprites) if (s != null) sheet.AddSprite(s);
        if (sprites.Length > 1)
        {
            sheet.timeMode = ParticleSystemAnimationTimeMode.FPS;
            sheet.fps = 10f;
        }
        return ps;
    }
}
