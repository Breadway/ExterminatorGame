using UnityEngine;

/// <summary>
/// 2D flying movement behavior using Rigidbody2D physics.
/// Can hover and move freely without ground constraints.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FlyingMovement : MonoBehaviour, IMovementBehavior
{
    private Rigidbody2D rb;
    private Transform target;
    private EnemyData enemyData;
    private float moveSpeed = 5f;

    [Header("Movement Settings")]
    [SerializeField] private float stoppingDistance = 1f;
    [SerializeField] private bool rotateToVelocity = true;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("Separation (Anti-Clumping)")]
    [SerializeField] private float separationRadius = 1.2f;
    [SerializeField] private float separationStrength = 0.6f;
    [SerializeField] private LayerMask enemyLayerMask;

    private static Transform cachedPlayer;
    private bool isStopped = false;
    
    // Pre-allocated buffer for overlap checks (avoid GC)
    private static readonly Collider2D[] separationBuffer = new Collider2D[16];
    private static int enemyLayer = -1;

    public void Initialize(EnemyData enemyData)
    {
        this.enemyData = enemyData;
        if (enemyData != null)
        {
            moveSpeed = enemyData.moveSpeed;
        }
        isStopped = false;
    }

    void Awake()
    {
        // Cache player once to avoid repeated scene searches
        if (cachedPlayer == null)
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            cachedPlayer = player != null ? player.transform : null;
        }

        target = cachedPlayer;
        rb = GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        
        // Cache enemy layer for separation checks
        if (enemyLayer == -1)
        {
            enemyLayer = LayerMask.NameToLayer("Enemy");
        }
        
        // Auto-configure layer mask if not set
        if (enemyLayerMask.value == 0 && enemyLayer != -1)
        {
            enemyLayerMask = 1 << enemyLayer;
        }
    }

    public void UpdateMovement()
    {
        if (target == null || rb == null || isStopped)
        {
            return;
        }

        Vector2 currentPos = rb.position;
        Vector2 targetPos = target.position;
        Vector2 direction = targetPos - currentPos;
        float distance = direction.magnitude;

        // Stop when close enough to target
        if (distance <= stoppingDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Calculate chase direction
        Vector2 moveDirection = direction / distance; // Already have magnitude, avoid extra sqrt
        
        // Add separation force to prevent clumping
        Vector2 separation = CalculateSeparation(currentPos);
        moveDirection = (moveDirection + separation).normalized;
        
        rb.linearVelocity = moveDirection * moveSpeed;

        // Rotate to face movement direction
        if (rotateToVelocity && moveDirection.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Calculate separation vector to steer away from nearby enemies.
    /// Uses a shared static buffer to avoid GC allocations.
    /// </summary>
    private Vector2 CalculateSeparation(Vector2 currentPos)
    {
        if (separationStrength <= 0f) return Vector2.zero;
        
        int neighborCount = Physics2D.OverlapCircleNonAlloc(currentPos, separationRadius, separationBuffer, enemyLayerMask);
        
        if (neighborCount <= 1) return Vector2.zero; // Only self or nothing
        
        Vector2 separationForce = Vector2.zero;
        int actualNeighbors = 0;
        
        for (int i = 0; i < neighborCount; i++)
        {
            Collider2D col = separationBuffer[i];
            if (col == null || col.gameObject == gameObject) continue;
            
            Vector2 neighborPos = col.transform.position;
            Vector2 away = currentPos - neighborPos;
            float distSqr = away.sqrMagnitude;
            
            if (distSqr > 0.001f && distSqr < separationRadius * separationRadius)
            {
                // Weight by inverse distance (closer = stronger push)
                float dist = Mathf.Sqrt(distSqr);
                separationForce += away / dist * (1f - dist / separationRadius);
                actualNeighbors++;
            }
        }
        
        if (actualNeighbors > 0)
        {
            separationForce /= actualNeighbors;
            separationForce *= separationStrength;
        }
        
        return separationForce;
    }
    
    public void SetSpeedMultiplier(float multiplier)
    {
        moveSpeed = enemyData != null ? enemyData.moveSpeed * multiplier : moveSpeed * multiplier;
    }

    public void Stop()
    {
        isStopped = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}  