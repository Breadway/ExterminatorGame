using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour {
    [Header("Components")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAiming aiming;
    [SerializeField] private Health health;
    [SerializeField] private float baseFireRate = 0.5f; // shots per second
    [SerializeField] private Transform firePoint;
    private PlayerControls controls;
    private bool isFiring = false;
    private float nextFireTime = 0f;
    private IWeaponAttack currentWeapon;
    void Awake()
    {
        controls = new PlayerControls();
        playerController = GetComponent<PlayerController>();
        aiming = GetComponent<PlayerAiming>();
        health = GetComponent<Health>();
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
    }

    void OnEnable()
    {
        controls.Player.Attack.Enable();
        controls.Player.Attack.started += OnAttackStarted;
        controls.Player.Attack.canceled += OnAttackCanceled;
    }

    void OnDisable()
    {
        controls.Player.Attack.started -= OnAttackStarted;
        controls.Player.Attack.canceled -= OnAttackCanceled;
        controls.Player.Attack.Disable();
    }

    void Update()
    {
        // Handle simple auto-fire behavior when holding the attack
        if (isFiring && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + (baseFireRate > 0f ? 1f / baseFireRate : 0.5f);
            if (currentWeapon != null)
            {
                currentWeapon.Attack();
            }
            else
            {
                // Safe fallback: no weapon component found — record attempt
                Debug.Log("PlayerAttack: attack attempted (no weapon found)");
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
            Debug.Log("PlayerAttack: Attack started (no weapon found)");
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