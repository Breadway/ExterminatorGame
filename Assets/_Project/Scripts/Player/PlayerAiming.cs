using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player aiming direction based on mouse position.
/// Responsibility: Read mouse input → Calculate aim direction → Rotate firePoint (NOT player character)
/// Fires aiming events for other systems to react to (animation, UI, etc).
/// </summary>
public class PlayerAiming : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The transform that should rotate to face the mouse. Usually a child object containing the weapon/attack point.")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera mainCamera;
    
    [Header("Settings")]
    [Tooltip("If true, the firePoint will instantly snap to face the mouse. If false, it will smoothly rotate.")]
    [SerializeField] private bool instantAim = true;
    [SerializeField] private float aimSmoothSpeed = 15f;
    
    /// <summary>
    /// Current aim direction in world space (normalized).
    /// </summary>
    public Vector2 AimDirection { get; private set; } = Vector2.up;
    
    /// <summary>
    /// Current mouse position in world space.
    /// </summary>
    public Vector2 MouseWorldPosition { get; private set; }
    
    /// <summary>
    /// The effective range of the current weapon. Queried from IWeaponAttack.
    /// Used for UI feedback (range indicators, etc).
    /// </summary>
    public float WeaponRange { get; private set; }
    
    private IWeaponAttack currentWeapon;
    
    void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }
    
    void Start()
    {
        // Query weapon range from the weapon component
        // This is decoupled - PlayerAiming finds the weapon, weapon doesn't need to know about aiming
        currentWeapon = GetComponent<IWeaponAttack>();
        if (currentWeapon == null)
        {
            currentWeapon = GetComponent<IWeaponAttack>();
        }
        
        if (currentWeapon != null)
        {
            WeaponRange = currentWeapon.WeaponRange;
        }
        else
        {
            GameEvents.DebugWarning("PlayerAiming: No IWeaponAttack component found. WeaponRange will be 0.", DebugCategory.Input);
        }
    }
    
    void Update()
    {
        UpdateAimDirection();
        RotateFirePoint();
    }
    
    private void UpdateAimDirection()
    {
        if (mainCamera == null) return;
        
        // Get mouse screen position from input system
        if (Mouse.current == null) return;
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        
        // Convert to world position
        MouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        
        // Calculate direction from player to mouse
        Vector2 toMouse = MouseWorldPosition - (Vector2)firePoint.position;
        
        if (toMouse.sqrMagnitude > 0.001f)
        {
            AimDirection = toMouse.normalized;
            
            // Fire event for other systems (animation, UI crosshair, etc)
            GameEvents.OnPlayerAimDirectionChanged?.Invoke(AimDirection);
        }
    }
    
    private void RotateFirePoint()
    {
        if (firePoint == null) return;
        
        // Calculate target rotation (2D: rotate around Z axis so "up" faces the aim direction)
        float targetAngle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg - 90f;
        
        if (instantAim)
        {
            firePoint.rotation = Quaternion.Euler(0f, 0f, targetAngle);
        }
        else
        {
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
            firePoint.rotation = Quaternion.Slerp(firePoint.rotation, targetRotation, aimSmoothSpeed * Time.deltaTime);
        }
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;
        
        // Draw aim direction line with weapon range
        Gizmos.color = Color.cyan;
        Vector3 start = firePoint.position;
        float displayRange = WeaponRange > 0 ? WeaponRange : 3f;
        Vector3 end = start + (Vector3)(AimDirection * displayRange);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.2f);
        
        // Draw range circle
        Gizmos.color = new Color(0, 1, 1, 0.1f);
        UnityEditor.Handles.color = new Color(0, 1, 1, 0.3f);
        UnityEditor.Handles.DrawWireDisc(start, Vector3.forward, displayRange);
    }
    #endif
}
