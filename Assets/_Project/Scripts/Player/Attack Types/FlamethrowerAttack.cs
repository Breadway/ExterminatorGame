using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class FlamethrowerAttack : MonoBehaviour, IWeaponAttack {
    [Header("Flamethrower Settings")]
    [SerializeField] private float damage = 5f; // Damage per tick
    [SerializeField] private float range = 6f;
    [SerializeField] private float coneAngle = 45f; // Degrees
    [SerializeField] private float tickRate = 0.1f; // Damage every 0.1 seconds
    [SerializeField] private int raysPerFrame = 5; // Number of raycasts in cone
    
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private ParticleSystem flameParticles;
    
    private PlayerStats stats;
    private float nextTickTime;
    private HashSet<IDamageable> enemiesHitThisFrame = new HashSet<IDamageable>();
    private Coroutine attackCoroutine;
    
    void Awake() {
        stats = GetComponentInParent<PlayerController>().stats;
    }

    public void Initialize(PlayerStats playerStats) {
        stats = playerStats;
    }
    
    public void Attack() {
        // Start continuous attack loop if not already running
        if (attackCoroutine == null) {
            attackCoroutine = StartCoroutine(AttackLoop());
        }
    }

    private IEnumerator AttackLoop() {
        while (true) {
            if (Time.time >= nextTickTime) {
                enemiesHitThisFrame.Clear();

                // Cast multiple rays in a cone
                for (int i = 0; i < raysPerFrame; i++) {
                    float angle = Random.Range(-coneAngle / 2f, coneAngle / 2f);
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * firePoint.forward;

                    if (Physics.SphereCast(firePoint.position, 0.3f, direction, out RaycastHit hit, range)) {
                        IDamageable target = hit.collider.GetComponent<IDamageable>();
                        if (target != null && target.IsAlive && !enemiesHitThisFrame.Contains(target)) {
                            enemiesHitThisFrame.Add(target);
                        }
                    }

                    Debug.DrawRay(firePoint.position, direction * range, Color.red, 0.1f);
                }

                float finalDamage = damage; //* stats.currentDamage;
                foreach (IDamageable enemy in enemiesHitThisFrame) {
                    enemy.TakeDamage(finalDamage, firePoint.position, firePoint.forward);
                }

                // Emit particles
                if (flameParticles != null && !flameParticles.isPlaying) {
                    flameParticles.Play();
                }

                nextTickTime = Time.time + tickRate;
            }

            yield return null;
        }
    }
    
    public void StopAttack() {
        // Stop continuous attack loop
        if (attackCoroutine != null) {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (flameParticles != null && flameParticles.isPlaying) {
            flameParticles.Stop();
        }
    }
}