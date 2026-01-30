using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player movement via Rigidbody2D.
/// Responsibility: Read input → Calculate velocity → Move via physics
/// Fires movement events for other systems to react to (animation, sfx, etc).
/// </summary>
public class PlayerMovement : MonoBehaviour, IMovementModifiable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.8f;
    [SerializeField] private float dashRecoveryTime = 0.1f;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackSpeed = 8f;
    [SerializeField] private float knockbackDuration = 0.2f;

    private Rigidbody2D rb;
    private PlayerControls controls;
    private PlayerStats stats;
    private Vector2 moveInput;
    private Vector2 dashDirection;
    private bool isInitialized = false;

    private float lastDashTime = -Mathf.Infinity;
    private float dashEndTime = 0f;
    private bool isDashing = false;
    private bool isRecovering = false;
    private float recoveryEndTime = 0f;
    private Vector2 recoveryStartVelocity;

    private float knockbackEndTime = 0f;
    private Vector2 knockbackVelocity;
    
    [Header("Collision")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float skinWidth = 0.05f;
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    private Collider2D col;
    private bool isCapsule = false;
    private float capsuleRadius = 0.5f;
    private float capsuleHeight = 2f;
    private float speedMultiplier = 1f;
    private float currentSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        // Get PlayerStats from PlayerController
        var playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            stats = playerController.stats;
        }
        
        var cap = GetComponent<CapsuleCollider2D>();
        if (cap != null)
        {
            isCapsule = true;
            capsuleRadius = Mathf.Max(cap.size.x * 0.5f * Mathf.Max(transform.localScale.x, transform.localScale.y), 0.01f);
            capsuleHeight = Mathf.Max(cap.size.y * transform.localScale.y, 0.01f);
        }
        else if (col != null)
        {
            // approximate radius/height from bounds
            capsuleRadius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y);
            capsuleHeight = Mathf.Max(col.bounds.size.y, 0.01f);
        }

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Smoother collision handling
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smoother visual movement
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            controls = new PlayerControls();
            isInitialized = true;
        }
        controls.Player.Move.Enable();
        controls.Player.Move.performed += OnMoveInput;
        controls.Player.Move.canceled += OnMoveInput;

        controls.Player.Dash.Enable();
        controls.Player.Dash.performed += OnDashInput;
    }

    private void OnDisable()
    {
        if (controls != null)
        {
            controls.Player.Move.performed -= OnMoveInput;
            controls.Player.Move.canceled -= OnMoveInput;
            controls.Player.Move.Disable();

            controls.Player.Dash.performed -= OnDashInput;
            controls.Player.Dash.Disable();
        }
    }
    private void OnMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>().normalized;
        GameEvents.OnPlayerMovementInput?.Invoke(moveInput);
    }

    public void UpdateSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    private void OnDashInput(InputAction.CallbackContext context)
    {
        // Use PlayerStats dash cooldown if available, otherwise fall back to serialized value
        float currentDashCooldown = stats != null ? stats.currentDashCooldown : dashCooldown;
        if (Time.time >= lastDashTime + currentDashCooldown)
        {
            PerformDash();
        }
    }

    private void PerformDash()
    {
        // Use input direction for dash when input exists, otherwise player's forward (up in 2D)
        if (moveInput.sqrMagnitude > 0.0001f)
        {
            dashDirection = moveInput.normalized;
        }
        else
        {
            dashDirection = transform.up;
        }
        lastDashTime = Time.time;
        dashEndTime = Time.time + dashDuration;
        isDashing = true;

        GameEvents.OnPlayerDash?.Invoke(dashDirection);
    }
    private void FixedUpdate()
    {
        if (Time.time < knockbackEndTime)
        {
            rb.linearVelocity = knockbackVelocity;
            return;
        }

        if (isDashing)
        {
            if (Time.time < dashEndTime)
            {
                rb.linearVelocity = dashDirection * dashSpeed;
            }
            else
            {
                isDashing = false;
                isRecovering = true;
                recoveryStartVelocity = rb.linearVelocity;
                recoveryEndTime = Time.time + dashRecoveryTime;
            }
            return;
        }

        if (isRecovering)
        {
            float t = Mathf.Clamp01(1f - (recoveryEndTime - Time.time) / dashRecoveryTime);
            Vector2 target = Vector2.zero;
            rb.linearVelocity = Vector2.Lerp(recoveryStartVelocity, target, t);
            if (Time.time >= recoveryEndTime)
            {
                isRecovering = false;
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        Move();
    }

    public void ApplyKnockback(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector2 flatDir = direction.normalized;
        knockbackVelocity = flatDir * knockbackSpeed;
        knockbackEndTime = Time.time + knockbackDuration;

        isDashing = false;
        isRecovering = false;
    }

    private void Move()
    {
        // Normal movement via velocity to ensure collision response
        // Use PlayerStats speed if available, otherwise fall back to serialized value
        currentSpeed = stats != null ? stats.currentSpeed : moveSpeed;
        currentSpeed *= speedMultiplier;
        Vector2 desiredVelocity = moveInput * currentSpeed;

        // If no input, zero velocity
        if (desiredVelocity.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = desiredVelocity.normalized;
        float checkDistance = desiredVelocity.magnitude * Time.fixedDeltaTime + skinWidth;

        RaycastHit2D hit;
        bool blocked = false;

        // Use the collider's current position for casting
        Vector2 castOrigin = (Vector2)transform.position;

        if (isCapsule)
        {
            hit = Physics2D.CapsuleCast(castOrigin, new Vector2(capsuleRadius * 2f, capsuleHeight),
                CapsuleDirection2D.Vertical, 0f, dir, checkDistance, obstacleMask);
            blocked = hit.collider != null && hit.distance > 0f; // Only block if we're not already overlapping
        }
        else
        {
            hit = Physics2D.CircleCast(castOrigin, capsuleRadius, dir, checkDistance, obstacleMask);
            blocked = hit.collider != null && hit.distance > 0f; // Only block if we're not already overlapping
        }

        if (blocked && hit.collider != null)
        {
            // Slide along surface instead of penetrating
            // Calculate the component of velocity parallel to the surface
            Vector2 slideDir = Vector2.Perpendicular(hit.normal);
            float slideMagnitude = Vector2.Dot(desiredVelocity, slideDir);
            desiredVelocity = slideDir * slideMagnitude;
        }

        // Let the Rigidbody2D handle the actual collision resolution
        // by setting velocity and allowing physics to do its job
        rb.linearVelocity = desiredVelocity;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    private void OnDestroy()
    {
        if (controls != null)
        {
            controls.Dispose();
        }
    }
}
