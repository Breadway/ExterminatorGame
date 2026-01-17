using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public class ChaseMovement : MonoBehaviour, IMovementBehavior {
    public Transform target;
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
    
    public void Initialize(EnemyData enemyData) {
        this.enemyData = enemyData;
        ApplyMovementSettings();
    }

    void Awake()
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if (player != null) {
            target = player.transform;
        } else {
            Debug.LogWarning("PlayerController not found in the scene.");
            target = null;
        }
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        ApplyMovementSettings();
        
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
        if (target != null && agent != null) {
            agent.destination = target.position;
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
    }

    public void Stop()
    {
        
    }
}