using UnityEngine;
using System.Collections;

public class WaveSpawner : MonoBehaviour {
    [Header("Wave Settings")]
    [SerializeField] private GameObject scorpionPrefab;
    // add other enemy prefabs here
    [SerializeField] private int baseEnemiesPerWave = 5;
    [SerializeField] private float enemyIncreasePerWave = 1.5f;
    [SerializeField] private float timeBetweenWaves = 5f;

    [Header("Spawn Area")]
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private float minSpawnDistance = 8f;
    
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
        Debug.Log($"Starting Wave {currentWave}: {enemiesToSpawn} enemies");
        #endif
        
        waveInProgress = true;
        
        for (int i = 0; i < enemiesToSpawn; i++) {
            Vector2 spawnPos = GetRandomSpawnPosition();
            
            // Use pooling system instead of Instantiate
            GameObject enemy = PoolingSystem.Instance?.Get(poolId, spawnPos, Quaternion.identity);
            
            // Fallback to Instantiate if pooling not available
            if (enemy == null)
            {
                enemy = Instantiate(scorpionPrefab, (Vector3)spawnPos, Quaternion.identity);
            }
            
            enemiesAlive++;
        }
    }
    
    private Vector2 GetRandomSpawnPosition() {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
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
            Debug.Log($"Wave {currentWave} cleared!");
            #endif
        }
    }
}