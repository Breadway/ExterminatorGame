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
    private IHoldWeapon holdWeapon;

    /// <summary>Make this the active weapon. Used by CharacterLoadout when a character is chosen.</summary>
    public void SetWeapon(IWeaponAttack weapon)
    {
        if (currentWeapon != null) currentWeapon.StopAttack();
        currentWeapon = weapon;
        holdWeapon = weapon as IHoldWeapon;
        isFiring = false;
        if (weapon != null && stats != null) weapon.Initialize(stats);
        if (weapon != null) GameEvents.WeaponEquipped(weapon.WeaponId);
    }
    
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
        // Create the controls once. Keyed on the object, not a flag: a script reload clears the
        // object but can leave a flag set, which made OnEnable throw.
        if (controls == null)
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

    void Start()
    {
        // All Awake methods have completed, regardless of component execution order.
        stats = playerController != null ? playerController.stats : null;
        currentWeapon?.Initialize(stats);
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
        if (isFiring && Time.timeScale > 0f && health != null && health.IsAlive && Time.time >= nextFireTime)
        {
            // Use PlayerStats attack speed if available, otherwise fall back to serialized value
            float attackSpeed = stats != null ? stats.currentAttackSpeed * stats.AttackSpeedBuff : baseFireRate;
            nextFireTime = Time.time + (attackSpeed > 0f ? 1f / attackSpeed : 0.5f);
            if (currentWeapon != null)
            {
                currentWeapon.Attack();
            }
            else
            {
                // Safe fallback: no weapon component found - record attempt
                GameEvents.DebugLog("PlayerAttack: attack attempted (no weapon found)", DebugCategory.Input);
            }
        }
    }

    void OnAttackStarted(InputAction.CallbackContext context)
    {
        if (holdWeapon != null) { holdWeapon.BeginHold(); return; }
        isFiring = true;
    }

    void OnAttackCanceled(InputAction.CallbackContext context)
    {
        if (holdWeapon != null) { holdWeapon.ReleaseHold(); return; }
        isFiring = false;
        if (currentWeapon != null)
        {
            currentWeapon.StopAttack();
        }
    }

    void OnDestroy()
    {
        controls?.Dispose();
    }

}
