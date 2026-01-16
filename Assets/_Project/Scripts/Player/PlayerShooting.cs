using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour {
    [Header("Components")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerAiming aiming;
    [SerializeField] private Health health;

    [SerializeField] private int baseDamage = 10;
    [SerializeField] private float baseFireRate = 0.5f; // shots per second
    [SerializeField] private float range = 10f;
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
        currentWeapon = GetComponent<IWeaponAttack>();
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
       
    }

    void OnAttackStarted(InputAction.CallbackContext context)
    {
        currentWeapon.Attack();
    }

    void OnAttackCanceled(InputAction.CallbackContext context)
    {
        currentWeapon.StopAttack();
    }

}