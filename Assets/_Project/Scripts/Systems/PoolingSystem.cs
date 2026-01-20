using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// High-performance object pooling system to eliminate GC allocations from Instantiate/Destroy.
/// Use for frequently spawned objects like enemies, projectiles, VFX, etc.
/// </summary>
public class PoolingSystem : MonoBehaviour
{
    public static PoolingSystem Instance { get; private set; }

    [System.Serializable]
    public class PoolConfig
    {
        public string poolId;
        public GameObject prefab;
        public int initialSize = 20;
        public bool expandable = true;
        public int maxSize = 100;
    }

    [Header("Pool Configurations")]
    [SerializeField] private List<PoolConfig> poolConfigs = new List<PoolConfig>();

    // Dictionaries for fast lookup
    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, PoolConfig> configLookup = new Dictionary<string, PoolConfig>();
    private Dictionary<GameObject, string> activeObjectToPoolId = new Dictionary<GameObject, string>();
    
    // Pre-allocated list to avoid allocations during pool operations
    private Transform poolContainer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        poolContainer = new GameObject("PooledObjects").transform;
        poolContainer.SetParent(transform);

        InitializePools();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializePools()
    {
        foreach (var config in poolConfigs)
        {
            CreatePool(config);
        }
    }

    /// <summary>
    /// Create a new pool from configuration
    /// </summary>
    public void CreatePool(PoolConfig config)
    {
        if (pools.ContainsKey(config.poolId))
        {
            Debug.LogWarning($"Pool '{config.poolId}' already exists.");
            return;
        }

        pools[config.poolId] = new Queue<GameObject>(config.initialSize);
        configLookup[config.poolId] = config;

        // Pre-warm the pool
        for (int i = 0; i < config.initialSize; i++)
        {
            CreatePooledObject(config);
        }
    }

    /// <summary>
    /// Create a pool at runtime from a prefab (auto-generates ID from prefab name)
    /// </summary>
    public void CreatePool(GameObject prefab, int initialSize = 20, bool expandable = true, int maxSize = 100)
    {
        string poolId = prefab.name;
        if (pools.ContainsKey(poolId))
        {
            return; // Pool already exists
        }

        var config = new PoolConfig
        {
            poolId = poolId,
            prefab = prefab,
            initialSize = initialSize,
            expandable = expandable,
            maxSize = maxSize
        };

        CreatePool(config);
    }

    private GameObject CreatePooledObject(PoolConfig config)
    {
        GameObject obj = Instantiate(config.prefab, poolContainer);
        obj.SetActive(false);
        pools[config.poolId].Enqueue(obj);
        return obj;
    }

    /// <summary>
    /// Get an object from the pool. Returns null if pool doesn't exist or is empty and non-expandable.
    /// </summary>
    public GameObject Get(string poolId, Vector3 position, Quaternion rotation)
    {
        if (!pools.TryGetValue(poolId, out var pool))
        {
            Debug.LogWarning($"Pool '{poolId}' not found. Create it first or use Instantiate.");
            return null;
        }

        GameObject obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            var config = configLookup[poolId];
            if (!config.expandable)
            {
                Debug.LogWarning($"Pool '{poolId}' is empty and not expandable.");
                return null;
            }

            // Check max size limit
            if (activeObjectToPoolId.Count >= config.maxSize)
            {
                Debug.LogWarning($"Pool '{poolId}' reached max size ({config.maxSize}).");
                return null;
            }

            obj = Instantiate(config.prefab);
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.SetParent(null);
        obj.SetActive(true);

        activeObjectToPoolId[obj] = poolId;

        // Notify the object it was spawned from pool
        var poolable = obj.GetComponent<IPoolable>();
        poolable?.OnSpawnFromPool();

        return obj;
    }

    /// <summary>
    /// Get an object from pool using prefab name as ID
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string poolId = prefab.name;
        
        // Auto-create pool if it doesn't exist
        if (!pools.ContainsKey(poolId))
        {
            CreatePool(prefab);
        }

        return Get(poolId, position, rotation);
    }

    /// <summary>
    /// Return an object to its pool. Much faster than Destroy().
    /// </summary>
    public void Return(GameObject obj)
    {
        if (obj == null) return;

        if (!activeObjectToPoolId.TryGetValue(obj, out string poolId))
        {
            // Object wasn't from a pool, destroy it normally
            Destroy(obj);
            return;
        }

        // Notify the object it's being returned
        var poolable = obj.GetComponent<IPoolable>();
        poolable?.OnReturnToPool();

        activeObjectToPoolId.Remove(obj);

        obj.SetActive(false);
        obj.transform.SetParent(poolContainer);

        if (pools.TryGetValue(poolId, out var pool))
        {
            pool.Enqueue(obj);
        }
        else
        {
            // Pool was destroyed, destroy the object
            Destroy(obj);
        }
    }

    /// <summary>
    /// Return all active objects to their pools
    /// </summary>
    public void ReturnAll()
    {
        // Copy keys to avoid modification during iteration
        var activeObjects = new List<GameObject>(activeObjectToPoolId.Keys);
        foreach (var obj in activeObjects)
        {
            Return(obj);
        }
    }

    /// <summary>
    /// Return all active objects of a specific pool
    /// </summary>
    public void ReturnAll(string poolId)
    {
        var toReturn = new List<GameObject>();
        foreach (var kvp in activeObjectToPoolId)
        {
            if (kvp.Value == poolId)
            {
                toReturn.Add(kvp.Key);
            }
        }

        foreach (var obj in toReturn)
        {
            Return(obj);
        }
    }

    /// <summary>
    /// Get stats for debugging
    /// </summary>
    public (int available, int active) GetPoolStats(string poolId)
    {
        if (!pools.TryGetValue(poolId, out var pool))
        {
            return (0, 0);
        }

        int active = 0;
        foreach (var kvp in activeObjectToPoolId)
        {
            if (kvp.Value == poolId) active++;
        }

        return (pool.Count, active);
    }

    /// <summary>
    /// Check if a pool exists
    /// </summary>
    public bool HasPool(string poolId) => pools.ContainsKey(poolId);
}

/// <summary>
/// Interface for pooled objects to receive spawn/return callbacks
/// </summary>
public interface IPoolable
{
    /// <summary>Called when object is retrieved from pool and activated</summary>
    void OnSpawnFromPool();
    
    /// <summary>Called when object is being returned to pool</summary>
    void OnReturnToPool();
}