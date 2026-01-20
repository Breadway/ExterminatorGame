using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class FlamethrowerAttack : MonoBehaviour, IWeaponAttack {
    [Header("Flamethrower Settings")]
    [SerializeField] private float damage = 5f; // Damage per tick
    [SerializeField] private float range = 6f;
    [SerializeField] private float coneAngle = 45f; // Degrees
    [SerializeField] private float tickRate = 0.1f; // Damage every 0.1 seconds
    [SerializeField] private float coneRadius = 1.5f; // Cone width at max range
    [SerializeField] private LayerMask damageMask = ~0;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private bool requireLineOfSight = true;
    [SerializeField] private float muzzleProbeDistance = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = false;
    [SerializeField] private float debugDuration = 0.1f;
    
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private ParticleSystem flameParticles;
    
    private PlayerStats stats;
    private float nextTickTime;
    private readonly HashSet<IDamageable> enemiesHitThisFrame = new HashSet<IDamageable>();
    private Coroutine attackCoroutine;
    
    // Pre-allocated arrays for Physics.OverlapSphereNonAlloc to avoid GC
    private const int MAX_HITS = 64;
    private Collider[] hitBuffer = new Collider[MAX_HITS];
    
    // Cached WaitForFixedUpdate to avoid GC allocations
    private WaitForFixedUpdate waitForFixed;
    
    // Pre-calculate frequently used values
    private float maxAngle;
    private float maxAngleRad;
    
    void Awake() {
        stats = GetComponentInParent<PlayerController>().stats;
        waitForFixed = new WaitForFixedUpdate();
        
        // Pre-calculate cone angle values
        maxAngle = coneAngle * 0.5f;
        maxAngleRad = maxAngle * Mathf.Deg2Rad;
    }

    public void Initialize(PlayerStats playerStats) {
        stats = playerStats;
    }
    
    public void Attack() {
        // Start continuous attack loop if not already running
        if (attackCoroutine == null) {
            attackCoroutine = StartCoroutine(AttackLoop());
        }
    }

    private IEnumerator AttackLoop() {
        while (true) {
            if (Time.time >= nextTickTime) {
                PerformAttackTick();
                nextTickTime = Time.time + tickRate;
            }

            // Use cached WaitForFixedUpdate instead of yield return null for more consistent timing
            yield return waitForFixed;
        }
    }

    private void PerformAttackTick()
    {
        enemiesHitThisFrame.Clear();

        Vector3 forward = firePoint.forward;
        Vector3 origin = firePoint.position + forward * muzzleProbeDistance;
        
        if (Physics.Raycast(firePoint.position, forward, out RaycastHit muzzleHit, muzzleProbeDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            origin = muzzleHit.point;
        }

        #if UNITY_EDITOR
        if (drawDebug)
        {
            Vector3 leftDir = Quaternion.Euler(0f, -maxAngle, 0f) * forward;
            Vector3 rightDir = Quaternion.Euler(0f, maxAngle, 0f) * forward;
            Debug.DrawRay(origin, leftDir * range, Color.yellow, debugDuration);
            Debug.DrawRay(origin, rightDir * range, Color.yellow, debugDuration);
            Debug.DrawRay(origin, forward * range, Color.yellow, debugDuration);
            Debug.DrawRay(firePoint.position, forward * muzzleProbeDistance, Color.cyan, debugDuration);
        }
        #endif

        // Use NonAlloc version to avoid GC allocations every tick
        int hitCount = Physics.OverlapSphereNonAlloc(origin, range, hitBuffer, damageMask, QueryTriggerInteraction.Ignore);
        
        float tanMaxAngle = Mathf.Tan(maxAngleRad);
        
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitBuffer[i];
            if (col == null) continue;
            
            var target = col.GetComponent<IDamageable>();
            if (target == null || !target.IsAlive) continue;

            Vector3 toTarget = col.bounds.center - origin;
            float distanceSqr = toTarget.sqrMagnitude;
            float rangeSqr = range * range;
            
            if (distanceSqr <= Mathf.Epsilon || distanceSqr > rangeSqr) continue;

            float distance = Mathf.Sqrt(distanceSqr);
            Vector3 dir = toTarget / distance;
            float angle = Vector3.Angle(forward, dir);
            
            if (angle > maxAngle) continue;

            float coneMaxRadiusAtDistance = tanMaxAngle * distance;
            Vector3 lateral = Vector3.ProjectOnPlane(toTarget, forward);
            if (lateral.sqrMagnitude > (coneMaxRadiusAtDistance + coneRadius) * (coneMaxRadiusAtDistance + coneRadius))
            {
                continue;
            }

            if (requireLineOfSight)
            {
                if (Physics.Raycast(origin, dir, out RaycastHit blockHit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    if (blockHit.collider != col)
                    {
                        #if UNITY_EDITOR
                        if (drawDebug)
                        {
                            Debug.DrawRay(origin, dir * distance, Color.red, debugDuration);
                        }
                        #endif
                        continue;
                    }
                }
            }

            #if UNITY_EDITOR
            if (drawDebug)
            {
                Debug.DrawRay(origin, dir * distance, Color.green, debugDuration);
            }
            #endif

            enemiesHitThisFrame.Add(target);
        }

        float finalDamage = damage; //* stats.currentDamage;
        foreach (IDamageable enemy in enemiesHitThisFrame) {
            enemy.TakeDamage(finalDamage, firePoint.position, firePoint.forward);
        }

        // Emit particles
        if (flameParticles != null && !flameParticles.isPlaying) {
            flameParticles.Play();
        }
    }
    
    public void StopAttack() {
        // Stop continuous attack loop
        if (attackCoroutine != null) {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (flameParticles != null && flameParticles.isPlaying) {
            flameParticles.Stop();
        }
    }
}