using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlamethrowerAttack : MonoBehaviour, IWeaponAttack
{
    [Header("Flamethrower Settings")]
    [SerializeField] private float damage = 5f; // Damage per tick
    [SerializeField] private float range = 6f; // Effective range of the flamethrower
    [SerializeField] private float coneAngle = 45f; // Degrees
    [SerializeField] private float tickRate = 0.1f; // Damage every 0.1 seconds
    [SerializeField] [Tooltip("Layer mask for entities that can be damaged. Should be set to 'Enemy' layer.")]
    private LayerMask damageMask;
    [SerializeField] [Tooltip("Layer mask for obstacles that block line of sight. Should exclude Player and Enemy layers.")]
    private LayerMask obstacleMask;
    [SerializeField] private bool requireLineOfSight = true;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = false;
    [SerializeField] private float debugDuration = 0.1f;
    [Tooltip("Master toggle for all debug logs")]
    [SerializeField] private bool enableDebugLogs = true;
    [Tooltip("Only log every X seconds to reduce spam")]
    [SerializeField] private float debugLogInterval = 0.5f;
    
    [Header("Debug Log Categories")]
    [Tooltip("Log attack origin, direction, range, and layer mask info")]
    [SerializeField] private bool logAttackInfo = true;
    [Tooltip("Log how many colliders were found by overlap check")]
    [SerializeField] private bool logColliderCount = true;
    [Tooltip("Log each collider being checked (name, layer)")]
    [SerializeField] private bool logColliderChecks = false;
    [Tooltip("Log when targets are rejected (no IDamageable, dead, outside cone, etc)")]
    [SerializeField] private bool logRejections = false;
    [Tooltip("Log when damage is successfully dealt")]
    [SerializeField] private bool logDamageDealt = true;
    
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private ParticleSystem flameParticles;
    
    private PlayerStats stats;
    private float nextTickTime;
    private readonly HashSet<IDamageable> enemiesHitThisFrame = new HashSet<IDamageable>();
    private Coroutine attackCoroutine;
    
    // Pre-allocated arrays for Physics2D.OverlapCircle to avoid GC
    private const int MAX_HITS = 128;
    private Collider2D[] hitBuffer = new Collider2D[MAX_HITS];
    
    // Cached WaitForFixedUpdate to avoid GC allocations
    private WaitForFixedUpdate waitForFixed;
    
    // Pre-calculate frequently used values
    private float maxAngle;
    private float maxAngleCos; // Cached cosine for faster cone checks
    private float rangeSqr;    // Cached range squared
    
    // Cached ContactFilter2D to avoid allocation every tick
    private ContactFilter2D contactFilter;
    
    // Debug throttle
    private float nextDebugLogTime;
    
    // Cached component lookups - maps collider instance ID to IDamageable
    private Dictionary<int, IDamageable> damageableCache = new Dictionary<int, IDamageable>(128);
    
    // Cached player collider to skip without GetComponent call
    private Collider2D playerCollider;
    
    // IWeaponAttack.WeaponRange implementation
    public float WeaponRange => range;
    
    void Awake()
    {
        stats = GetComponentInParent<PlayerController>().stats;
        waitForFixed = new WaitForFixedUpdate();
        
        // coneAngle represents TOTAL cone width (e.g., 45° total)
        // maxAngle is half of that for Vector2.Angle comparison (which measures from center)
        maxAngle = coneAngle * 0.5f;
        maxAngleCos = Mathf.Cos(maxAngle * Mathf.Deg2Rad); // Pre-calculate for dot product comparison
        rangeSqr = range * range;
        
        // Cache ContactFilter2D to avoid allocation every tick
        contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = damageMask,
            useTriggers = true
        };
        
        // Cache player collider for fast skip
        var playerController = GetComponentInParent<PlayerController>();
        if (playerController != null)
        {
            playerCollider = playerController.GetComponent<Collider2D>();
        }
        
        ConfigureDamageMask();
        ConfigureObstacleMask();
        
        // Update contactFilter after mask configuration
        contactFilter.layerMask = damageMask;
    }
    
    /// <summary>
    /// Auto-configure damageMask to ONLY target Enemy layer and exclude Player.
    /// </summary>
    private void ConfigureDamageMask()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int playerLayer = LayerMask.NameToLayer("Player");
        
        if (enemyLayer == -1)
        {
            Debug.LogError("[FlamethrowerAttack] 'Enemy' layer not found! Create it in Project Settings > Tags and Layers.", this);
        }
        
        // Auto-configure if not set
        if (damageMask.value == 0 || damageMask.value == -1)
        {
            if (enemyLayer != -1)
            {
                damageMask = 1 << enemyLayer;
                Debug.Log($"[FlamethrowerAttack] DamageMask auto-configured to 'Enemy' layer only (LayerMask: {damageMask.value})", this);
            }
            else
            {
                Debug.LogWarning("[FlamethrowerAttack] Cannot auto-configure DamageMask. Manually set to 'Enemy' layer in Inspector.", this);
            }
        }
        
        // Safety check: ensure Player is NOT in damage mask
        if (playerLayer != -1 && ((damageMask.value & (1 << playerLayer)) != 0))
        {
            Debug.LogError("[FlamethrowerAttack] DamageMask includes Player layer! Removing to prevent self-damage.", this);
            damageMask &= ~(1 << playerLayer);
        }
        
        Debug.Log($"[FlamethrowerAttack] Final DamageMask: {damageMask.value} (Layers: {GetLayerNames(damageMask)})", this);
    }
    
    /// <summary>
    /// Auto-configure obstacleMask to exclude Player and Enemy layers.
    /// Obstacles should only be walls/terrain, not characters.
    /// </summary>
    private void ConfigureObstacleMask()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int playerLayer = LayerMask.NameToLayer("Player");
        
        // Auto-configure if not set
        if (obstacleMask.value == 0 || obstacleMask.value == -1)
        {
            obstacleMask = ~0;
            if (playerLayer != -1)
            {
                obstacleMask &= ~(1 << playerLayer); // Exclude Player
            }
            if (enemyLayer != -1)
            {
                obstacleMask &= ~(1 << enemyLayer); // Exclude Enemy
            }
            Debug.Log($"[FlamethrowerAttack] ObstacleMask auto-configured to exclude Player and Enemy layers (LayerMask: {obstacleMask.value})", this);
        }
        else
        {
            // Verify Player and Enemy are NOT in obstacle mask
            if (playerLayer != -1 && ((obstacleMask.value & (1 << playerLayer)) != 0))
            {
                Debug.LogWarning($"[FlamethrowerAttack] ObstacleMask includes 'Player' layer! This will block line of sight. Removing Player layer from mask.", this);
                obstacleMask &= ~(1 << playerLayer);
            }
            if (enemyLayer != -1 && ((obstacleMask.value & (1 << enemyLayer)) != 0))
            {
                Debug.LogWarning($"[FlamethrowerAttack] ObstacleMask includes 'Enemy' layer! This will block line of sight. Removing Enemy layer from mask.", this);
                obstacleMask &= ~(1 << enemyLayer);
            }
        }
        
        Debug.Log($"[FlamethrowerAttack] Final DamageMask value: {damageMask.value} (Layers: {GetLayerNames(damageMask)})", this);
        Debug.Log($"[FlamethrowerAttack] Final ObstacleMask value: {obstacleMask.value} (Layers: {GetLayerNames(obstacleMask)})", this);
    }
    
    void OnEnable()
    {
        GameEvents.OnEnemyDamaged += HandleEnemyDamaged;
    }
    
    void OnDisable()
    {
        GameEvents.OnEnemyDamaged -= HandleEnemyDamaged;
    }
    
    void HandleEnemyDamaged(float amount, Vector2 hitPoint, Vector2 hitDirection)
    {
        if (enableDebugLogs && logDamageDealt)
        {
            GameEvents.DebugLog($"[FlamethrowerAttack] Enemy damaged event received: {amount} damage at {hitPoint}");
        }
    }
    
    /// <summary>
    /// Helper to convert LayerMask to readable layer names for debugging.
    /// </summary>
    private string GetLayerNames(LayerMask mask)
    {
        List<string> layers = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                layers.Add(string.IsNullOrEmpty(layerName) ? $"Unnamed({i})" : layerName);
            }
        }
        return layers.Count > 0 ? string.Join(", ", layers) : "None";
    }

    public void Initialize(PlayerStats playerStats)
    {
        stats = playerStats;
    }
    
    public void Attack()
    {
        if (attackCoroutine == null)
        {
            attackCoroutine = StartCoroutine(AttackLoop());
        }
    }

    private IEnumerator AttackLoop()
    {
        while (true)
        {
            if (Time.time >= nextTickTime)
            {
                PerformAttackTick();
                nextTickTime = Time.time + tickRate;
            }

            yield return waitForFixed;
        }
    }

    private void PerformAttackTick()
    {
        enemiesHitThisFrame.Clear();

        Vector2 forward = firePoint.up;
        Vector2 origin = firePoint.position;

        // Throttled debug logging to avoid console spam
        bool shouldLog = enableDebugLogs && Time.time >= nextDebugLogTime;
        if (shouldLog)
        {
            nextDebugLogTime = Time.time + debugLogInterval;
            if (logAttackInfo)
            {
                GameEvents.DebugLog($"[FlamethrowerAttack] Origin: {origin}, Forward: {forward}, Range: {range}, DamageMask: {damageMask.value}");
                GameEvents.DebugLog($"[FlamethrowerAttack] FirePoint position: {firePoint.position}, rotation: {firePoint.rotation.eulerAngles.z:F1}°");
                GameEvents.DebugLog($"[FlamethrowerAttack] FirePoint.up (forward): {firePoint.up}, FirePoint.right: {firePoint.right}");
                GameEvents.DebugLog($"[FlamethrowerAttack] Cone angle: {coneAngle}° (half-angle: {maxAngle:F1}°)");
            }
        }

        #if UNITY_EDITOR
        if (drawDebug)
        {
            Vector2 leftDir = Rotate2D(forward, -maxAngle);
            Vector2 rightDir = Rotate2D(forward, maxAngle);
            Debug.DrawRay(origin, leftDir * range, Color.yellow, debugDuration);
            Debug.DrawRay(origin, rightDir * range, Color.yellow, debugDuration);
            Debug.DrawRay(origin, forward * range, Color.cyan, debugDuration);
        }
        #endif

        // Use cached ContactFilter2D to avoid allocation every tick
        int hitCount = Physics2D.OverlapCircle(origin, range, contactFilter, hitBuffer);
        if (shouldLog && logColliderCount)
        {
            GameEvents.DebugLog($"[FlamethrowerAttack] OverlapCircle found {hitCount} colliders");
            
            if (hitCount == 0)
            {
                GameEvents.DebugLog($"[FlamethrowerAttack] No colliders found! Check: 1) Enemy has Collider2D, 2) DamageMask includes enemy layer, 3) Project Settings > Physics 2D > Queries Hit Triggers is enabled if using triggers");
            }
        }
        
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = hitBuffer[i];
            if (col == null) continue;
            
            // Fast skip player using cached reference (no GetComponent call)
            if (col == playerCollider)
            {
                continue;
            }
            
            if (shouldLog && logColliderChecks)
            {
                GameEvents.DebugLog($"[FlamethrowerAttack] Checking: {col.gameObject.name} on layer {LayerMask.LayerToName(col.gameObject.layer)} (index: {col.gameObject.layer})");
            }
            
            // Use cached IDamageable lookup to avoid GetComponent every tick
            int colId = col.GetInstanceID();
            if (!damageableCache.TryGetValue(colId, out IDamageable target))
            {
                target = col.GetComponent<IDamageable>();
                damageableCache[colId] = target; // Cache even null results
            }
            
            if (target == null)
            {
                if (shouldLog && logRejections)
                {
                    GameEvents.DebugLog($"[FlamethrowerAttack] {col.gameObject.name} has no IDamageable component");
                }
                continue;
            }
            
            if (!target.IsAlive)
            {
                if (shouldLog && logRejections)
                {
                    GameEvents.DebugLog($"[FlamethrowerAttack] {col.gameObject.name} is not alive");
                }
                continue;
            }

            // Use bounds.center only once
            Vector2 targetCenter = col.bounds.center;
            Vector2 toTarget = targetCenter - origin;
            float distanceSqr = toTarget.sqrMagnitude;
            
            // Early exit using cached rangeSqr
            if (distanceSqr <= Mathf.Epsilon || distanceSqr > rangeSqr) continue;

            // Use fast inverse sqrt approximation for normalization
            float invDistance = FastInvSqrt(distanceSqr);
            Vector2 dir = toTarget * invDistance;
            
            // Use dot product instead of Vector2.Angle (avoids expensive acos)
            float dot = Vector2.Dot(forward, dir);
            
            if (shouldLog && (logColliderChecks || logRejections))
            {
                float distance = 1f / invDistance;
                float angle = Mathf.Acos(Mathf.Clamp01(dot)) * Mathf.Rad2Deg;
                GameEvents.DebugLog($"[FlamethrowerAttack] {col.gameObject.name}:");
                GameEvents.DebugLog($"  Target pos: {targetCenter}, Origin: {origin}");
                GameEvents.DebugLog($"  ToTarget: {toTarget} (dist: {distance:F2})");
                GameEvents.DebugLog($"  Forward: {forward}, DirToTarget: {dir}");
                GameEvents.DebugLog($"  Angle: {angle:F1}° vs MaxAngle: {maxAngle:F1}° (cone total: {coneAngle}°)");
            }
            
            // Compare dot product to cached cosine (faster than angle comparison)
            // Higher dot = smaller angle, so target is in cone if dot >= maxAngleCos
            if (dot < maxAngleCos)
            {
                if (shouldLog && logRejections)
                {
                    float angle = Mathf.Acos(Mathf.Clamp01(dot)) * Mathf.Rad2Deg;
                    GameEvents.DebugLog($"[FlamethrowerAttack] {col.gameObject.name} outside cone angle ({angle:F1} > {maxAngle:F1})");
                }
                continue;
            }

            if (requireLineOfSight)
            {
                float distance = 1f / invDistance;
                RaycastHit2D blockHit = Physics2D.Raycast(origin, dir, distance, obstacleMask);
                if (blockHit.collider != null && blockHit.collider != col)
                {
                    if (shouldLog && logRejections)
                    {
                        GameEvents.DebugLog($"[FlamethrowerAttack] {col.gameObject.name} blocked by {blockHit.collider.gameObject.name} (distance: {blockHit.distance:F2})");
                    }
                    #if UNITY_EDITOR
                    if (drawDebug)
                    {
                        Debug.DrawRay(origin, dir * distance, Color.red, debugDuration);
                    }
                    #endif
                    continue;
                }
                else if (shouldLog && logColliderChecks)
                {
                    GameEvents.DebugLog($"[FlamethrowerAttack] {col.gameObject.name} has clear line of sight");
                }
                
                #if UNITY_EDITOR
                if (drawDebug)
                {
                    Debug.DrawRay(origin, dir * distance, Color.green, debugDuration);
                }
                #endif
            }

            if (shouldLog && logColliderChecks)
            {
                GameEvents.DebugLog($"[FlamethrowerAttack] {col.gameObject.name} PASSED all checks, adding to hit list");
            }
            enemiesHitThisFrame.Add(target);
        }

        // Only process damage if we hit something
        if (enemiesHitThisFrame.Count > 0)
        {
            float finalDamage = damage;
            if (shouldLog && logDamageDealt)
            {
                GameEvents.DebugLog($"[FlamethrowerAttack] Dealing {finalDamage} damage to {enemiesHitThisFrame.Count} targets");
            }
            
            // Cache firePoint values to avoid repeated property access
            Vector3 firePos = firePoint.position;
            Vector3 fireUp = firePoint.up;
            
            foreach (IDamageable enemy in enemiesHitThisFrame)
            {
                enemy.TakeDamage(finalDamage, firePos, fireUp);
                if (enableDebugLogs && logDamageDealt)
                {
                    GameEvents.DebugLog($"[FlamethrowerAttack] Damaged: {(enemy as Component)?.gameObject.name ?? "unknown"}");
                }
            }
        }

        if (flameParticles != null && !flameParticles.isPlaying)
        {
            flameParticles.Play();
        }
    }
    
    /// <summary>
    /// Fast inverse square root approximation (Quake III style).
    /// Good enough for direction normalization, avoids expensive sqrt.
    /// </summary>
    private static float FastInvSqrt(float x)
    {
        float xhalf = 0.5f * x;
        int i = System.BitConverter.SingleToInt32Bits(x);
        i = 0x5f3759df - (i >> 1);
        x = System.BitConverter.Int32BitsToSingle(i);
        x = x * (1.5f - xhalf * x * x); // One Newton-Raphson iteration
        return x;
    }
    
    /// <summary>
    /// Clear the damageable cache when enemies are destroyed/pooled.
    /// Call this from object pool or when enemies die.
    /// </summary>
    public void InvalidateCache(Collider2D col)
    {
        if (col != null)
        {
            damageableCache.Remove(col.GetInstanceID());
        }
    }
    
    /// <summary>
    /// Clear entire cache (call on scene load or when many enemies die at once).
    /// </summary>
    public void ClearCache()
    {
        damageableCache.Clear();
    }
    
    /// <summary>
    /// Helper to rotate a 2D vector by an angle in degrees.
    /// </summary>
    private Vector2 Rotate2D(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }
    
    public void StopAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (flameParticles != null && flameParticles.isPlaying)
        {
            flameParticles.Stop();
        }
    }
}