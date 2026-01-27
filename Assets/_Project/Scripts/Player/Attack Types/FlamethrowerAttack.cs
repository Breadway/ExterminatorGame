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
    
    // Pre-allocated arrays for Physics2D.OverlapCircleNonAlloc to avoid GC
    private const int MAX_HITS = 64;
    private Collider2D[] hitBuffer = new Collider2D[MAX_HITS];
    
    // Cached WaitForFixedUpdate to avoid GC allocations
    private WaitForFixedUpdate waitForFixed;
    
    // Pre-calculate frequently used values
    private float maxAngle;
    
    void Awake() {
        stats = GetComponentInParent<PlayerController>().stats;
        waitForFixed = new WaitForFixedUpdate();
        
        // Pre-calculate cone angle values
        maxAngle = coneAngle * 0.5f;
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

        Vector2 forward = firePoint.up;
        Vector2 origin = (Vector2)firePoint.position + forward * muzzleProbeDistance;
        
        RaycastHit2D muzzleHit = Physics2D.Raycast(firePoint.position, forward, muzzleProbeDistance, obstacleMask);
        if (muzzleHit.collider != null)
        {
            origin = muzzleHit.point;
        }

        #if UNITY_EDITOR
        if (drawDebug)
        {
            Vector2 leftDir = Rotate2D(forward, -maxAngle);
            Vector2 rightDir = Rotate2D(forward, maxAngle);
            Debug.DrawRay(origin, leftDir * range, Color.yellow, debugDuration);
            Debug.DrawRay(origin, rightDir * range, Color.yellow, debugDuration);
            Debug.DrawRay(origin, forward * range, Color.yellow, debugDuration);
            Debug.DrawRay(firePoint.position, forward * muzzleProbeDistance, Color.cyan, debugDuration);
        }
        #endif

        // Use NonAlloc version to avoid GC allocations every tick
        int hitCount = Physics2D.OverlapCircleNonAlloc(origin, range, hitBuffer, damageMask);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = hitBuffer[i];
            if (col == null) continue;
            
            var target = col.GetComponent<IDamageable>();
            if (target == null || !target.IsAlive) continue;

            Vector2 toTarget = (Vector2)col.bounds.center - origin;
            float distanceSqr = toTarget.sqrMagnitude;
            float rangeSqr = range * range;
            
            if (distanceSqr <= Mathf.Epsilon || distanceSqr > rangeSqr) continue;

            float distance = Mathf.Sqrt(distanceSqr);
            Vector2 dir = toTarget / distance;
            float angle = Vector2.Angle(forward, dir);
            
            if (angle > maxAngle) continue;

            if (requireLineOfSight)
            {
                RaycastHit2D blockHit = Physics2D.Raycast(origin, dir, distance, obstacleMask);
                if (blockHit.collider != null && blockHit.collider != col)
                {
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
            enemy.TakeDamage(finalDamage, firePoint.position, firePoint.up);
        }

        // Emit particles
        if (flameParticles != null && !flameParticles.isPlaying) {
            flameParticles.Play();
        }
    }
    
    // Helper method to rotate a 2D vector by an angle in degrees
    private Vector2 Rotate2D(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
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