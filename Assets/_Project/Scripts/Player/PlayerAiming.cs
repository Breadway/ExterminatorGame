using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player character rotation in 2D by rotating towards mouse position.
/// Responsibility: Read mouse input → Calculate direction → Rotate player
/// Fires aiming events for other systems to react to (animation, UI, etc).
/// </summary>
public class PlayerAiming : MonoBehaviour
{
    [SerializeField] private float rotationLerpSpeed = 20f;
    
    private Camera mainCamera;
    private Rigidbody2D rb;
    private Quaternion targetRotation = Quaternion.identity;
    private Vector2 cachedMousePos;
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
            cachedMousePos = currentMousePos;
            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(currentMousePos.x, currentMousePos.y, mainCamera.nearClipPlane));
            Vector2 worldPoint2D = new Vector2(worldPoint.x, worldPoint.y);
            
            Vector2 dir = (worldPoint2D - (Vector2)transform.position).normalized;
            
            if (dir.sqrMagnitude > 0f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                targetRotation = Quaternion.Euler(0f, 0f, angle - 90f); // -90 because up is 0 degrees in 2D
                
                // Fire event if direction changed significantly
                if (Vector2.Distance(lastAimDirection, dir) > 0.01f)
                {
                    lastAimDirection = dir;
                    GameEvents.OnPlayerAimDirectionChanged?.Invoke(dir);
                }
            }
        }
    }
    private void FixedUpdate()
    {
        // Apply rotation smoothly
        if (rb != null)
        {
            float currentAngle = rb.rotation;
            float targetAngle = targetRotation.eulerAngles.z;
            float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationLerpSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.fixedDeltaTime);
        }
    }
}
