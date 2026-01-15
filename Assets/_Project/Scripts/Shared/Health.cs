using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable {
    [Header("Health Stats")]
    [SerializeField] private float maxHP = 100f;
    private float currentHP;
    
    [Header("Settings")]
    [SerializeField] private bool isInvulnerable = false;
    [SerializeField] private bool canHeal = true;
    
    // Events for other systems to react to
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action<float, Vector3, Vector3> OnDamaged; // amount, hitPoint, hitDirection
    public event Action OnDied;
    public event Action<float> OnHealed; // amount
    
    public bool IsAlive => currentHP > 0;
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public float HealthPercent => currentHP / maxHP;
    
    void Awake() {
        currentHP = maxHP;
    }
    
    // IDamageable implementation
    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection) {
        if (!IsAlive || isInvulnerable) return;
        
        currentHP -= amount;
        currentHP = Mathf.Max(0, currentHP);
        
        OnDamaged?.Invoke(amount, hitPoint, hitDirection);
        OnHealthChanged?.Invoke(currentHP, maxHP);
        
        if (currentHP <= 0) {
            Die();
        }
    }
    
    public void Heal(float amount) {
        if (!IsAlive || !canHeal) return;
        
        currentHP += amount;
        currentHP = Mathf.Min(currentHP, maxHP);
        
        OnHealed?.Invoke(amount);
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }
    
    public void SetMaxHP(float newMax) {
        maxHP = newMax;
        currentHP = Mathf.Min(currentHP, maxHP); // Don't exceed new max
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }
    
    void Die() {
        OnDied?.Invoke();
        // Health doesn't know HOW to die (player vs enemy vs destructible)
        // It just announces "I died" via event
    }
}