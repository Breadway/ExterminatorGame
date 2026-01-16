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
    private Plane groundPlane;
    private Rigidbody rb;
    private Quaternion targetRotation = Quaternion.identity;
    private Vector2 cachedMousePos;
    private Vector3 lastAimDirection = Vector3.forward;

    private void Awake()
    {
        mainCamera = Camera.main;
        groundPlane = new Plane(Vector3.up, Vector3.zero);
        rb = GetComponent<Rigidbody>();
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
            Ray ray = mainCamera.ScreenPointToRay(currentMousePos);
            if (groundPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance);
                worldPoint.y = transform.position.y;
                Vector3 dir = (worldPoint - transform.position).normalized;
                
                if (dir.sqrMagnitude > 0f)
                {
                    targetRotation = Quaternion.LookRotation(dir);
                    
                    // Fire event if direction changed significantly
                    if (Vector3.Distance(lastAimDirection, dir) > 0.01f)
                    {
                        lastAimDirection = dir;
                        GameEvents.OnPlayerAimDirectionChanged?.Invoke(dir);
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        // Apply rotation smoothly
        if (rb != null)
        {
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationLerpSpeed * Time.fixedDeltaTime));
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.fixedDeltaTime);
        }
    }
}
