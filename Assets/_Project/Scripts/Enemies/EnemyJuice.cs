using UnityEngine;

/// <summary>
/// Visual life for enemies without per-enemy animation art: a white hit flash, a squash on impact,
/// a waddle while moving, a wobble tilt, and a death pop. Works on the Enemy's SpriteRenderer and
/// resets cleanly when pooled, so a reused enemy never spawns still flashing or squashed.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyJuice : MonoBehaviour
{
    [SerializeField] private float flashSeconds = 0.07f;
    [SerializeField] private float waddleAmount = 5f;
    [SerializeField] private float waddleSpeed = 14f;
    [SerializeField] private float squashRecover = 16f;

    private Enemy enemy;
    private SpriteRenderer sprite;
    private Rigidbody2D body;
    private Transform visual;
    private Vector3 baseScale = Vector3.one;
    private Material original;
    private static Material flashMaterial;
    private float flashUntil;
    private Vector2 squash = Vector2.one;
    private float phase;
    private float appliedTilt;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        body = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null)
        {
            visual = sprite.transform;
            baseScale = visual.localScale;
            original = sprite.sharedMaterial;
        }
        if (flashMaterial == null)
        {
            var shader = Shader.Find("GUI/Text Shader");
            if (shader != null) flashMaterial = new Material(shader);
        }
        phase = Random.value * 6.28f;
    }

    void OnEnable()
    {
        ResetLook();
        if (enemy != null && enemy.Health != null) enemy.Health.OnDamaged += HandleDamaged;
        // Pop in on spawn
        squash = new Vector2(0.4f, 0.4f);
    }

    void OnDisable()
    {
        if (enemy != null && enemy.Health != null) enemy.Health.OnDamaged -= HandleDamaged;
        ResetLook();
    }

    private void ResetLook()
    {
        flashUntil = 0f;
        squash = Vector2.one;
        if (sprite != null && original != null) sprite.sharedMaterial = original;
        if (visual != null)
        {
            visual.localScale = baseScale;
            // Remove only our own tilt; the movement script owns the facing rotation
            visual.Rotate(0f, 0f, -appliedTilt);
        }
        appliedTilt = 0f;
    }

    private void HandleDamaged(float amount, Vector2 hitPoint, Vector2 hitDirection)
    {
        flashUntil = Time.unscaledTime + flashSeconds;
        float heavy = Mathf.Clamp01(amount / 30f);
        squash = new Vector2(1f + 0.2f + 0.15f * heavy, 1f - 0.18f - 0.12f * heavy);
    }

    void Update()
    {
        if (sprite == null || visual == null || Time.timeScale <= 0f) return;

        bool flashing = Time.unscaledTime < flashUntil;
        if (flashMaterial != null) sprite.sharedMaterial = flashing ? flashMaterial : original;

        squash = Vector2.Lerp(squash, Vector2.one, Time.deltaTime * squashRecover);
        float speed = body != null ? body.linearVelocity.magnitude : 0f;

        // Movement waddle, faster and tighter as speed rises; stationary enemies breathe instead
        float moving = Mathf.Clamp01(speed / 4f);
        float sway = Mathf.Sin(Time.time * waddleSpeed * (0.6f + moving) + phase);
        float breathe = Mathf.Sin(Time.time * 3f + phase) * 0.02f;
        float stretch = 1f + (moving > 0.1f ? sway * 0.05f : breathe);

        visual.localScale = new Vector3(baseScale.x * squash.x * (2f - stretch), baseScale.y * squash.y * stretch, baseScale.z);

        // The movement script rotates this transform to face the player, so the waddle is applied
        // as a delta on top of whatever rotation it left, never as an absolute angle.
        float tilt = sway * waddleAmount * moving;
        visual.Rotate(0f, 0f, tilt - appliedTilt);
        appliedTilt = tilt;
    }
}
