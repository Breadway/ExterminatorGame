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
        if (mainCamera == null) return;

        // Cache mouse position to avoid repeated ReadValue() calls
        Vector2 currentMousePos = Mouse.current.position.ReadValue();
        
        // Only raycast if mouse position has changed
        if (currentMousePos != cachedMousePos)
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
