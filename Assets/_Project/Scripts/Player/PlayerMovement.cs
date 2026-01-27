using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player movement via Rigidbody2D.
/// Responsibility: Read input → Calculate velocity → Move via physics
/// Fires movement events for other systems to react to (animation, sfx, etc).
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 20f;
    
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 40f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.5f;
    [SerializeField] private float dashRecoveryTime = 0.05f;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackSpeed = 12f;
    [SerializeField] private float knockbackDuration = 0.15f;

    private Rigidbody2D rb;
    private PlayerControls controls;
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
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
        if (Time.time >= lastDashTime + dashCooldown)
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
        Vector2 desiredVelocity = moveInput * moveSpeed;

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

        if (isCapsule)
        {
            hit = Physics2D.CapsuleCast((Vector2)transform.position, new Vector2(capsuleRadius * 2f, capsuleHeight), CapsuleDirection2D.Vertical, 0f, dir, checkDistance, obstacleMask);
            blocked = hit.collider != null;
        }
        else
        {
            hit = Physics2D.CircleCast((Vector2)transform.position, capsuleRadius, dir, checkDistance, obstacleMask);
            blocked = hit.collider != null;
        }

        if (blocked && hit.collider != null)
        {
            // slide along surface instead of penetrating
            Vector2 slid = Vector2.Perpendicular(hit.normal) * Vector2.Dot(desiredVelocity, Vector2.Perpendicular(hit.normal));
            desiredVelocity = slid;
        }

        rb.linearVelocity = desiredVelocity;
    }
    private void OnDestroy()
    {
        if (controls != null)
        {
            controls.Dispose();
        }
    }
}
