using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Performance utilities to reduce GC allocations and improve frame times.
/// </summary>
public static class PerformanceUtils
{
    // ========== STRING BUILDER POOL ==========
    private static readonly Stack<System.Text.StringBuilder> sbPool = new Stack<System.Text.StringBuilder>();
    private const int SB_INITIAL_CAPACITY = 256;

    /// <summary>
    /// Get a StringBuilder from the pool to avoid string allocations.
    /// Remember to call ReturnStringBuilder when done!
    /// </summary>
    public static System.Text.StringBuilder GetStringBuilder()
    {
        return sbPool.Count > 0 ? sbPool.Pop() : new System.Text.StringBuilder(SB_INITIAL_CAPACITY);
    }

    public static void ReturnStringBuilder(System.Text.StringBuilder sb)
    {
        sb.Clear();
        sbPool.Push(sb);
    }

    // ========== CACHED WAIT FOR SECONDS ==========
    private static readonly Dictionary<float, WaitForSeconds> waitCache = new Dictionary<float, WaitForSeconds>();

    /// <summary>
    /// Get a cached WaitForSeconds to avoid GC allocations in coroutines.
    /// </summary>
    public static WaitForSeconds GetWaitForSeconds(float seconds)
    {
        if (!waitCache.TryGetValue(seconds, out var wait))
        {
            wait = new WaitForSeconds(seconds);
            waitCache[seconds] = wait;
        }
        return wait;
    }

    // Pre-cached common waits
    public static readonly WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();
    public static readonly WaitForFixedUpdate WaitForFixedUpdate = new WaitForFixedUpdate();

    // ========== VECTOR MATH HELPERS ==========
    
    /// <summary>
    /// Faster distance check using squared magnitude (avoids sqrt).
    /// </summary>
    public static bool IsWithinDistance(Vector3 a, Vector3 b, float maxDistance)
    {
        return (a - b).sqrMagnitude <= maxDistance * maxDistance;
    }

    /// <summary>
    /// Faster distance check using squared magnitude (avoids sqrt) - Vector2 version.
    /// </summary>
    public static bool IsWithinDistance(Vector2 a, Vector2 b, float maxDistance)
    {
        return (a - b).sqrMagnitude <= maxDistance * maxDistance;
    }

    /// <summary>
    /// Faster distance check for 2D (XY plane) using Vector3 inputs.
    /// </summary>
    public static bool IsWithinDistance2D(Vector3 a, Vector3 b, float maxDistance)
    {
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        return (dx * dx + dy * dy) <= maxDistance * maxDistance;
    }

    /// <summary>
    /// Get squared distance (faster than Vector3.Distance).
    /// </summary>
    public static float SqrDistance(Vector3 a, Vector3 b)
    {
        return (a - b).sqrMagnitude;
    }

    /// <summary>
    /// Get squared distance (faster than Vector2.Distance) - Vector2 version.
    /// </summary>
    public static float SqrDistance(Vector2 a, Vector2 b)
    {
        return (a - b).sqrMagnitude;
    }

    // ========== LAYER MASK HELPERS ==========
    
    /// <summary>
    /// Check if a layer is in a layer mask.
    /// </summary>
    public static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    // ========== COMPONENT CACHING ==========
    
    /// <summary>
    /// Try to get a component with caching (reduces GetComponent calls).
    /// </summary>
    public static T GetCachedComponent<T>(GameObject go, ref T cached) where T : Component
    {
        if (cached == null)
        {
            cached = go.GetComponent<T>();
        }
        return cached;
    }
}

/// <summary>
/// Extension methods for common performance optimizations.
/// </summary>
public static class PerformanceExtensions
{
    /// <summary>
    /// Set position and rotation in one call (fewer transform updates).
    /// </summary>
    public static void SetPositionAndRotation(this Transform t, Vector3 position, Quaternion rotation)
    {
        t.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// Check if approximately zero without allocations.
    /// </summary>
    public static bool IsApproximatelyZero(this Vector3 v, float threshold = 0.0001f)
    {
        return v.sqrMagnitude < threshold;
    }

    /// <summary>
    /// Check if approximately zero without allocations - Vector2 version.
    /// </summary>
    public static bool IsApproximatelyZero(this Vector2 v, float threshold = 0.0001f)
    {
        return v.sqrMagnitude < threshold;
    }

    /// <summary>
    /// Normalize only if needed (avoids unnecessary sqrt).
    /// </summary>
    public static Vector3 NormalizeIfNeeded(this Vector3 v)
    {
        float sqrMag = v.sqrMagnitude;
        if (sqrMag > 1.0001f || sqrMag < 0.9999f)
        {
            return v / Mathf.Sqrt(sqrMag);
        }
        return v;
    }

    /// <summary>
    /// Normalize only if needed (avoids unnecessary sqrt) - Vector2 version.
    /// </summary>
    public static Vector2 NormalizeIfNeeded(this Vector2 v)
    {
        float sqrMag = v.sqrMagnitude;
        if (sqrMag > 1.0001f || sqrMag < 0.9999f)
        {
            return v / Mathf.Sqrt(sqrMag);
        }
        return v;
    }
}
