using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour {
    [Header("Components")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAiming aiming;
    [SerializeField] private Health health;
    [SerializeField] private float baseFireRate = 0.5f; // shots per second (fallback)
    [SerializeField] private Transform firePoint;
    private PlayerControls controls;
    private PlayerStats stats;
    private bool isInitialized = false;
    private bool isFiring = false;
    private float nextFireTime = 0f;
    private IWeaponAttack currentWeapon;
    
    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        aiming = GetComponent<PlayerAiming>();
        health = GetComponent<Health>();
        
        // Get PlayerStats from PlayerController
        if (playerController != null)
        {
            stats = playerController.stats;
        }
        
        // Try to find any component that implements IWeaponAttack safely
        currentWeapon = GetComponent<IWeaponAttack>();
        if (currentWeapon == null) {
            // Try non-generic lookup and cast (covers some Unity versions)
            var comp = GetComponent(typeof(IWeaponAttack));
            if (comp is IWeaponAttack casted) currentWeapon = casted;
        }
        if (currentWeapon == null) {
            // Try children as a fallback
            currentWeapon = GetComponentInChildren<IWeaponAttack>();
        }
        
        // Initialize weapon with player stats for damage calculations
        if (currentWeapon != null && stats != null)
        {
            currentWeapon.Initialize(stats);
        }
    }

    void OnEnable()
    {
        // Only create controls once to avoid multiple input listeners
        if (!isInitialized)
        {
            controls = new PlayerControls();
            isInitialized = true;
        }
        
        // Reset firing state when enabled to prevent stuck attacking
        isFiring = false;
        
        controls.Player.Attack.Enable();
        controls.Player.Attack.started += OnAttackStarted;
        controls.Player.Attack.canceled += OnAttackCanceled;
    }

    void OnDisable()
    {
        // Stop any active attack when disabled
        isFiring = false;
        if (currentWeapon != null)
        {
            currentWeapon.StopAttack();
        }
        
        if (controls != null)
        {
            controls.Player.Attack.started -= OnAttackStarted;
            controls.Player.Attack.canceled -= OnAttackCanceled;
            controls.Player.Attack.Disable();
        }
    }

    void Update()
    {
        // Handle simple auto-fire behavior when holding the attack
        if (isFiring && Time.time >= nextFireTime)
        {
            // Use PlayerStats attack speed if available, otherwise fall back to serialized value
            float attackSpeed = stats != null ? stats.currentAttackSpeed : baseFireRate;
            nextFireTime = Time.time + (attackSpeed > 0f ? 1f / attackSpeed : 0.5f);
            if (currentWeapon != null)
            {
                currentWeapon.Attack();
            }
            else
            {
                // Safe fallback: no weapon component found — record attempt
                GameEvents.DebugLog("PlayerAttack: attack attempted (no weapon found)", DebugCategory.Input);
            }
        }
    }

    void OnAttackStarted(InputAction.CallbackContext context)
    {
        isFiring = true;
        if (currentWeapon != null)
        {
            currentWeapon.Attack();
        }
        else
        {
            GameEvents.DebugLog("PlayerAttack: Attack started (no weapon found)", DebugCategory.Input);
        }
    }

    void OnAttackCanceled(InputAction.CallbackContext context)
    {
        isFiring = false;
        if (currentWeapon != null)
        {
            currentWeapon.StopAttack();
        }
    }

}