using UnityEngine;

/// <summary>
/// 2D chase movement using Rigidbody2D physics.
/// Moves directly toward the target player position.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ChaseMovement : MonoBehaviour, IMovementBehavior
{
    [SerializeField] private Transform target;
    private Rigidbody2D rb;
    private EnemyData enemyData;
    private float moveSpeed = 5f;

    [Header("Movement Settings")]
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private bool rotateToVelocity = true;
    [SerializeField] private float rotationSpeed = 12f;

    private static Transform cachedPlayer;
    private bool isStopped = false;

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
        // Cache player once to avoid repeated scene searches on mass spawns.
        if (cachedPlayer == null)
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            cachedPlayer = player != null ? player.transform : null;
        }

        if (target == null)
        {
            target = cachedPlayer;
            if (target == null)
            {
                Debug.LogWarning("PlayerController not found in the scene.");
            }
        }

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
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

        // Move toward target
        Vector2 moveDirection = direction.normalized;
        rb.linearVelocity = moveDirection * moveSpeed;

        // Rotate to face movement direction
        if (rotateToVelocity && moveDirection.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
            Quaternion targetRot = Quaternion.Euler(0f, 0f, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }
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