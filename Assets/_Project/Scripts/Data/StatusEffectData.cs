using UnityEngine;

/// <summary>
/// Types of status effects that can be applied to entities.
/// </summary>
public enum StatusEffectType {
    Fire,
    Ice,
    Poison,
    Shock,
    Bleed
}

/// <summary>
/// ScriptableObject that defines a status effect's properties.
/// Create instances in Assets/_Project/Data/StatusEffects/
/// Right-click → Create → Exterminator → Status Effect Data
/// </summary>
[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Exterminator/Status Effect Data")]
public class StatusEffectData : ScriptableObject {
    
    // ========================================
    // IDENTIFICATION
    // ========================================
    
    [Header("Identification")]
    [SerializeField]
    [Tooltip("The type of status effect")]
    private StatusEffectType effectType;
    
    [SerializeField]
    [Tooltip("Display name for the effect")]
    private string effectName;
    
    // ========================================
    // TIMING
    // ========================================
    
    [Header("Timing")]
    [SerializeField]
    [Tooltip("How long the effect lasts in seconds")]
    private float duration = 5f;
    
    [SerializeField]
    [Tooltip("How often the effect ticks (for DoT effects)")]
    private float tickInterval = 0.5f;
    
    [SerializeField]
    [Tooltip("Can this effect stack multiple times?")]
    private bool canStack = false;
    
    [SerializeField]
    [Tooltip("Max stacks if stacking is enabled")]
    private int maxStacks = 1;
    
    [SerializeField]
    [Tooltip("Does applying refresh the duration?")]
    private bool refreshDurationOnReapply = true;
    
    // ========================================
    // DAMAGE OVER TIME
    // ========================================
    
    [Header("Damage Over Time")]
    [SerializeField]
    [Tooltip("Damage dealt per tick")]
    private float damagePerTick = 0f;
    
    // ========================================
    // MODIFIERS
    // ========================================
    
    [Header("Movement Modifier")]
    [SerializeField]
    [Tooltip("Multiplier for movement speed (0.5 = 50% slower, 1.0 = no change)")]
    [Range(0f, 2f)]
    private float moveSpeedMultiplier = 1f;
    
    [Header("Damage Modifier")]
    [SerializeField]
    [Tooltip("Multiplier for damage taken while afflicted")]
    [Range(0f, 3f)]
    private float damageTakenMultiplier = 1f;
    
    // ========================================
    // SPREAD (Fire propagation)
    // ========================================
    
    [Header("Spread (Fire propagation)")]
    [SerializeField]
    [Tooltip("Can this effect spread to nearby entities?")]
    private bool canSpread = false;
    
    [SerializeField]
    [Tooltip("Radius to check for spread targets")]
    private float spreadRadius = 2f;
    
    [SerializeField]
    [Tooltip("How long after initial application before spread starts")]
    private float spreadDelay = 0f;
    
    [SerializeField]
    [Tooltip("How long the spread window lasts")]
    private float spreadDuration = 8f;
    
    [SerializeField]
    [Tooltip("Interval between spread attempts")]
    private float spreadInterval = 1f;
    
    [SerializeField]
    [Tooltip("Layer mask for spread targets")]
    private LayerMask spreadTargetMask;
    
    // ========================================
    // VISUAL/AUDIO
    // ========================================
    
    [Header("Visual/Audio")]
    [SerializeField]
    [Tooltip("VFX prefab to spawn on target when effect is applied")]
    private GameObject effectPrefab;
    
    [SerializeField]
    [Tooltip("Tint color for the visual effect")]
    private Color effectTint = Color.white;
    
    // ========================================
    // PUBLIC ACCESSORS (Read-Only)
    // ========================================
    
    public StatusEffectType EffectType => effectType;
    public string EffectName => effectName;
    public float Duration => duration;
    public float TickInterval => tickInterval;
    public bool CanStack => canStack;
    public int MaxStacks => maxStacks;
    public bool RefreshDurationOnReapply => refreshDurationOnReapply;
    public float DamagePerTick => damagePerTick;
    public float MoveSpeedMultiplier => moveSpeedMultiplier;
    public float DamageTakenMultiplier => damageTakenMultiplier;
    public bool CanSpread => canSpread;
    public float SpreadRadius => spreadRadius;
    public float SpreadDelay => spreadDelay;
    public float SpreadDuration => spreadDuration;
    public float SpreadInterval => spreadInterval;
    public LayerMask SpreadTargetMask => spreadTargetMask;
    public GameObject EffectPrefab => effectPrefab;
    public Color EffectTint => effectTint;
}
