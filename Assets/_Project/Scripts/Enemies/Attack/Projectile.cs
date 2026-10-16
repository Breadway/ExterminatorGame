using UnityEngine;

/// <summary>
/// A pooled projectile that damages the first Health it overlaps on the target layer.
/// Pooling hands it back through PoolingSystem.Return, never Destroy.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour, IPoolable
{
    private Rigidbody2D rb;
    private float damage;
    private float dieAt;
    private bool live;
    private LayerMask hitMask;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public virtual void Fire(Vector2 direction, float speed, float damage, float lifetime, LayerMask hitMask)
    {
        this.damage = damage;
        this.hitMask = hitMask;
        dieAt = Time.time + Mathf.Max(0.1f, lifetime);
        live = true;
        rb.linearVelocity = direction.normalized * speed;
        transform.up = direction;
    }

    void Update()
    {
        if (live && Time.time >= dieAt) Release();
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (!live) return;
        if ((hitMask.value & (1 << other.gameObject.layer)) != 0)
        {
            var health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                Vector2 dir = rb.linearVelocity.normalized;
                health.TakeDamage(damage, transform.position, dir);
            }
            Release();
        }
        else if (!other.isTrigger && other.GetComponentInParent<Health>() == null)
        {
            Release(); // wall or cover
        }
    }

    private void Release()
    {
        if (!live) return;
        live = false;
        if (PoolingSystem.Instance != null) PoolingSystem.Instance.Return(gameObject);
        else Destroy(gameObject);
    }

    public void OnSpawnFromPool() { live = false; }

    public void OnReturnToPool()
    {
        live = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
