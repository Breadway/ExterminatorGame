using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gives behaviour to upgrades with a specialEffectID. It only reads PlayerStats (which effects
/// the player owns) and reacts to GameEvents, so it adds no coupling to weapons or enemies.
/// Effect ids: chain_lightning, explode_on_kill, lifesteal, second_wind, adrenaline, scavenger.
/// </summary>
public class UpgradeEffects : MonoBehaviour
{
    public const string ChainLightning = "chain_lightning";
    public const string ExplodeOnKill = "explode_on_kill";
    public const string Lifesteal = "lifesteal";
    public const string SecondWind = "second_wind";
    public const string Adrenaline = "adrenaline";
    public const string Scavenger = "scavenger";
    public const string Executioner = "executioner";
    public const string Berserker = "berserker_rage";
    public const string Momentum = "momentum";
    public const string TimeDilation = "time_dilation";
    public const string Venom = "venom_rounds";
    public const string Emergency = "emergency_protocols";

    [Header("Chain Lightning")]
    [SerializeField] private float chainRadius = 4f;
    [SerializeField] private int chainTargets = 2;
    [SerializeField] private float chainDamageFraction = 0.4f;
    [SerializeField] private float chainCooldown = 0.2f;

    [Header("Explosive Finale")]
    [SerializeField] private float explosionRadius = 2.6f;
    [SerializeField] private float explosionDamageFraction = 1.4f;

    [Header("Life Leech")]
    [SerializeField] private int killsPerHeal = 6;

    [Header("Second Wind")]
    [SerializeField] private float reviveHealthFraction = 0.4f;
    [SerializeField] private float reviveGraceSeconds = 1.5f;

    [Header("Adrenaline Surge")]
    [SerializeField] private float adrenalineBelowHealth = 0.35f;
    [SerializeField] private float adrenalineBoost = 1.3f;

    [Header("Executioner")]
    [SerializeField] private float executionBonusBelow = 0.3f;
    [SerializeField] private float executionBonusFraction = 0.5f;
    [SerializeField] private float executeBelow = 0.08f;

    [Header("Berserker Rage")]
    [SerializeField] private float berserkerMaxBonus = 0.6f;

    [Header("Momentum")]
    [SerializeField] private float momentumPerKill = 0.08f;
    [SerializeField] private float momentumMax = 0.4f;
    [SerializeField] private float momentumWindow = 4f;

    [Header("Time Dilation")]
    [SerializeField] private float dilationRadius = 7f;
    [SerializeField] private float dilationSlow = 0.65f;
    [SerializeField] private float dilationTick = 0.3f;

    [Header("Venom Rounds")]
    [SerializeField] private StatusEffectData poisonEffect;
    [SerializeField] [Range(0f, 1f)] private float venomChance = 0.35f;

    [Header("Emergency Protocols")]
    [SerializeField] private float emergencyBelow = 0.3f;
    [SerializeField] private float emergencyHealFraction = 0.25f;

    [Header("Layers")]
    [SerializeField] private LayerMask enemyMask;

    private static readonly Collider2D[] buffer = new Collider2D[48];
    private PlayerController player;
    private Health playerHealth;
    private float nextChainTime;
    private int killCounter;
    private bool secondWindUsed;
    private bool inChain;
    private float momentumStacks;
    private float momentumTimer;
    private float dilationTimer;
    private int emergencyRoom = int.MinValue;

    void Awake()
    {
        if (enemyMask.value == 0)
        {
            int layer = LayerMask.NameToLayer("Enemy");
            if (layer >= 0) enemyMask = 1 << layer;
        }
    }

    void OnEnable()
    {
        GameEvents.OnEnemyHit += HandleEnemyHit;
        GameEvents.OnEnemyKilled += HandleEnemyKilled;
        GameEvents.OnHealthChanged += HandleHealthChanged;
        GameEvents.OnRunStarted += ResetRun;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyHit -= HandleEnemyHit;
        GameEvents.OnEnemyKilled -= HandleEnemyKilled;
        GameEvents.OnHealthChanged -= HandleHealthChanged;
        GameEvents.OnRunStarted -= ResetRun;
        if (playerHealth != null) playerHealth.OnWouldDie -= HandlePlayerDying;
    }

    private void ResetRun()
    {
        secondWindUsed = false;
        killCounter = 0;
        momentumStacks = 0f;
        emergencyRoom = int.MinValue;
    }

    private bool Bind()
    {
        if (player != null && playerHealth != null) return player.stats != null;
        player = PlayerController.Instance;
        if (player == null) return false;
        player.GetHealth(out playerHealth);
        if (playerHealth != null)
        {
            playerHealth.OnWouldDie -= HandlePlayerDying;
            playerHealth.OnWouldDie += HandlePlayerDying;
        }
        return player.stats != null;
    }

    // ---- Chain lightning, executioner, venom ----
    private void HandleEnemyHit(Enemy enemy, float amount)
    {
        if (inChain || enemy == null || !Bind()) return;

        if (player.stats.CountEffect(ChainLightning) > 0 && Time.time >= nextChainTime)
        {
            nextChainTime = Time.time + chainCooldown;
            inChain = true; // chained damage must not chain again
            try
            {
                int stacks = player.stats.CountEffect(ChainLightning);
                int hits = Physics2D.OverlapCircle(enemy.transform.position, chainRadius, new ContactFilter2D { useLayerMask = true, layerMask = enemyMask, useTriggers = true }, buffer);
                int applied = 0;
                for (int i = 0; i < hits && applied < chainTargets + (stacks - 1); i++)
                {
                    var other = buffer[i] != null ? buffer[i].GetComponentInParent<Enemy>() : null;
                    if (other == null || other == enemy || other.Health == null || !other.Health.IsAlive) continue;
                    other.Health.TakeDamage(amount * chainDamageFraction, other.transform.position, Vector2.zero);
                    applied++;
                }
            }
            finally { inChain = false; }
        }

        // Executioner: wounded prey does not escape. Bonus damage below a threshold,
        // and non-boss enemies near death are finished outright.
        int execution = player.stats.CountEffect(Executioner);
        if (execution > 0 && enemy.Health != null && enemy.Health.IsAlive && !enemy.IsBoss)
        {
            float fraction = enemy.Health.HealthPercent;
            if (fraction <= executeBelow)
            {
                inChain = true;
                try { enemy.Health.TakeDamage(99999f, enemy.transform.position, Vector2.zero); }
                finally { inChain = false; }
                GameEvents.PlaySound("crit", enemy.transform.position);
                return;
            }
            if (fraction <= executionBonusBelow)
            {
                inChain = true;
                try { enemy.Health.TakeDamage(amount * executionBonusFraction * execution, enemy.transform.position, Vector2.zero); }
                finally { inChain = false; }
            }
        }

        // Venom Rounds: hits sometimes poison. Poison stacks, so sustained fire melts.
        if (poisonEffect != null && player.stats.CountEffect(Venom) > 0 && enemy.Health != null && enemy.Health.IsAlive)
        {
            if (Random.value < venomChance)
            {
                var controller = enemy.GetComponent<StatusEffectController>();
                if (controller != null) controller.ApplyEffect(poisonEffect);
            }
        }
    }

    // ---- Explosive finale and life leech ----
    private void HandleEnemyKilled(Enemy enemy)
    {
        if (enemy == null || !Bind()) return;

        if (player.stats.CountEffect(Lifesteal) > 0)
        {
            killCounter++;
            int needed = Mathf.Max(1, killsPerHeal - (player.stats.CountEffect(Lifesteal) - 1));
            if (killCounter >= needed)
            {
                killCounter = 0;
                playerHealth.Heal(1f + player.stats.CountEffect(Lifesteal));
            }
        }

        if (player.stats.CountEffect(Momentum) > 0)
        {
            momentumStacks = Mathf.Min(momentumMax, momentumStacks + momentumPerKill * player.stats.CountEffect(Momentum));
            momentumTimer = Time.time + momentumWindow;
            player.stats.SetBuff("momentum", 1f + momentumStacks, 1f, 1f);
        }

        int explosions = player.stats.CountEffect(ExplodeOnKill);
        if (explosions > 0 && !inChain)
        {
            inChain = true;
            try
            {
                float damage = Mathf.Max(3f, player.stats.currentDamage * explosionDamageFraction * explosions);
                int hits = Physics2D.OverlapCircle(enemy.transform.position, explosionRadius, new ContactFilter2D { useLayerMask = true, layerMask = enemyMask, useTriggers = true }, buffer);
                for (int i = 0; i < hits; i++)
                {
                    var other = buffer[i] != null ? buffer[i].GetComponentInParent<Enemy>() : null;
                    if (other == null || other == enemy || other.Health == null || !other.Health.IsAlive) continue;
                    other.Health.TakeDamage(damage, other.transform.position, Vector2.zero);
                }
                GameEvents.ScreenShake(0.25f);
            }
            finally { inChain = false; }
        }
    }

    // ---- Second wind ----
    private void HandlePlayerDying()
    {
        if (secondWindUsed || !Bind() || player.stats.CountEffect(SecondWind) == 0) return;
        secondWindUsed = true;
        playerHealth.Revive(reviveHealthFraction);
        playerHealth.GrantInvulnerability(reviveGraceSeconds);
        GameEvents.ScreenShake(0.6f);
        GameEvents.ShowNotification("SECOND WIND");
    }

    // ---- Adrenaline surge, berserker rage, emergency protocols ----
    private void HandleHealthChanged(float current, float max)
    {
        if (!Bind()) return;
        float fraction = max > 0f ? current / max : 1f;
        bool active = player.stats.CountEffect(Adrenaline) > 0 && max > 0f && fraction <= adrenalineBelowHealth;
        if (active) player.stats.SetBuff("adrenaline", adrenalineBoost, adrenalineBoost, 1f);
        else player.stats.ClearBuff("adrenaline");

        // Berserker Rage: the angrier (more hurt) you get, the harder you hit
        int rage = player.stats.CountEffect(Berserker);
        if (rage > 0) player.stats.SetBuff("berserker", 1f, 1f, 1f + (1f - fraction) * berserkerMaxBonus * rage);
        else player.stats.ClearBuff("berserker");

        // Emergency Protocols: damage control kicks in automatically when things get rough, once per room
        int room = RoomManager.Instance != null ? RoomManager.Instance.CurrentRoom : 0;
        if (player.stats.CountEffect(Emergency) > 0 && fraction <= emergencyBelow && emergencyRoom != room && current > 0f)
        {
            emergencyRoom = room;
            playerHealth.Heal(max * emergencyHealFraction);
            GameEvents.PlaySound("heal");
            GameEvents.ShowNotification("EMERGENCY PROTOCOLS");
        }
    }

    void Update()
    {
        // Momentum fades when the killing stops
        if (momentumStacks > 0f && Time.time >= momentumTimer)
        {
            momentumStacks = 0f;
            if (player != null && player.stats != null) player.stats.ClearBuff("momentum");
        }

        // Time Dilation Field: everything near you moves through syrup
        if (player != null && player.stats != null && player.stats.CountEffect(TimeDilation) > 0 && Time.timeScale > 0f)
        {
            if (Time.time >= dilationTimer)
            {
                dilationTimer = Time.time + dilationTick;
                int hits = Physics2D.OverlapCircle(transform.position, dilationRadius, new ContactFilter2D { useLayerMask = true, layerMask = enemyMask, useTriggers = true }, buffer);
                for (int i = 0; i < hits; i++)
                {
                    var enemy = buffer[i] != null ? buffer[i].GetComponentInParent<Enemy>() : null;
                    if (enemy == null) continue;
                    enemy.ApplyHazardSlow(dilationSlow, dilationTick * 2.5f);
                }
            }
        }
    }

    /// <summary>Scavenger scales scrap; read by ScrapManager.</summary>
    public static float ScrapMultiplier()
    {
        var player = PlayerController.Instance;
        if (player == null || player.stats == null) return 1f;
        return 1f + 0.25f * player.stats.CountEffect(Scavenger);
    }

    public static float HealDropChance()
    {
        var player = PlayerController.Instance;
        if (player == null || player.stats == null) return 0f;
        return 0.04f + 0.06f * player.stats.CountEffect(Scavenger);
    }
}
