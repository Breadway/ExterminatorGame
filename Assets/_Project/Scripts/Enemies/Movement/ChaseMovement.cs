using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public class ChaseMovement : MonoBehaviour, IMovementBehavior {
    [SerializeField] private Transform target;
    private Rigidbody rb;
    private NavMeshAgent agent;
    private EnemyData enemyData;

    [Header("Agent Tuning")]
    [SerializeField] private float angularSpeed = 720f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float stoppingDistance = 0.2f;
    [SerializeField] private bool makeRigidbodyKinematic = true;
    [SerializeField] private bool rotateToVelocity = true;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Path Update")]
    [SerializeField, Tooltip("Seconds between path recalculations.")]
    private float repathInterval = 0.2f;
    [SerializeField, Tooltip("Minimum distance change before updating destination.")]
    private float repathDistanceThreshold = 0.25f;

    private float nextRepathTime;
    private Vector3 lastDestination;
    private static Transform cachedPlayer;
    
    public void Initialize(EnemyData enemyData) {
        this.enemyData = enemyData;
        ApplyMovementSettings();
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

        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        ApplyMovementSettings();
        lastDestination = Vector3.positiveInfinity; // force first set
    }

    private void ApplyMovementSettings()
    {
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            if (makeRigidbodyKinematic)
            {
                rb.isKinematic = true;
            }
        }

        if (agent != null)
        {
            agent.speed = enemyData != null ? enemyData.moveSpeed : 20f;
            agent.angularSpeed = angularSpeed;
            agent.acceleration = acceleration;
            agent.stoppingDistance = stoppingDistance;
            agent.updateRotation = !rotateToVelocity;
        }
    }
    
    public void UpdateMovement()
    {
        if (target == null || agent == null)
        {
            return;
        }

        // Throttle path recalculation to reduce NavMesh cost.
        if (Time.time >= nextRepathTime)
        {
            Vector3 desired = target.position;
            if ((desired - lastDestination).sqrMagnitude >= repathDistanceThreshold * repathDistanceThreshold)
            {
                agent.SetDestination(desired);
                lastDestination = desired;
                nextRepathTime = Time.time + repathInterval;
            }
        }

        if (rotateToVelocity)
        {
            Vector3 flatVelocity = agent.velocity;
            flatVelocity.y = 0f;
            if (flatVelocity.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(flatVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }
    }

    public void Stop()
    {
        if (agent != null)
        {
            agent.ResetPath();
        }
    }
}