using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player character rotation by raycasting to find a point on the ground plane.
/// Responsibility: Read mouse input → Raycast to ground → Rotate player to face direction
/// Fires aiming events for other systems to react to (animation, UI, etc).
/// </summary>
public class PlayerAiming : MonoBehaviour
{
    [SerializeField] private float rotationLerpSpeed = 20f;
    
    private Camera mainCamera;
    private Rigidbody2D rb;
    private float targetAngle;
    private float lastSentAngle;
    private Vector2 lastAimDirection = Vector2.up;

    private void Awake()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (mainCamera == null || Mouse.current == null)
            return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y, -mainCamera.transform.position.z)
        );

        Vector2 aimDir = (mouseWorld - transform.position);
        if (aimDir.sqrMagnitude < 0.0001f)
            return;

        aimDir.Normalize();

        // Angle for top-down 2D (Z rotation)
        targetAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90f;

        if (Vector2.Distance(lastAimDirection, aimDir) > 0.01f)
        {
            lastAimDirection = aimDir;
            GameEvents.OnPlayerAimDirectionChanged?.Invoke(aimDir);
        }
    }
    private void FixedUpdate()
    {
        float newAngle = Mathf.LerpAngle(
            rb.rotation,
            targetAngle,
            rotationLerpSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(newAngle);
    }
}
