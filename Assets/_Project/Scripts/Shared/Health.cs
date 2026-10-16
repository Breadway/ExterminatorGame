using System;
using UnityEngine;

/// <summary>Something that soaks damage before it reaches HP (the Apiarist's bees). Returns the damage left over.</summary>
public interface IDamageAbsorber {
    float Absorb(float amount, Vector2 hitPoint);
}

public class Health : MonoBehaviour, IDamageable {
    [Header("Health Stats")]
    [SerializeField] private float maxHP = 100f;
    private float currentHP;
    private bool deathAnnounced;
    private float invulnerableUntil;
    private StatusEffectController statusEffects;
    private PlayerController player;
    private readonly System.Collections.Generic.List<IDamageAbsorber> absorbers = new System.Collections.Generic.List<IDamageAbsorber>();
    private float hitCapFraction;
    public bool CanHeal => canHeal;
    public void SetCanHeal(bool value) { canHeal = value; }
    public void AddAbsorber(IDamageAbsorber a) { if (a != null && !absorbers.Contains(a)) absorbers.Add(a); }
    public void RemoveAbsorber(IDamageAbsorber a) { absorbers.Remove(a); }

    /// <summary>No single hit may take more than this fraction of max HP (0 = no cap).</summary>
    public void SetHitCap(float fraction) { hitCapFraction = Mathf.Clamp01(fraction); }

    [Header("Settings")]
    [SerializeField] private bool isInvulnerable = false;
    [SerializeField] private bool canHeal = true;
    [Tooltip("Fraction of incoming damage ignored (0.5 = half damage). Used for armored enemies.")]
    [SerializeField] [Range(0f, 0.9f)] private float damageReduction = 0f;

    // Events for other systems to react to
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action<float, Vector2, Vector2> OnDamaged; // amount, hitPoint, hitDirection
    public event Action OnDied;
    public event Action<float> OnHealed; // amount

    /// <summary>
    /// Raised before OnDied. A listener may call Revive() to cancel the death (Second Wind).
    /// Runs ahead of every OnDied handler regardless of subscription order.
    /// </summary>
    public event Action OnWouldDie;

    public bool IsAlive => currentHP > 0;
    public bool IsInvulnerable => isInvulnerable || Time.time < invulnerableUntil;
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public float HealthPercent => maxHP > 0f ? currentHP / maxHP : 0f;

    void Awake() {
        maxHP = Mathf.Max(1f, maxHP);
        currentHP = maxHP;
        statusEffects = GetComponent<StatusEffectController>();
        player = GetComponent<PlayerController>();
    }

    /// <summary>
    /// Reset health to max (used for object pooling)
    /// </summary>
    public void ResetHealth()
    {
        currentHP = maxHP;
        deathAnnounced = false;
        invulnerableUntil = 0f;
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    // IDamageable implementation
    public void TakeDamage(float amount, Vector2 hitPoint, Vector2 hitDirection) {
        if (!IsAlive || IsInvulnerable || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        if (statusEffects != null) amount *= statusEffects.DamageTakenMultiplier;
        if (damageReduction > 0f) amount *= 1f - damageReduction;
        if (player != null && player.stats != null) amount = player.stats.ApplyArmorToDamage(amount);
        if (hitCapFraction > 0f) amount = Mathf.Min(amount, maxHP * hitCapFraction);
        for (int i = 0; i < absorbers.Count && amount > 0f; i++) amount = absorbers[i].Absorb(amount, hitPoint);
        if (amount <= 0f) return;
        currentHP = Mathf.Max(0f, currentHP - amount);

        OnDamaged?.Invoke(amount, hitPoint, hitDirection);
        OnHealthChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0 && !deathAnnounced) {
            deathAnnounced = true;
            Die();
        }
    }

    public void Heal(float amount) {
        if (!IsAlive || !canHeal || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        amount = Mathf.Min(amount, maxHP - currentHP);
        if (amount <= 0f) return;

        currentHP += amount;
        currentHP = Mathf.Min(currentHP, maxHP);

        OnHealed?.Invoke(amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    public void SetMaxHP(float newMax) {
        if (float.IsNaN(newMax) || float.IsInfinity(newMax)) return;
        maxHP = Mathf.Max(1f, newMax);
        currentHP = Mathf.Min(currentHP, maxHP); // Don't exceed new max
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    public void SetDamageReduction(float fraction) {
        damageReduction = Mathf.Clamp(fraction, 0f, 0.9f);
    }

    /// <summary>
    /// Ignore all damage for a short time (dash i-frames, post-revive grace).
    /// </summary>
    public void GrantInvulnerability(float seconds) {
        if (seconds <= 0f) { invulnerableUntil = 0f; return; }
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
    }

    /// <summary>How long the current invulnerability lasts, for debugging.</summary>
    public float InvulnerableRemaining => Mathf.Max(0f, invulnerableUntil - Time.time);


    /// <summary>
    /// Bring a dead (or nearly dead) entity back at a fraction of max HP.
    /// Only valid when called while handling OnDied, before the owner tears itself down.
    /// </summary>
    public void Revive(float fraction) {
        currentHP = Mathf.Clamp(maxHP * fraction, 1f, maxHP);
        deathAnnounced = false;
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    void Die() {
        OnWouldDie?.Invoke();
        if (currentHP > 0f) {
            // Revived by a listener: the death never happened.
            return;
        }
        OnDied?.Invoke();
        // Health doesn't know HOW to die (player vs enemy vs destructible)
        // It just announces "I died" via event
    }
}
