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

    private Rigidbody rb;
    private PlayerControls controls;
    private Vector2 moveInput;
    private bool isInitialized = false;

    private float lastDashTime = -Mathf.Infinity;
    private float dashEndTime = 0f;
    private Vector3 dashDirection;
    private bool isDashing = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
        // Use player's current facing direction for dash
        dashDirection = transform.forward;
        lastDashTime = Time.time;
        dashEndTime = Time.time + dashDuration;
        isDashing = true;

        GameEvents.OnPlayerDash?.Invoke(dashDirection);
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            if (Time.time < dashEndTime)
            {
                // Direct velocity application for dash
                rb.linearVelocity = dashDirection * dashSpeed;
            }
            else
            {
                // Stop dashing and kill velocity to prevent sliding
                isDashing = false;
                rb.linearVelocity = Vector3.zero;
            }
            return;
        }

        Move();
    }

    public void Move()
    {
        // Normal movement via MovePosition
        Vector3 moveVelocity = new Vector3(moveInput.x, 0f, moveInput.y) * moveSpeed;
        Vector3 targetPos = rb.position + moveVelocity * Time.fixedDeltaTime;
        rb.MovePosition(targetPos);
    }
}
