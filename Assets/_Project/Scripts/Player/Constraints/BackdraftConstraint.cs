using UnityEngine;

/// <summary>
/// The Pyrotechnic's constraint. The leaking tank drops fuel as you move. Stand still for more than
/// a second and the fuel under you ignites, burning you and everything on it. Fire then lasts
/// 8 seconds and spreads along the fuel trail. Moving keeps you ahead of your own fire, and the trail
/// doubles as a weapon against anything chasing you.
/// </summary>
public class BackdraftConstraint : MonoBehaviour, ICharacterConstraint
{
    [Header("Fuel trail")]
    [SerializeField] private float dropEvery = 0.55f;
    [SerializeField] private float fuelRadius = 0.55f;
    [SerializeField] private float fuelLifetime = 14f;

    [Header("Backdraft")]
    [SerializeField] private float standStillSeconds = 1f;
    [SerializeField] private float moveThreshold = 0.6f;
    [SerializeField] private float fireSeconds = 8f;
    [SerializeField] private float fireDps = 7f;
    [SerializeField] private float selfDamageMultiplier = 1f;

    public string CharacterId => "firebug";

    private Rigidbody2D rb;
    private PlayerController owner;
    private float stillTime;
    private float nextDrop;
    private Vector2 lastDrop;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        owner = GetComponent<PlayerController>();
    }

    void OnEnable()
    {
        stillTime = 0f;
        nextDrop = 0f;
    }

    void Update()
    {
        if (Time.timeScale <= 0f || owner == null || HazardField.Instance == null) return;
        if (owner.CharacterId != CharacterId) return;
        owner.GetHealth(out var health);
        if (health == null || !health.IsAlive) return;

        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        if (speed > moveThreshold)
        {
            stillTime = 0f;
            if (Time.time >= nextDrop && Vector2.Distance(lastDrop, transform.position) > fuelRadius * 0.9f)
            {
                nextDrop = Time.time + dropEvery;
                lastDrop = transform.position;
                HazardField.Instance.FireDps = fireDps;
                HazardField.Instance.Spawn(HazardKind.Fuel, transform.position, fuelRadius, fuelLifetime, 0f, true, true);
            }
            return;
        }

        stillTime += Time.deltaTime;
        GameEvents.WeaponMeter("BACKDRAFT", Mathf.Clamp01(stillTime / standStillSeconds), stillTime > standStillSeconds * 0.6f);
        if (stillTime >= standStillSeconds)
        {
            stillTime = 0f;
            Ignite();
        }
    }

    private void Ignite()
    {
        HazardField.Instance.FireDps = fireDps;
        // The puddle under you: light it, and make sure one exists even if you never moved
        var puddle = HazardField.Instance.Spawn(HazardKind.Fuel, transform.position, fuelRadius * 1.3f, 2f, 0f, true, true);
        puddle?.Ignite(0f, fireSeconds, fireDps * selfDamageMultiplier);
        HazardField.Instance.IgniteNear(transform.position, fuelRadius * 2f);
        GameEvents.PlaySound("explosion");
        GameEvents.ScreenShake(0.35f);
        GameEvents.ShowNotification("BACKDRAFT");
    }
}
