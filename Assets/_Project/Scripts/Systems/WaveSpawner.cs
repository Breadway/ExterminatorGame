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
    
    [Header("References")]
    [SerializeField] private Transform player;


    private int currentWave = 0;
    private int enemiesAlive = 0;
    private bool waveInProgress = false;
    
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

        GameEvents.OnEnemyKilled += OnEnemyDied;

        StartCoroutine(WaveController());
    }
    void OnDestroy() {
        GameEvents.OnEnemyKilled -= OnEnemyDied;
    }
    
    private IEnumerator WaveController() {
        while (true) {
            // Wait between waves
            yield return new WaitForSeconds(timeBetweenWaves);
            
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
        
        Debug.Log($"Starting Wave {currentWave}: {enemiesToSpawn} enemies");
        
        waveInProgress = true;
        
        for (int i = 0; i < enemiesToSpawn; i++) {
            Vector3 spawnPos = GetRandomSpawnPosition();
            Instantiate(scorpionPrefab, spawnPos, Quaternion.identity);
            enemiesAlive++;
        }
    }
    
    private Vector3 GetRandomSpawnPosition() {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minSpawnDistance, spawnRadius);
        
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * distance,
            0f,
            Mathf.Sin(angle) * distance
        );
        
        return player.position + offset;
    }
    
    private void OnEnemyDied(Enemy enemy) {
        enemiesAlive--;
        
        if (enemiesAlive <= 0) {
            waveInProgress = false;
            Debug.Log($"Wave {currentWave} cleared!");
        }
    }
}