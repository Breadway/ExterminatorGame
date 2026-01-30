using UnityEngine;

/// <summary>
/// Handles player animation based on movement input and aim direction.
/// Responsibility: Listen to movement/aim events → Determine animation state → Play appropriate animation
/// Supports 8-directional movement animations.
/// </summary>
public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Animation Parameters")]
    [Tooltip("Parameter name for horizontal movement (-1 to 1)")]
    [SerializeField] private string moveXParam = "MoveX";
    [Tooltip("Parameter name for vertical movement (-1 to 1)")]
    [SerializeField] private string moveYParam = "MoveY";
    [Tooltip("Parameter name for whether player is moving")]
    [SerializeField] private string isMovingParam = "IsMoving";
    [Tooltip("Parameter name for aim horizontal direction (-1 to 1)")]
    [SerializeField] private string aimXParam = "AimX";
    [Tooltip("Parameter name for aim vertical direction (-1 to 1)")]
    [SerializeField] private string aimYParam = "AimY";
    
    // Current state tracking
    private Vector2 currentMoveDirection;
    private Vector2 currentAimDirection;
    private bool isMoving;
    
    // Animation direction enum for 5-directional (with sprite flipping for left/right)
    public enum Direction5
    {
        Up,         // 0
        UpDiagonal, // 1 (UpRight, flip for UpLeft)
        Side,       // 2 (Right, flip for Left)
        DownDiagonal, // 3 (DownRight, flip for DownLeft)
        Down        // 4
    }
    
    /// <summary>
    /// Current movement direction as 5-directional enum.
    /// </summary>
    public Direction5 CurrentMoveDirection5 { get; private set; } = Direction5.Down;
    
    /// <summary>
    /// Current aim direction as 5-directional enum.
    /// </summary>
    public Direction5 CurrentAimDirection5 { get; private set; } = Direction5.Down;
    
    /// <summary>
    /// Whether the sprite should be flipped horizontally.
    /// </summary>
    public bool ShouldFlipSprite { get; private set; } = false;
    
    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
    
    void OnEnable()
    {
        GameEvents.OnPlayerMovementInput += HandleMovementInput;
        GameEvents.OnPlayerAimDirectionChanged += HandleAimDirectionChanged;
    }
    
    void OnDisable()
    {
        GameEvents.OnPlayerMovementInput -= HandleMovementInput;
        GameEvents.OnPlayerAimDirectionChanged -= HandleAimDirectionChanged;
    }
    
    private void HandleMovementInput(Vector2 moveInput)
    {
        currentMoveDirection = moveInput;
        isMoving = moveInput.sqrMagnitude > 0.01f;
        
        if (isMoving)
        {
            var result = VectorToDirection5(moveInput);
            CurrentMoveDirection5 = result.direction;
            ShouldFlipSprite = result.flip;
        }
        
        UpdateAnimatorParameters();
        UpdateMovementAnimation();
    }
    
    private void HandleAimDirectionChanged(Vector2 aimDirection)
    {
        currentAimDirection = aimDirection;
        var result = VectorToDirection5(aimDirection);
        CurrentAimDirection5 = result.direction;
        ShouldFlipSprite = result.flip;
        
        UpdateAnimatorParameters();
        UpdateAimAnimation();
    }
    
    private void UpdateAnimatorParameters()
    {
        if (animator == null) return;
        
        // Set movement parameters
        animator.SetFloat(moveXParam, currentMoveDirection.x);
        animator.SetFloat(moveYParam, currentMoveDirection.y);
        animator.SetBool(isMovingParam, isMoving);
        
        // Set aim parameters
        animator.SetFloat(aimXParam, currentAimDirection.x);
        animator.SetFloat(aimYParam, currentAimDirection.y);
    }
    
    /// <summary>
    /// Convert a Vector2 direction to a 5-directional enum with flip flag.
    /// </summary>
    public static (Direction5 direction, bool flip) VectorToDirection5(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
        {
            return (Direction5.Down, false); // Default
        }
        
        // Get angle in degrees (0 = right, 90 = up, etc.)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Normalize to 0-360
        if (angle < 0) angle += 360f;
        
        bool flip = angle > 90f && angle < 270f; // Flip when pointing left
        
        // Determine direction (treat as right-facing, flip sprite for left)
        // Up = 67.5 to 112.5 (no diagonals)
        // UpDiagonal = 22.5 to 67.5 and 112.5 to 157.5
        // Side = 337.5 to 22.5 and 157.5 to 202.5
        // DownDiagonal = 202.5 to 247.5 and 292.5 to 337.5
        // Down = 247.5 to 292.5
        
        if (angle >= 67.5f && angle < 112.5f)
        {
            return (Direction5.Up, false); // Up never flips
        }
        else if ((angle >= 22.5f && angle < 67.5f) || (angle >= 112.5f && angle < 157.5f))
        {
            return (Direction5.UpDiagonal, flip);
        }
        else if ((angle >= 337.5f || angle < 22.5f) || (angle >= 157.5f && angle < 202.5f))
        {
            return (Direction5.Side, flip);
        }
        else if ((angle >= 202.5f && angle < 247.5f) || (angle >= 292.5f && angle < 337.5f))
        {
            return (Direction5.DownDiagonal, flip);
        }
        else // 247.5 to 292.5
        {
            return (Direction5.Down, false); // Down never flips
        }
    }
    
    // ========================================
    // ANIMATION STUBS - Implement when animations are ready
    // ========================================
    
    /// <summary>
    /// Update movement animation based on current direction.
    /// TODO: Implement when 5-directional movement animations are ready.
    /// </summary>
    private void UpdateMovementAnimation()
    {
        // STUB: Play appropriate movement animation based on CurrentMoveDirection5
        // Example implementation:
        // switch (CurrentMoveDirection5)
        // {
        //     case Direction5.Up:
        //         PlayAnimation("Walk_Up");
        //         break;
        //     case Direction5.UpDiagonal:
        //         PlayAnimation("Walk_UpDiagonal");
        //         break;
        //     case Direction5.Side:
        //         PlayAnimation("Walk_Side");
        //         break;
        //     case Direction5.DownDiagonal:
        //         PlayAnimation("Walk_DownDiagonal");
        //         break;
        //     case Direction5.Down:
        //         PlayAnimation("Walk_Down");
        //         break;
        // }
        
        // Alternative: Use blend tree with MoveX/MoveY parameters (already set in UpdateAnimatorParameters)
    }
    
    /// <summary>
    /// Update aim/attack animation based on current aim direction.
    /// Applies sprite flipping when aiming left.
    /// </summary>
    private void UpdateAimAnimation()
    {
        // Apply sprite flipping based on aim direction
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = ShouldFlipSprite;
        }
    }
    
    /// <summary>
    /// Play idle animation for the given direction.
    /// TODO: Implement when idle animations are ready.
    /// </summary>
    public void PlayIdleAnimation(Direction5 direction)
    {
        // STUB: Play idle animation facing the specified direction
        // animator?.Play($"Idle_{direction}");
    }
    
    /// <summary>
    /// Play attack animation for the given direction.
    /// TODO: Implement when attack animations are ready.
    /// </summary>
    public void PlayAttackAnimation(Direction5 direction)
    {
        // STUB: Play attack animation facing the specified direction
        // animator?.Play($"Attack_{direction}");
    }
    
    /// <summary>
    /// Play dash animation for the given direction.
    /// TODO: Implement when dash animations are ready.
    /// </summary>
    public void PlayDashAnimation(Direction5 direction)
    {
        // STUB: Play dash animation facing the specified direction
        // animator?.Play($"Dash_{direction}");
    }
    
    /// <summary>
    /// Play damage/hurt animation.
    /// TODO: Implement when hurt animation is ready.
    /// </summary>
    public void PlayHurtAnimation()
    {
        // STUB: Play hurt animation
        // animator?.SetTrigger("Hurt");
    }
    
    /// <summary>
    /// Play death animation.
    /// TODO: Implement when death animation is ready.
    /// </summary>
    public void PlayDeathAnimation()
    {
        // STUB: Play death animation
        // animator?.SetTrigger("Death");
    }
    
    /// <summary>
    /// Helper to play a named animation state.
    /// </summary>
    private void PlayAnimation(string stateName)
    {
        if (animator != null && !string.IsNullOrEmpty(stateName))
        {
            animator.Play(stateName);
        }
    }
}
