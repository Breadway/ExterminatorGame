using UnityEngine;

/// <summary>
/// Orchestrates the player's subsystems (Movement, Aiming, Health, etc).
/// Responsibility: Initialize components and coordinate high-level player behavior.
/// Does NOT do work itself—tells other systems to act.
/// </summary>
public class PlayerController : MonoBehaviour
{   
    private PlayerMovement movement;
    private PlayerAim aim;
    private Health health;

    private void Awake()
    {
        // Cache references to player subsystems
        movement = GetComponent<PlayerMovement>();
        aim = GetComponent<PlayerAim>();
        health = GetComponent<Health>();
        
        // Validate all required components exist
        if (movement == null) Debug.LogError("PlayerController: PlayerMovement component not found!");
        if (aim == null) Debug.LogError("PlayerController: PlayerAim component not found!");
        if (health == null) Debug.LogError("PlayerController: Health component not found!");
    }

    private void Start()
    {
        // Initialize subsystems
    }
}