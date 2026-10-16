using UnityEngine;

public enum PickupKind
{
    Heal,
    Scrap
}

/// <summary>
/// A small trigger item that drifts to the player when close and applies its effect on contact.
/// Despawns after a while so a long fight does not litter the arena.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class Pickup : MonoBehaviour
{
    public PickupKind kind = PickupKind.Heal;
    public float amount = 0.15f;
    [SerializeField] private float magnetRange = 2.6f;
    [SerializeField] private float magnetSpeed = 9f;
    [SerializeField] private float lifetime = 14f;

    private float dieAt;
    private float bobSeed;
    private Vector3 basePosition;

    void Awake()
    {
        GetComponent<CircleCollider2D>().isTrigger = true;
        bobSeed = Random.value * 6.28f;
    }

    void OnEnable()
    {
        dieAt = Time.time + lifetime;
        basePosition = transform.position;
    }

    void Update()
    {
        if (Time.time >= dieAt) { Destroy(gameObject); return; }
        var player = PlayerController.Instance;
        if (player == null) return;

        Vector2 to = (Vector2)player.transform.position - (Vector2)transform.position;
        if (to.magnitude < magnetRange)
        {
            transform.position += (Vector3)(to.normalized * magnetSpeed * Time.deltaTime);
            basePosition = transform.position;
        }
        else
        {
            transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 4f + bobSeed) * 0.08f);
        }
        // Blink during the last seconds so players know it is about to vanish
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = Time.time < dieAt - 3f || Mathf.FloorToInt(Time.time * 8f) % 2 == 0;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var controller = other.GetComponentInParent<PlayerController>();
        if (controller == null) return;
        if (kind == PickupKind.Heal)
        {
            controller.GetHealth(out var health);
            if (health == null || health.CurrentHP >= health.MaxHP) return; // keep it for later
            health.Heal(health.MaxHP * amount);
        }
        else
        {
            MetaProgression.Instance?.Earn(Mathf.Max(1, Mathf.RoundToInt(amount)));
        }
        Destroy(gameObject);
    }
}
