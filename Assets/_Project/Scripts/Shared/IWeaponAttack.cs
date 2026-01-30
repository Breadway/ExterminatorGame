using UnityEngine;

public interface IWeaponAttack {
    void Attack(); // Called every frame while shooting
    void StopAttack(); // Called when player releases fire button
    void Initialize(PlayerStats stats); // Pass player stats for damage scaling
    
    /// <summary>
    /// The effective range of this weapon in world units.
    /// Used by aiming systems for UI feedback (range indicators, reticles, etc).
    /// </summary>
    float WeaponRange { get; }
}