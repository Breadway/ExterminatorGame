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

    private Rigidbody rb;
    private PlayerControls controls;
    private Vector2 moveInput;
    private bool isInitialized = false;

    private float lastDashTime = -Mathf.Infinity;
    private float dashEndTime = 0f;
    private Vector3 dashDirection;
    private bool isDashing = false;
    private bool isRecovering = false;
    private float recoveryEndTime = 0f;
    private Vector3 recoveryStartVelocity;

    private float knockbackEndTime = 0f;
    private Vector3 knockbackVelocity;
    
    [Header("Collision")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float skinWidth = 0.05f;
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    private Collider col;
    private bool isCapsule = false;
    private float capsuleRadius = 0.5f;
    private float capsuleHeight = 2f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        var cap = GetComponent<CapsuleCollider>();
        if (cap != null)
        {
            isCapsule = true;
            capsuleRadius = Mathf.Max(cap.radius * Mathf.Max(transform.localScale.x, transform.localScale.z), 0.01f);
            capsuleHeight = Mathf.Max(cap.height * transform.localScale.y, 0.01f);
        }
        else if (col != null)
        {
            // approximate radius/height from bounds
            capsuleRadius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);
            capsuleHeight = Mathf.Max(col.bounds.size.y, 0.01f);
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
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

    private void OnDestroy()
    {
        if (controls != null)
        {
            controls.Dispose();
        }
    }

    public void UpdateSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
    private void OnMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>().normalized;
        GameEvents.OnPlayerMovementInput?.Invoke(moveInput);
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
        // Use camera-relative input direction for dash when input exists, otherwise player's forward
        if (cameraTransform != null && moveInput.sqrMagnitude > 0.0001f)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();
            dashDirection = (camRight * moveInput.x + camForward * moveInput.y).normalized;
        }
        else
        {
            dashDirection = transform.forward;
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
            rb.linearVelocity = new Vector3(knockbackVelocity.x, rb.linearVelocity.y, knockbackVelocity.z);
            return;
        }

        if (isDashing)
        {
            if (Time.time < dashEndTime)
            {
                // Apply horizontal dash velocity, preserve vertical (gravity)
                rb.linearVelocity = new Vector3(dashDirection.x * dashSpeed, rb.linearVelocity.y, dashDirection.z * dashSpeed);
            }
            else
            {
                // End dash and begin short recovery to avoid abrupt stop
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
            Vector3 target = new Vector3(0f, recoveryStartVelocity.y, 0f);
            rb.linearVelocity = Vector3.Lerp(recoveryStartVelocity, target, t);
            if (Time.time >= recoveryEndTime)
            {
                isRecovering = false;
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
            return;
        }

        Move();
    }

    public void ApplyKnockback(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 flatDir = direction.normalized;
        knockbackVelocity = flatDir * knockbackSpeed;
        knockbackEndTime = Time.time + knockbackDuration;
        isDashing = false;
        isRecovering = false;
    }

    public void Move()
    {
        // Normal movement via velocity to ensure collision response
        Vector3 horizontalDesired;
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();
            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            horizontalDesired = (camRight * moveInput.x + camForward * moveInput.y) * moveSpeed;
        }
        else
        {
            horizontalDesired = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed;
        }

        // If no input, zero horizontal velocity
        if (horizontalDesired.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 dir = horizontalDesired.normalized;
        float checkDistance = horizontalDesired.magnitude * Time.fixedDeltaTime + skinWidth;

        RaycastHit hit;
        bool blocked = false;

        if (col == null)
        {
            // fallback: no collider, just move
            rb.linearVelocity = new Vector3(horizontalDesired.x, rb.linearVelocity.y, horizontalDesired.z);
            return;
        }

        if (isCapsule)
        {
            Vector3 p1 = transform.position + Vector3.up * capsuleRadius;
            Vector3 p2 = transform.position + Vector3.up * (capsuleHeight - capsuleRadius);
            blocked = Physics.CapsuleCast(p1, p2, capsuleRadius, dir, out hit, checkDistance, obstacleMask, QueryTriggerInteraction.Ignore);
        }
        else
        {
            Vector3 sphereCenter = transform.position + Vector3.up * (col.bounds.extents.y * 0.5f);
            blocked = Physics.SphereCast(sphereCenter, capsuleRadius, dir, out hit, checkDistance, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        if (blocked)
        {
            // slide along surface instead of penetrating
            Vector3 slid = Vector3.ProjectOnPlane(horizontalDesired, hit.normal);
            horizontalDesired = slid;
        }

        rb.linearVelocity = new Vector3(horizontalDesired.x, rb.linearVelocity.y, horizontalDesired.z);
    }
}
