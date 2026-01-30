using UnityEngine;

/// <summary>
/// Runtime instance of an active status effect on an entity.
/// Not a MonoBehaviour - just data managed by StatusEffectController.
/// </summary>
public class ActiveStatusEffect {
    public StatusEffectData Data { get; private set; }
    public float RemainingDuration { get; private set; }
    public int CurrentStacks { get; private set; }
    public float NextTickTime { get; private set; }
    public GameObject VisualInstance { get; set; }
    
    // Spread tracking
    public float SpreadStartTime { get; private set; }
    public float NextSpreadTime { get; private set; }
    public bool HasSpreadWindow => Data.CanSpread && 
        Time.time >= SpreadStartTime && 
        Time.time <= SpreadStartTime + Data.SpreadDuration;
    
    private float initialDuration;
    
    public ActiveStatusEffect(StatusEffectData data, float duration) {
        Data = data;
        RemainingDuration = duration;
        initialDuration = duration;
        CurrentStacks = 1;
        NextTickTime = Time.time + data.TickInterval;
        SpreadStartTime = Time.time + data.SpreadDelay;
        NextSpreadTime = SpreadStartTime;
    }
    
    public void RefreshDuration() {
        RemainingDuration = initialDuration;
    }
    
    public void AddStack() {
        if (CurrentStacks < Data.MaxStacks) {
            CurrentStacks++;
        }
    }
    
    public void UpdateTick(float deltaTime) {
        RemainingDuration -= deltaTime;
    }
    
    public bool ShouldTick() {
        if (Time.time >= NextTickTime) {
            NextTickTime = Time.time + Data.TickInterval;
            return true;
        }
        return false;
    }
    
    public bool ShouldSpread() {
        if (!HasSpreadWindow) return false;
        if (Time.time >= NextSpreadTime) {
            NextSpreadTime = Time.time + Data.SpreadInterval;
            return true;
        }
        return false;
    }
    
    public bool IsExpired => RemainingDuration <= 0f;
    
    public float GetTotalDamagePerTick() {
        return Data.DamagePerTick * CurrentStacks;
    }
}