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

    /// <summary>Matches CharacterData.weaponId so a character enables exactly one weapon.</summary>
    string WeaponId { get; }
}

/// <summary>
/// A weapon driven by holding and releasing the button (charge shots) rather than a repeating Attack().
/// PlayerAttack calls BeginHold/ReleaseHold instead of its auto-fire loop.
/// </summary>
public interface IHoldWeapon : IWeaponAttack {
    void BeginHold();
    void ReleaseHold();
}

/// <summary>A component that is only active for one character (a signature constraint).</summary>
public interface ICharacterConstraint {
    string CharacterId { get; }
}
