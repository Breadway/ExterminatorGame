using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveSpawner : MonoBehaviour {
    public static WaveSpawner Instance { get; private set; }

    [Header("Wave Settings")]
    [Tooltip("Generic enemy prefab. Used for every EnemyData that has no prefab of its own.")]
    [SerializeField] private GameObject scorpionPrefab;
    [Tooltip("Enemy types used by the built-in endless waves. Empty = the prefab's own data.")]
    [SerializeField] private EnemyData[] defaultTable;
    [SerializeField] private int baseEnemiesPerWave = 4;
    [SerializeField] private float enemyIncreasePerWave = 1.3f;
    [SerializeField] private float timeBetweenWaves = 3f;

    [Header("Spawn Area")]
    [SerializeField] private float spawnRadius = 12f;
    [SerializeField] private float minSpawnDistance = 6f;
    [SerializeField] private Vector2 arenaSize = new Vector2(25f, 20f);

    [Header("Spawn Patterns")]
    [Tooltip("How much of the circle around player is blocked for spawning (0-1). 0.5 = enemies spawn in 180 degree arc")]
    [SerializeField] [Range(0.2f, 0.8f)] private float safeArcRatio = 0.35f;
    [Tooltip("Chance to spawn from arena edges instead of around player")]
    [SerializeField] [Range(0f, 1f)] private float edgeSpawnChance = 0.6f;
    [Tooltip("Chance to spawn enemies in clusters")]
    [SerializeField] [Range(0f, 1f)] private float clusterChance = 0.25f;
    [SerializeField] private float clusterRadius = 2f;
    [SerializeField] private int clusterSize = 3;

    [Header("Pooling")]
    [SerializeField] private int initialPoolSize = 50;
    [SerializeField] private int maxPoolSize = 200;

    [Header("References")]
    [SerializeField] private Transform player;

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private bool waveInProgress = false;
    private readonly HashSet<Enemy> spawnedEnemies = new HashSet<Enemy>();
    private Coroutine waveRoutine;
    private bool initialized;
    private bool runEnded;
    private WaveSpec activeSpec;
    private bool hasBounds;
    private Rect arenaBounds;

    // Cached values to avoid allocations
    private WaitForSeconds waveWait;
    private readonly List<EnemyData> eligible = new List<EnemyData>(8);

    // Spawn pattern state
    private float currentSafeAngle; // Direction player is facing/moving - we spawn AWAY from this

    // Public Accessors for Debug UI
    public int CurrentWave => currentWave;
    public int EnemiesAlive => enemiesAlive;
    public bool WaveInProgress => waveInProgress;

    /// <summary>
    /// When true the spawner never starts waves by itself. RoomManager sets this and
    /// calls StartWave when the player enters a room.
    /// </summary>
    public bool ExternalControl { get; set; }

    /// <summary>
    /// Debug setter for the debug menu.
    /// </summary>
    public void DebugSetWave(int wave) => currentWave = Mathf.Max(0, wave);

    /// <summary>Restrict spawning to a rectangle (the current arena). Pass hasBounds=false to clear.</summary>
    public void SetArenaBounds(Rect bounds, bool enabled = true)
    {
        arenaBounds = bounds;
        hasBounds = enabled;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (scorpionPrefab == null || scorpionPrefab.GetComponent<Enemy>() == null) {
            Debug.LogError("WaveSpawner requires a prefab with an Enemy component.", this);
            enabled = false;
            return;
        }
        baseEnemiesPerWave = Mathf.Max(1, baseEnemiesPerWave);
        clusterSize = Mathf.Max(1, clusterSize);

        if (player == null)
        {
            var playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                player = playerController.transform;
            }
        }

        // Pre-cache wait to avoid GC allocation each wave
        waveWait = new WaitForSeconds(timeBetweenWaves);

        EnsurePool(scorpionPrefab, initialPoolSize);

        initialized = true;
        waveRoutine = StartCoroutine(WaveController());
    }

    void OnEnable() {
        GameEvents.OnEnemyKilled += OnEnemyDied;
        GameEvents.OnRunEnded += HandleRunEnded;
        if (initialized) waveRoutine = StartCoroutine(WaveController());
    }

    void OnDisable() {
        GameEvents.OnEnemyKilled -= OnEnemyDied;
        GameEvents.OnRunEnded -= HandleRunEnded;
        if (waveRoutine != null) StopCoroutine(waveRoutine);
        waveRoutine = null;
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    private void EnsurePool(GameObject prefab, int size)
    {
        if (prefab == null || PoolingSystem.Instance == null) return;
        PoolingSystem.Instance.CreatePool(prefab, size, true, maxPoolSize);
    }

    // Endless built-in behaviour. Skipped entirely when a RoomManager has taken control.
    private IEnumerator WaveController() {
        while (!runEnded) {
            while (waveInProgress) yield return null;
            yield return waveWait;
            if (ExternalControl) {
                yield return null;
                continue;
            }
            while (player == null) {
                if (PlayerController.Instance != null) player = PlayerController.Instance.transform;
                yield return null;
            }

            StartWave(BuildDefaultSpec());

            while (waveInProgress) {
                yield return null;
            }
        }
    }

    private WaveSpec BuildDefaultSpec()
    {
        int count = Mathf.RoundToInt(baseEnemiesPerWave * Mathf.Pow(enemyIncreasePerWave, currentWave));
        return new WaveSpec {
            enemyCount = Mathf.Max(1, count),
            table = defaultTable != null && defaultTable.Length > 0 ? new List<EnemyData>(defaultTable) : null,
        };
    }

    /// <summary>Begin the next wave using the supplied spec. Ignored while a wave is running.</summary>
    public bool StartWave(WaveSpec spec)
    {
        if (waveInProgress || runEnded || spec == null) return false;
        if (player == null && PlayerController.Instance != null) player = PlayerController.Instance.transform;
        if (player == null) return false;

        currentWave++;
        activeSpec = spec;
        SpawnWave(spec);
        return true;
    }

    private void SpawnWave(WaveSpec spec) {
        int enemiesToSpawn = Mathf.Max(1, spec.enemyCount);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        GameEvents.DebugLog($"Starting Wave {currentWave}: {enemiesToSpawn} enemies", DebugCategory.EnemyAI);
        #endif

        waveInProgress = true;
        GameEvents.WaveStarted(currentWave, enemiesToSpawn + (spec.boss != null ? 1 : 0));

        // Update safe angle based on player's facing direction
        UpdateSafeAngle();

        if (spec.boss != null)
        {
            SpawnEnemy(spec.boss, GetBossSpawnPosition(), spec);
        }

        int i = 0;
        while (i < enemiesToSpawn)
        {
            float roll = Random.value;

            if (roll < clusterChance && i + clusterSize <= enemiesToSpawn)
            {
                Vector2 clusterCenter = GetSpawnPosition();
                int actualClusterSize = Random.Range(Mathf.Max(1, clusterSize / 2), clusterSize + 1);
                var clusterType = PickEnemyType(spec);

                for (int j = 0; j < actualClusterSize && i < enemiesToSpawn; j++)
                {
                    Vector2 offset = Random.insideUnitCircle * clusterRadius;
                    SpawnEnemy(clusterType, ClampToArena(clusterCenter + offset), spec);
                    i++;
                }
            }
            else
            {
                SpawnEnemy(PickEnemyType(spec), GetSpawnPosition(), spec);
                i++;
            }
        }
        if (enemiesAlive == 0) FinishWave();
    }

    private EnemyData PickEnemyType(WaveSpec spec)
    {
        var table = spec.table != null && spec.table.Count > 0 ? spec.table : null;
        if (table == null) return null; // prefab's own EnemyData

        eligible.Clear();
        float total = 0f;
        foreach (var data in table)
        {
            if (data == null || data.isBoss || data.minWave > currentWave) continue;
            eligible.Add(data);
            total += Mathf.Max(0.01f, data.spawnWeight);
        }
        if (eligible.Count == 0) return table[0];

        float roll = Random.value * total;
        foreach (var data in eligible)
        {
            roll -= Mathf.Max(0.01f, data.spawnWeight);
            if (roll <= 0f) return data;
        }
        return eligible[eligible.Count - 1];
    }

    /// <summary>Add enemies mid-wave (boss reinforcements). They count toward clearing the wave.</summary>
    public void SpawnReinforcements(int count, EnemyData type = null)
    {
        if (!waveInProgress || activeSpec == null) return;
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy(type != null ? type : PickEnemyType(activeSpec), GetSpawnPosition(), activeSpec);
        }
    }

    private void SpawnEnemy(EnemyData data, Vector2 position, WaveSpec spec)
    {
        GameObject prefab = data != null && data.prefab != null ? data.prefab : scorpionPrefab;
        EnsurePool(prefab, data != null && data.prefab != null ? 10 : initialPoolSize);

        // Pooling system first, direct Instantiate as a fallback
        GameObject go = PoolingSystem.Instance != null ? PoolingSystem.Instance.Get(prefab, position, Quaternion.identity) : null;
        if (go == null)
        {
            GameEvents.DebugLog("Pooling system not available or pool empty, instantiating enemy directly.", DebugCategory.EnemyAI);
            go = Instantiate(prefab, (Vector3)position, Quaternion.identity);
        }
        if (go == null) return;

        var enemy = go.GetComponent<Enemy>();
        if (enemy == null) return;
        enemy.Configure(data, spec.healthMultiplier, spec.speedMultiplier, spec.xpMultiplier, spec.damageMultiplier);

        if (spawnedEnemies.Add(enemy))
        {
            enemiesAlive++;
            GameEvents.EnemyCountChanged(enemiesAlive);
        }
    }

    private void UpdateSafeAngle()
    {
        var playerRb = player != null ? player.GetComponent<Rigidbody2D>() : null;
        if (playerRb != null && playerRb.linearVelocity.sqrMagnitude > 0.1f)
        {
            // Player is moving - keep their path clear
            currentSafeAngle = Mathf.Atan2(playerRb.linearVelocity.y, playerRb.linearVelocity.x) * Mathf.Rad2Deg;
        }
        else
        {
            // Player is stationary - pick a random safe direction
            currentSafeAngle = Random.Range(0f, 360f);
        }
    }

    private Vector2 ClampToArena(Vector2 p)
    {
        if (!hasBounds) return p;
        const float inset = 1.2f;
        return new Vector2(
            Mathf.Clamp(p.x, arenaBounds.xMin + inset, arenaBounds.xMax - inset),
            Mathf.Clamp(p.y, arenaBounds.yMin + inset, arenaBounds.yMax - inset));
    }

    private Vector2 GetSpawnPosition()
    {
        // A few tries so enemies do not appear on top of the player
        Vector2 best = Vector2.zero;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            best = Random.value < edgeSpawnChance ? GetEdgeSpawnPosition() : GetArcSpawnPosition();
            if (player == null || ((Vector2)player.position - best).sqrMagnitude >= minSpawnDistance * minSpawnDistance * 0.5f) break;
        }
        return best;
    }

    private Vector2 GetBossSpawnPosition()
    {
        // Far side of the arena from the player
        if (hasBounds)
        {
            Vector2 toCenter = arenaBounds.center - (Vector2)player.position;
            Vector2 dir = toCenter.sqrMagnitude > 1f ? toCenter.normalized : Random.insideUnitCircle.normalized;
            return ClampToArena((Vector2)arenaBounds.center + dir * Mathf.Min(arenaBounds.width, arenaBounds.height) * 0.3f);
        }
        return (Vector2)player.position + Random.insideUnitCircle.normalized * minSpawnDistance * 1.5f;
    }

    /// <summary>
    /// Spawn from arena edges (not surrounding player).
    /// </summary>
    private Vector2 GetEdgeSpawnPosition()
    {
        float halfWidth, halfHeight;
        Vector2 arenaCenter;
        if (hasBounds)
        {
            arenaCenter = arenaBounds.center;
            halfWidth = arenaBounds.width * 0.5f - 1.2f;
            halfHeight = arenaBounds.height * 0.5f - 1.2f;
        }
        else
        {
            arenaCenter = player != null ? (Vector2)player.position : Vector2.zero;
            halfWidth = arenaSize.x * 0.5f;
            halfHeight = arenaSize.y * 0.5f;
        }

        // Pick a random edge (0=top, 1=right, 2=bottom, 3=left)
        int edge = Random.Range(0, 4);
        float t = Random.value; // Position along edge (0-1)

        Vector2 spawnPos;
        switch (edge)
        {
            case 0: spawnPos = new Vector2(Mathf.Lerp(-halfWidth, halfWidth, t), halfHeight); break;
            case 1: spawnPos = new Vector2(halfWidth, Mathf.Lerp(-halfHeight, halfHeight, t)); break;
            case 2: spawnPos = new Vector2(Mathf.Lerp(-halfWidth, halfWidth, t), -halfHeight); break;
            default: spawnPos = new Vector2(-halfWidth, Mathf.Lerp(-halfHeight, halfHeight, t)); break;
        }

        return arenaCenter + spawnPos;
    }

    /// <summary>
    /// Spawn in an arc AWAY from player's safe direction (leaves escape route).
    /// </summary>
    private Vector2 GetArcSpawnPosition()
    {
        float spawnArcCenter = currentSafeAngle + 180f; // Opposite direction
        float arcHalfWidth = (1f - safeArcRatio) * 180f; // How wide the spawn arc is

        float angle = spawnArcCenter + Random.Range(-arcHalfWidth, arcHalfWidth);
        angle *= Mathf.Deg2Rad;

        float distance = Random.Range(minSpawnDistance, spawnRadius);
        Vector2 offset = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);

        return ClampToArena((Vector2)player.position + offset);
    }

    private void OnEnemyDied(Enemy enemy) {
        if (enemy == null || !spawnedEnemies.Remove(enemy)) return;
        enemiesAlive--;
        GameEvents.EnemyCountChanged(Mathf.Max(0, enemiesAlive));

        if (enemiesAlive <= 0) {
            FinishWave();
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            GameEvents.DebugLog($"Wave {currentWave} cleared!", DebugCategory.EnemyAI);
            #endif
        }
    }

    private void FinishWave()
    {
        enemiesAlive = 0;
        waveInProgress = false;
        activeSpec = null;
        GameEvents.WaveCleared(currentWave);
    }

    /// <summary>Return every live enemy to the pool without awarding kills (room reset, run end).</summary>
    public void ClearEnemies()
    {
        foreach (var enemy in new List<Enemy>(spawnedEnemies))
        {
            if (enemy == null) continue;
            if (PoolingSystem.Instance != null) PoolingSystem.Instance.Return(enemy.gameObject);
            else Destroy(enemy.gameObject);
        }
        spawnedEnemies.Clear();
        enemiesAlive = 0;
        waveInProgress = false;
        activeSpec = null;
        GameEvents.EnemyCountChanged(0);
    }

    private void HandleRunEnded(bool victory) {
        // Endless mode keeps playing after victory, so only a defeat or a final victory stops the loop.
        runEnded = true;
        if (waveRoutine != null) StopCoroutine(waveRoutine);
        waveRoutine = null;
    }

    /// <summary>Allow waves again after a victory when the player chooses endless mode.</summary>
    public void ResumeAfterVictory()
    {
        runEnded = false;
        if (waveRoutine == null && initialized && isActiveAndEnabled) waveRoutine = StartCoroutine(WaveController());
    }
}
