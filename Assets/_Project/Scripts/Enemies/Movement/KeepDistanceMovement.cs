using UnityEngine;

/// <summary>
/// Flies toward the player until preferredRange, backs off if the player closes in, and strafes
/// in between so ranged enemies are not stationary targets.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class KeepDistanceMovement : MonoBehaviour, IMovementBehavior
{
    private Rigidbody2D rb;
    private Transform target;
    private EnemyData data;
    private float baseSpeed = 4f;
    private float speedMultiplier = 1f;
    private bool stopped;
    private float strafeSign = 1f;
    private float nextFlip;

    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Transform visual;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        if (visual == null) visual = transform;
    }

    public void Initialize(EnemyData enemyData)
    {
        data = enemyData;
        if (enemyData != null) baseSpeed = enemyData.moveSpeed;
        stopped = false;
        strafeSign = Random.value < 0.5f ? -1f : 1f;
        nextFlip = Time.time + Random.Range(1.5f, 3.5f);
    }

    public void UpdateMovement()
    {
        if (stopped || rb == null) return;
        if (target == null)
        {
            if (PlayerController.Instance == null) return;
            target = PlayerController.Instance.transform;
        }

        Vector2 toPlayer = (Vector2)target.position - rb.position;
        float distance = toPlayer.magnitude;
        if (distance < 0.01f) return;
        Vector2 dir = toPlayer / distance;

        float preferred = data != null ? data.preferredRange : 6f;
        Vector2 move;
        if (distance > preferred + 1f) move = dir;
        else if (distance < preferred - 1.5f) move = -dir;
        else
        {
            if (Time.time >= nextFlip)
            {
                strafeSign = -strafeSign;
                nextFlip = Time.time + Random.Range(1.5f, 3.5f);
            }
            move = new Vector2(-dir.y, dir.x) * strafeSign;
        }

        float speed = baseSpeed * speedMultiplier;
        rb.linearVelocity = move * speed;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.Euler(0f, 0f, angle), rotationSpeed * Time.fixedDeltaTime);
    }

    public void SetSpeedMultiplier(float multiplier) { speedMultiplier = multiplier; }

    public void Stop()
    {
        stopped = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
