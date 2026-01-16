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
    
    void Awake() {
        stats = GetComponentInParent<PlayerController>().stats;
    }

    public void Initialize(PlayerStats playerStats) {
        stats = playerStats;
    }
    
    public void Attack() {
        if (Time.time < nextTickTime) return;
        
        enemiesHitThisFrame.Clear();
        
        // Cast multiple rays in a cone
        for (int i = 0; i < raysPerFrame; i++) {
            // Calculate angle for this ray within cone
            float angle = Random.Range(-coneAngle / 2f, coneAngle / 2f);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * firePoint.forward;
            
            // SphereCast to hit larger area
            if (Physics.SphereCast(firePoint.position, 0.3f, direction, out RaycastHit hit, range)) {
                IDamageable target = hit.collider.GetComponent<IDamageable>();
                
                if (target != null && target.IsAlive && !enemiesHitThisFrame.Contains(target)) {
                    enemiesHitThisFrame.Add(target); // Track so we don't hit same enemy multiple times this frame
                }
            }
            
            // Debug visualization
            Debug.DrawRay(firePoint.position, direction * range, Color.red, 0.1f);
        }
        
        // Apply damage to all unique enemies hit
        float finalDamage = damage; //* stats.currentDamage; // Scale by player stats
        foreach (IDamageable enemy in enemiesHitThisFrame) {
            enemy.TakeDamage(finalDamage, firePoint.position, firePoint.forward);
        }
        
        // Emit particles
        //if (flameParticles != null && !flameParticles.isPlaying) {
            //flameParticles.Play();
            //Debug.Log("Playing flame particles");
        //}
        
        nextTickTime = Time.time + (tickRate / 0.5f);//stats.currentAttackSpeed);
    }
    
    public void StopAttack() {
        //if (flameParticles != null && flameParticles.isPlaying) {
            //flameParticles.Stop();
        //}
    }
}