using UnityEngine;

public interface IWeaponAttack {
    void Attack(); // Called every frame while shooting
    void StopAttack(); // Called when player releases fire button
    void Initialize(PlayerStats stats); // Pass player stats for damage scaling
}