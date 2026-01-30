using UnityEngine;
using System.Collections;
/// <summary>
/// Interface for components that can have their movement speed modified.
/// Implemented by PlayerMovement, EnemyMovement, etc.
/// </summary>
public interface IMovementModifiable {
    void SetSpeedMultiplier(float multiplier);
}