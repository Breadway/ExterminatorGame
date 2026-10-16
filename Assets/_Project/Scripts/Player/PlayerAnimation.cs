using UnityEngine;

/// <summary>
/// Drives the player's Animator and procedural polish. The Animator (Idle/Walk/Dash/Hurt/Death)
/// comes from Exterminator > Build Animations. On top of the clips this adds squash and stretch,
/// a landing squash after a dash, a hit flash, footstep sounds and horizontal flipping toward the aim.
/// Without an Animator everything still works except the frame changes.
/// </summary>
public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Animation Parameters")]
    [SerializeField] private string isMovingParam = "IsMoving";
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string aimXParam = "AimX";
    [SerializeField] private string aimYParam = "AimY";

    [Header("Polish")]
    [SerializeField] private float stepInterval = 0.26f;
    [SerializeField] private float flashSeconds = 0.09f;
    [SerializeField] private float squashRecover = 14f;

    public enum Direction5 { Up, UpDiagonal, Side, DownDiagonal, Down }
    public Direction5 CurrentMoveDirection5 { get; private set; } = Direction5.Down;
    public Direction5 CurrentAimDirection5 { get; private set; } = Direction5.Down;
    public bool ShouldFlipSprite { get; private set; }

    private Vector2 moveDirection;
    private Vector2 aimDirection = Vector2.down;
    private bool isMoving;
    private bool dead;
    private float nextStep;
    private float flashUntil;
    private Vector3 baseScale;
    private Vector2 squash = Vector2.one;
    private Material flashMaterial;
    private Material originalMaterial;
    private bool hasAnimatorParams;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.sharedMaterial;
            var shader = Shader.Find("GUI/Text Shader");
            if (shader != null) flashMaterial = new Material(shader);
        }
        hasAnimatorParams = animator != null && animator.runtimeAnimatorController != null;
    }

    void OnEnable()
    {
        GameEvents.OnPlayerMovementInput += HandleMovementInput;
        GameEvents.OnPlayerAimDirectionChanged += HandleAimDirectionChanged;
        GameEvents.OnPlayerDash += HandleDash;
        GameEvents.OnPlayerDamaged += HandleDamaged;
        GameEvents.OnPlayerDied += HandleDied;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerMovementInput -= HandleMovementInput;
        GameEvents.OnPlayerAimDirectionChanged -= HandleAimDirectionChanged;
        GameEvents.OnPlayerDash -= HandleDash;
        GameEvents.OnPlayerDamaged -= HandleDamaged;
        GameEvents.OnPlayerDied -= HandleDied;
        if (spriteRenderer != null && originalMaterial != null) spriteRenderer.sharedMaterial = originalMaterial;
        transform.localScale = baseScale;
    }

    // ---------------- events ----------------
    private void HandleMovementInput(Vector2 input)
    {
        moveDirection = input;
        isMoving = input.sqrMagnitude > 0.01f;
        if (isMoving)
        {
            var r = VectorToDirection5(input);
            CurrentMoveDirection5 = r.direction;
        }
        PushParameters();
    }

    private void HandleAimDirectionChanged(Vector2 aim)
    {
        aimDirection = aim;
        var r = VectorToDirection5(aim);
        CurrentAimDirection5 = r.direction;
        ShouldFlipSprite = r.flip;
        if (spriteRenderer != null) spriteRenderer.flipX = ShouldFlipSprite;
        PushParameters();
    }

    private void HandleDash(Vector2 direction)
    {
        Trigger("Dash");
        squash = new Vector2(0.78f, 1.28f); // stretch along the dash, snap back after
        GameEvents.PlaySound("dash");
    }

    private void HandleDamaged(float amount, float hp, float max)
    {
        Trigger("Hurt");
        flashUntil = Time.unscaledTime + flashSeconds;
        squash = new Vector2(1.25f, 0.8f);
    }

    private void HandleDied()
    {
        dead = true;
        Trigger("Death");
    }

    // ---------------- per frame ----------------
    void Update()
    {
        if (dead) return;

        // Footsteps keyed to movement, not to the animation, so they stay in time with real speed
        if (isMoving && Time.timeScale > 0f && Time.time >= nextStep)
        {
            nextStep = Time.time + stepInterval;
            GameEvents.PlaySound("step_" + Random.Range(0, 2));
        }

        // Breathing squash while idle, springing back to rest after any impact
        float breathe = isMoving ? 0f : Mathf.Sin(Time.time * 2.4f) * 0.012f;
        squash = Vector2.Lerp(squash, new Vector2(1f - breathe, 1f + breathe), Time.unscaledDeltaTime * squashRecover);
        transform.localScale = new Vector3(baseScale.x * squash.x, baseScale.y * squash.y, baseScale.z);

        if (spriteRenderer != null && flashMaterial != null)
        {
            bool flashing = Time.unscaledTime < flashUntil;
            spriteRenderer.sharedMaterial = flashing ? flashMaterial : originalMaterial;
            spriteRenderer.color = flashing ? Color.white : spriteRenderer.color;
        }
    }

    // ---------------- animator helpers ----------------
    private void PushParameters()
    {
        if (!hasAnimatorParams) return;
        animator.SetBool(isMovingParam, isMoving);
        animator.SetFloat(moveXParam, moveDirection.x);
        animator.SetFloat(moveYParam, moveDirection.y);
        animator.SetFloat(aimXParam, aimDirection.x);
        animator.SetFloat(aimYParam, aimDirection.y);
    }

    private void Trigger(string name)
    {
        if (hasAnimatorParams) animator.SetTrigger(name);
    }

    /// <summary>Convert a Vector2 direction to a 5-directional enum with a flip flag.</summary>
    public static (Direction5 direction, bool flip) VectorToDirection5(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return (Direction5.Down, false);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        bool flip = angle > 90f && angle < 270f;

        if (angle >= 67.5f && angle < 112.5f) return (Direction5.Up, false);
        if ((angle >= 22.5f && angle < 67.5f) || (angle >= 112.5f && angle < 157.5f)) return (Direction5.UpDiagonal, flip);
        if (angle >= 337.5f || angle < 22.5f || (angle >= 157.5f && angle < 202.5f)) return (Direction5.Side, flip);
        if ((angle >= 202.5f && angle < 247.5f) || (angle >= 292.5f && angle < 337.5f)) return (Direction5.DownDiagonal, flip);
        return (Direction5.Down, false);
    }

    // Kept for callers of the earlier stubs
    public void PlayIdleAnimation(Direction5 direction) { }
    public void PlayAttackAnimation(Direction5 direction) { }
    public void PlayDashAnimation(Direction5 direction) { Trigger("Dash"); }
    public void PlayHurtAnimation() { Trigger("Hurt"); }
    public void PlayDeathAnimation() { Trigger("Death"); }
}
