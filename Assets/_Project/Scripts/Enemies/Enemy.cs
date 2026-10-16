using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyAttack))]
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour, IPoolable, IMovementModifiable {

    [Header("Components")]
    [SerializeField] protected Health health;
    IMovementBehavior movement;
    [SerializeField] Rigidbody2D rb;
    EnemyAttack attack;
    IAttackBehaviour attackBehavior;
    SpriteRenderer visual;
    [Header("Data")]
    [SerializeField] private EnemyData enemyData;
    public EnemyData EnemyData { get => enemyData; set => enemyData = value; }
    public bool IsBoss => enemyData != null && enemyData.isBoss;
    public Health Health => health;

    /// <summary>Per-spawn multipliers from the room/wave. Reset every spawn.</summary>
    public float XPMultiplier { get; private set; } = 1f;
    /// <summary>Room-scaling damage multiplier (late rooms hit harder).</summary>
    public float DamageMultiplier { get; private set; } = 1f;
    private float waveSpeedMultiplier = 1f;
    private float statusSpeedMultiplier = 1f;
    private float waveHealthMultiplier = 1f;

    // Cache for pooling
    private bool isInitialized = false;
    private bool dataInitialized;
    private float evolveAt = float.PositiveInfinity;
    private Vector3 prefabScale = Vector3.one;
    private bool scaleCaptured;

    void FixedUpdate()
    {
        if (movement != null)
        {
            movement.UpdateMovement();
        }
    }

    void Update()
    {
        if (Time.time >= evolveAt) Evolve();
        // Restore full speed once a hazard slow runs out, without waiting for another event
        if (hazardSlowUntil > 0f && Time.time >= hazardSlowUntil) { hazardSlowUntil = 0f; hazardSlow = 1f; PushSpeed(); }
    }

    void Awake() {
        CacheComponents();
        ConfigureRigidbody();
        ApplyEnemyData();
    }

    private void ConfigureRigidbody()
    {
        if (rb != null)
        {
            // Make enemy kinematic to prevent physics-based pushing/vibration
            // Enemy movement is handled by IMovementBehavior, not physics forces
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true; // Still detect collisions for damage
        }
    }

    private void CacheComponents()
    {
        if (isInitialized) return;

        health = GetComponent<Health>();
        attack = GetComponent<EnemyAttack>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<IMovementBehavior>();
        attackBehavior = GetComponent<IAttackBehaviour>();
        visual = GetComponentInChildren<SpriteRenderer>();
        if (!scaleCaptured)
        {
            prefabScale = transform.localScale;
            scaleCaptured = true;
        }

        isInitialized = true;
    }

    void ApplyEnemyData()
    {
        if (enemyData != null && health != null)
        {
            health.SetMaxHP(enemyData.maxHealth * waveHealthMultiplier);
            health.SetDamageReduction(enemyData.damageReduction);
        }
        ApplyLook();
    }

    // Sprite and size come from data so one prefab can serve several enemy types.
    private void ApplyLook()
    {
        if (enemyData == null) return;
        if (visual != null && enemyData.sprite != null) visual.sprite = enemyData.sprite;
        transform.localScale = prefabScale * Mathf.Max(0.1f, enemyData.scale);
        var circle = GetComponent<CircleCollider2D>();
        if (circle != null && enemyData.colliderRadius > 0f) circle.radius = enemyData.colliderRadius;
    }

    void Start()
    {
        if (enemyData != null && !dataInitialized)
        {
            Initialize(enemyData);
        }
        else if (enemyData == null)
        {
            GameEvents.DebugLog("Enemy has no EnemyData assigned.", DebugCategory.EnemyAI);
        }
    }

    /// <summary>
    /// Spawn-time configuration. Safe to call on a pooled enemy that was last used as another type.
    /// </summary>
    public void Configure(EnemyData data, float healthMultiplier = 1f, float speedMultiplier = 1f, float xpMultiplier = 1f, float damageMultiplier = 1f)
    {
        CacheComponents();
        waveHealthMultiplier = Mathf.Max(0.1f, healthMultiplier);
        waveSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
        XPMultiplier = Mathf.Max(0f, xpMultiplier);
        DamageMultiplier = Mathf.Max(0.1f, damageMultiplier);
        statusSpeedMultiplier = 1f;
        Initialize(data != null ? data : enemyData);
    }

    public void Initialize(EnemyData enemyData)
    {
        CacheComponents();
        this.enemyData = enemyData;
        dataInitialized = true;
        ApplyEnemyData();
        health.ResetHealth();
        evolveAt = enemyData != null && enemyData.evolvesInto != null
            ? Time.time + Mathf.Max(0.5f, enemyData.evolveAfterSeconds)
            : float.PositiveInfinity;

        if (attack == null)
        {
            GameEvents.DebugLog("Enemy missing EnemyAttack component.", DebugCategory.EnemyAI);
        }
        else if (attackBehavior == null)
        {
            GameEvents.DebugLog("Enemy missing IAttackBehaviour component.", DebugCategory.EnemyAI);
        }
        else
        {
            attack.Initialize(this.enemyData, attackBehavior);
        }

        if (movement == null)
        {
            GameEvents.DebugLog("Enemy missing IMovementBehavior component.", DebugCategory.EnemyAI);
        }
        else
        {
            movement.Initialize(this.enemyData);
            movement.SetSpeedMultiplier(waveSpeedMultiplier * statusSpeedMultiplier);
        }
    }

    /// <summary>
    /// Turn into the next form in place, keeping the same fraction of health. Doing it in
    /// place keeps wave bookkeeping intact because this is still the same Enemy instance.
    /// </summary>
    private void Evolve()
    {
        evolveAt = float.PositiveInfinity;
        var next = enemyData != null ? enemyData.evolvesInto : null;
        if (next == null || health == null || !health.IsAlive) return;

        float fraction = health.HealthPercent;
        enemyData = next;
        ApplyEnemyData();
        health.SetMaxHP(next.maxHealth * waveHealthMultiplier);
        health.Heal(health.MaxHP * fraction - health.CurrentHP);
        attack?.Initialize(enemyData, attackBehavior);
        movement?.Initialize(enemyData);
        movement?.SetSpeedMultiplier(waveSpeedMultiplier * statusSpeedMultiplier);
        evolveAt = next.evolvesInto != null ? Time.time + Mathf.Max(0.5f, next.evolveAfterSeconds) : float.PositiveInfinity;
        GameEvents.DebugLog($"[Enemy] {name} evolved into {next.enemyName}", DebugCategory.EnemyAI);
    }

    void OnEnable() {
        health.OnDied += HandleDeath;
        health.OnDamaged += HandleDamage;
    }

    void OnDisable() {
        health.OnDied -= HandleDeath;
        health.OnDamaged -= HandleDamage;
    }

    void HandleDamage(float amount, Vector2 hitPoint, Vector2 hitDirection) {
        // Fire GameEvent for damage tracking, VFX, audio, etc.
        GameEvents.EnemyDamaged(amount, hitPoint, hitDirection);
        GameEvents.EnemyHit(this, amount);
        GameEvents.DebugLog($"[Enemy] {gameObject.name} took {amount} damage. HP: {health.CurrentHP}/{health.MaxHP}", DebugCategory.EnemyAI);
    }

    void HandleDeath() {
        GameEvents.EnemyKilled(this);
        if (IsBoss) GameEvents.BossDefeated(enemyData.enemyName);
        if (enemyData != null && enemyData.spawnHazardOnDeath && HazardField.Instance != null)
        {
            HazardField.Instance.Spawn(enemyData.deathHazard, transform.position, enemyData.deathHazardRadius,
                enemyData.deathHazardLifetime, enemyData.deathHazardDps, true, true);
        }

        // Return to pool instead of destroying
        if (PoolingSystem.Instance != null)
        {
            PoolingSystem.Instance.Return(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // IPoolable implementation for object pooling
    public void OnSpawnFromPool()
    {
        CacheComponents();

        // Re-enable Rigidbody2D
        if (rb != null)
        {
            rb.simulated = true;
        }

        // Reset to prefab defaults; the spawner then calls Configure for the real type.
        waveHealthMultiplier = 1f;
        waveSpeedMultiplier = 1f;
        DamageMultiplier = 1f;
        statusSpeedMultiplier = 1f;
        hazardSlow = 1f;
        hazardSlowUntil = 0f;
        XPMultiplier = 1f;
        if (enemyData != null)
        {
            Initialize(enemyData);
        }
    }

    private float hazardSlow = 1f;
    private float hazardSlowUntil;

    public void SetSpeedMultiplier(float multiplier)
    {
        statusSpeedMultiplier = multiplier;
        PushSpeed();
    }

    /// <summary>Slow from floor hazards (webs, acid). Expires on its own and stacks with status effects.</summary>
    public void ApplyHazardSlow(float multiplier, float seconds)
    {
        hazardSlow = Mathf.Min(Time.time < hazardSlowUntil ? hazardSlow : 1f, Mathf.Clamp(multiplier, 0.1f, 1f));
        hazardSlowUntil = Time.time + seconds;
        PushSpeed();
    }

    private void PushSpeed()
    {
        float h = Time.time < hazardSlowUntil ? hazardSlow : 1f;
        if (movement != null) movement.SetSpeedMultiplier(waveSpeedMultiplier * statusSpeedMultiplier * h);
    }

    /// <summary>Boss enrage and similar effects scale speed without touching status slows.</summary>
    public void SetWaveSpeedMultiplier(float multiplier)
    {
        waveSpeedMultiplier = Mathf.Max(0.1f, multiplier);
        movement?.SetSpeedMultiplier(waveSpeedMultiplier * statusSpeedMultiplier);
    }

    public void OnReturnToPool()
    {
        evolveAt = float.PositiveInfinity;

        // Stop Rigidbody2D to prevent errors
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Stop movement
        if (movement != null)
        {
            movement.Stop();
        }
    }
}
