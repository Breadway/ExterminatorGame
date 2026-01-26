using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player movement via Rigidbody.
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
    private Collider2D col;
    private PlayerControls controls;
    private Vector2 moveInput;
    private Vector2 dashDirection;
    private bool isInitialized = false;

    private float lastDashTime = -Mathf.Infinity;
    private float dashEndTime = 0f;
    private bool isDashing = false;
    private bool isRecovering = false;
    private float recoveryEndTime = 0f;
    private float knockbackEndTime = 0f;
    private Vector2 recoveryStartVelocity;
    private Vector2 knockbackVelocity;
    
    [Header("Collision")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float skinWidth = 0.05f;
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

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
        dashDirection = moveInput.sqrMagnitude > 0.001f
            ? moveInput
            : Vector2.up;

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
            rb.linearVelocity = Vector2.Lerp(recoveryStartVelocity, Vector2.zero, t);

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
        if (direction.sqrMagnitude < 0.001f) return;

        knockbackVelocity = direction.normalized * knockbackSpeed;
        knockbackEndTime = Time.time + knockbackDuration;

        isDashing = false;
        isRecovering = false;
    }

    private void Move()
    {
        Vector2 desiredVelocity = moveInput * moveSpeed;

        if (desiredVelocity.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distance = desiredVelocity.magnitude * Time.fixedDeltaTime + skinWidth;

        RaycastHit2D[] hits = new RaycastHit2D[1];
        int hitCount = col.Cast(
            desiredVelocity.normalized,
            new ContactFilter2D { layerMask = obstacleMask, useLayerMask = true },
            hits,
            distance
        );
        RaycastHit2D hit = hitCount > 0 ? hits[0] : default;

        if (hit.collider != null)
        {
            // Project the desired velocity onto the surface tangent by removing the
            // component along the hit normal: v' = v - (v·n) n
            desiredVelocity = desiredVelocity - Vector2.Dot(desiredVelocity, hit.normal) * hit.normal;
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
