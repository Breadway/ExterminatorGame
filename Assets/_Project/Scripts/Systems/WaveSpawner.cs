using UnityEngine;
using System.Collections;

public class WaveSpawner : MonoBehaviour {
    [Header("Wave Settings")]
    [SerializeField] private GameObject scorpionPrefab;
    // add other enemy prefabs here
    [SerializeField] private int baseEnemiesPerWave = 4;
    [SerializeField] private float enemyIncreasePerWave = 1.3f;
    [SerializeField] private float timeBetweenWaves = 3f;

    [Header("Spawn Area")]
    [SerializeField] private float spawnRadius = 12f;
    [SerializeField] private float minSpawnDistance = 6f;
    [SerializeField] private Vector2 arenaSize = new Vector2(25f, 20f);
    
    [Header("Spawn Patterns")]
    [Tooltip("How much of the circle around player is blocked for spawning (0-1). 0.5 = enemies spawn in 180° arc")]
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
    
    // Cached values to avoid allocations
    private WaitForSeconds waveWait;
    private string poolId;
    
    // Spawn pattern state
    private float currentSafeAngle; // Direction player is facing/moving - we spawn AWAY from this
    
    // Public Accessors for Debug UI
    public int CurrentWave => currentWave;
    public int EnemiesAlive => enemiesAlive;
    public bool WaveInProgress => waveInProgress;
    
    /// <summary>
    /// Debug setter for the debug menu.
    /// </summary>
    public void DebugSetWave(int wave) => currentWave = Mathf.Max(0, wave);
    
    void Start()
    {
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
        
        // Initialize enemy pool
        if (scorpionPrefab != null)
        {
            poolId = scorpionPrefab.name;
            PoolingSystem.Instance?.CreatePool(scorpionPrefab, initialPoolSize, true, maxPoolSize);
        }

        GameEvents.OnEnemyKilled += OnEnemyDied;

        StartCoroutine(WaveController());
    }
    
    void OnDestroy() {
        GameEvents.OnEnemyKilled -= OnEnemyDied;
    }
    
    private IEnumerator WaveController() {
        while (true) {
            // Wait between waves (uses cached WaitForSeconds)
            yield return waveWait;
            
            // Start new wave
            currentWave++;
            SpawnWave();
            
            // Wait for wave to be cleared
            while (waveInProgress) {
                yield return null;
            }
        }
    }
    
    private void SpawnWave() {
        int enemiesToSpawn = Mathf.RoundToInt(baseEnemiesPerWave * Mathf.Pow(enemyIncreasePerWave, currentWave - 1));
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        GameEvents.DebugLog($"Starting Wave {currentWave}: {enemiesToSpawn} enemies", DebugCategory.EnemyAI);
        #endif
        
        waveInProgress = true;
        
        // Update safe angle based on player's facing direction
        UpdateSafeAngle();
        
        int i = 0;
        while (i < enemiesToSpawn)
        {
            // Decide spawn pattern
            float roll = Random.value;
            
            if (roll < clusterChance && i + clusterSize <= enemiesToSpawn)
            {
                // Spawn a cluster
                Vector2 clusterCenter = GetSpawnPosition();
                int actualClusterSize = Random.Range(clusterSize / 2, clusterSize + 1);
                
                for (int j = 0; j < actualClusterSize && i < enemiesToSpawn; j++)
                {
                    Vector2 offset = Random.insideUnitCircle * clusterRadius;
                    Vector2 spawnPos = clusterCenter + offset;
                    SpawnEnemy(spawnPos);
                    i++;
                }
            }
            else
            {
                // Spawn individual enemy
                Vector2 spawnPos = GetSpawnPosition();
                SpawnEnemy(spawnPos);
                i++;
            }
        }
    }
    
    private void SpawnEnemy(Vector2 position)
    {
        // Use pooling system instead of Instantiate
        GameObject enemy = PoolingSystem.Instance?.Get(poolId, position, Quaternion.identity);
        
        // Fallback to Instantiate if pooling not available
        if (enemy == null)
        {
            GameEvents.DebugLog("Pooling system not available or pool empty, instantiating enemy directly.", DebugCategory.EnemyAI);
            Instantiate(scorpionPrefab, (Vector3)position, Quaternion.identity);
        }
        
        enemiesAlive++;
    }
    
    private void UpdateSafeAngle()
    {
        // Try to get player's movement direction or facing direction
        var playerRb = player.GetComponent<Rigidbody2D>();
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
    
    private Vector2 GetSpawnPosition()
    {
        if (Random.value < edgeSpawnChance)
        {
            return GetEdgeSpawnPosition();
        }
        else
        {
            return GetArcSpawnPosition();
        }
    }
    
    /// <summary>
    /// Spawn from arena edges (not surrounding player).
    /// </summary>
    private Vector2 GetEdgeSpawnPosition()
    {
        Vector2 arenaCenter = player != null ? (Vector2)player.position : Vector2.zero;
        
        // Pick a random edge (0=top, 1=right, 2=bottom, 3=left)
        int edge = Random.Range(0, 4);
        float t = Random.value; // Position along edge (0-1)
        
        Vector2 spawnPos;
        float halfWidth = arenaSize.x * 0.5f;
        float halfHeight = arenaSize.y * 0.5f;
        
        switch (edge)
        {
            case 0: // Top
                spawnPos = new Vector2(Mathf.Lerp(-halfWidth, halfWidth, t), halfHeight);
                break;
            case 1: // Right
                spawnPos = new Vector2(halfWidth, Mathf.Lerp(-halfHeight, halfHeight, t));
                break;
            case 2: // Bottom
                spawnPos = new Vector2(Mathf.Lerp(-halfWidth, halfWidth, t), -halfHeight);
                break;
            default: // Left
                spawnPos = new Vector2(-halfWidth, Mathf.Lerp(-halfHeight, halfHeight, t));
                break;
        }
        
        return arenaCenter + spawnPos;
    }
    
    /// <summary>
    /// Spawn in an arc AWAY from player's safe direction (leaves escape route).
    /// </summary>
    private Vector2 GetArcSpawnPosition()
    {
        // Calculate spawn arc (opposite of safe direction)
        float spawnArcCenter = currentSafeAngle + 180f; // Opposite direction
        float arcHalfWidth = (1f - safeArcRatio) * 180f; // How wide the spawn arc is
        
        // Random angle within the spawn arc
        float angle = spawnArcCenter + Random.Range(-arcHalfWidth, arcHalfWidth);
        angle *= Mathf.Deg2Rad;
        
        float distance = Random.Range(minSpawnDistance, spawnRadius);
        
        Vector2 offset = new Vector2(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance
        );
        
        return (Vector2)player.position + offset;
    }
    
    private void OnEnemyDied(Enemy enemy) {
        enemiesAlive--;
        
        if (enemiesAlive <= 0) {
            waveInProgress = false;
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            GameEvents.DebugLog($"Wave {currentWave} cleared!");
            #endif
        }
    }
}