using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Apiarist's swarm. Her bees orbit her, attack marked enemies for her, and die in her place:
/// she has 50 HP, cannot be healed, and every point of damage is a dead bee (one bee = one HP).
/// Killing marked enemies grows the swarm up to 30, and every bee adds +2% weapon damage. With no
/// bees and empty HP she dies like anyone else.
/// </summary>
public class BeeHive : MonoBehaviour, ICharacterConstraint, IDamageAbsorber
{
    public const string Id = "apiarist";
    public string CharacterId => Id;

    [Header("Swarm")]
    [SerializeField] private int maxBees = 30;
    [SerializeField] private int startBees = 8;
    [SerializeField] private float beeHP = 50f;
    [SerializeField] private float damagePerBeeFraction = 0.02f;

    [Header("Combat")]
    [SerializeField] private float attackRadius = 7f;
    [SerializeField] private float stingInterval = 0.5f;
    [Tooltip("How many bees may gang up on one enemy.")]
    [SerializeField] private int maxAttackersPerEnemy = 3;
    [Tooltip("Only enemies marked this recently are valid bee targets.")]
    [SerializeField] private float markGrace = 1.5f;

    private class Bee
    {
        public Transform body;
        public SpriteRenderer glow;
        public Enemy target;
        public float phase;
        public float cooldown;
        public Color color;
    }

    private readonly List<Bee> bees = new List<Bee>();
    private readonly Dictionary<Enemy, float> marks = new Dictionary<Enemy, float>();
    private PlayerController owner;
    private Health health;
    private string boundCharacter;
    private float pulseAt;

    public int BeeCount => bees.Count;
    public bool IsActive => owner != null && owner.CharacterId == Id && boundCharacter == Id;

    void Awake()
    {
        owner = GetComponent<PlayerController>();
        health = GetComponent<Health>();
    }

    /// <summary>Called by CharacterLoadout: own the constraint only when the Apiarist is selected.</summary>
    public void Bind(string characterId)
    {
        boundCharacter = characterId;
        if (IsActive)
        {
            health.AddAbsorber(this);
            health.SetMaxHP(beeHP);
            health.ResetHealth();
            health.SetCanHeal(false);
            // The 50 HP cap stands, but paid-for damage and speed bonuses still apply
            var meta = MetaProgression.Instance;
            owner.stats.SetMetaBonuses(0f,
                meta != null ? meta.Bonus(MetaStat.Damage) : 0f,
                meta != null ? meta.Bonus(MetaStat.MoveSpeed) : 0f);
            owner.stats.SetBuff("bees", 1f, 1f, 1f + bees.Count * damagePerBeeFraction);
            while (bees.Count < startBees) AddBee();
        }
        else
        {
            health.RemoveAbsorber(this);
            health.SetCanHeal(true);
            owner.stats.ClearBuff("bees");
            visible(false);
        }
    }

    void OnEnable()
    {
        GameEvents.OnEnemyKilled += HandleKill;
        GameEvents.OnRunStarted += ResetRun;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyKilled -= HandleKill;
        GameEvents.OnRunStarted -= ResetRun;
        if (health != null) health.RemoveAbsorber(this);
    }

    private void ResetRun()
    {
        bees.Clear();
        marks.Clear();
        boundCharacter = null;
    }

    // ---- marking ----
    public void Mark(Enemy enemy, float seconds)
    {
        if (!IsActive || enemy == null) return;
        marks[enemy] = Time.time + seconds + markGrace;
    }

    private bool IsMarked(Enemy enemy)
    {
        if (enemy == null) return false;
        if (marks.TryGetValue(enemy, out float until))
        {
            if (Time.time <= until) return true;
            marks.Remove(enemy);
        }
        return false;
    }

    // ---- bees as a shield: damage kills bees first, one bee per point ----
    public float Absorb(float amount, Vector2 hitPoint)
    {
        if (!IsActive || bees.Count == 0) return amount;
        int lose = Mathf.Min(bees.Count, Mathf.Max(1, Mathf.FloorToInt(amount)));
        amount -= lose;
        for (int i = 0; i < lose; i++)
        {
            var bee = bees[bees.Count - 1];
            bees.RemoveAt(bees.Count - 1);
            if (bee.body != null) Destroy(bee.body.gameObject);
        }
        GameEvents.ShowNotification(lose >= 10 ? $"SWARM -{lose}" : null);
        RetuneDamage();
        return amount;
    }

    private void HandleKill(Enemy enemy)
    {
        if (!IsActive || enemy == null) return;
        // Only marked kills feed the hive; anything else was never yours
        if (!IsMarked(enemy)) return;
        AddBee();
        GameEvents.PlaySound("pickup");
    }

    private void AddBee()
    {
        if (bees.Count >= maxBees || gameObject == null) return;
        var go = new GameObject("Bee");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BeeSprite.Instance != null ? BeeSprite.Instance.Pick() : null;
        sr.sortingOrder = 9;
        sr.color = ColorFor(bees.Count);
        bees.Add(new Bee { body = go.transform, glow = sr, phase = Random.value * 6.28f, color = sr.color });
        RetuneDamage();
    }

    private static Color ColorFor(int index)
    {
        Color[] hive = { new Color(1f, 0.85f, 0.2f), new Color(1f, 0.7f, 0.15f), new Color(0.95f, 0.6f, 0.2f) };
        return hive[index % hive.Length];
    }

    private void RetuneDamage()
    {
        if (owner != null && owner.stats != null)
            owner.stats.SetBuff("bees", 1f, 1f, 1f + bees.Count * damagePerBeeFraction);
    }

    // ---- bees as a weapon ----
    void Update()
    {
        if (!IsActive || Time.timeScale <= 0f || bees.Count == 0) return;
        visible(true);

        var assigned = new Dictionary<Enemy, int>();
        for (int i = 0; i < bees.Count; i++)
        {
            var bee = bees[i];
            if (bee.body == null) continue;

            if (bee.target == null || bee.target.Health == null || !bee.target.Health.IsAlive || !IsMarked(bee.target))
                bee.target = FindTarget(assigned);
            if (bee.target != null)
            {
                if (!assigned.ContainsKey(bee.target)) assigned[bee.target] = 0;
                assigned[bee.target]++;
            }

            // Ring formation around you, each bee darting at its target
            float angle = Time.time * 1.7f + bee.phase;
            Vector2 home = (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.8f) * 1.1f;
            Vector2 pos = bee.body.position;
            Vector2 goal = home;
            if (bee.target != null)
            {
                Vector2 tp = bee.target.transform.position;
                goal = Vector2.Distance(home, tp) < 0.5f ? (Vector2)tp : home;
                if (Vector2.Distance(pos, tp) < 0.45f && Time.time >= bee.cooldown)
                {
                    bee.cooldown = Time.time + stingInterval;
                    float damage = Mathf.Max(1f, owner.stats.currentDamage * 0.35f);
                    bee.target.Health.TakeDamage(damage, tp, (tp - pos).normalized);
                    if (Fx.Instance != null) Fx.Instance.Spark(tp, 2, bee.color, 0.08f);
                    GameEvents.PlaySound("hit_2", tp);
                }
            }
            bee.body.position = Vector2.MoveTowards(pos, goal, 9f * Time.deltaTime);
            // Excited flicker when heading to a target, slow breathing otherwise
            if (bee.glow != null)
            {
                float pulse = bee.target != null ? 0.75f + 0.25f * Mathf.Sin(Time.time * 30f + bee.phase) : 0.9f + 0.1f * Mathf.Sin(Time.time * 5f + bee.phase);
                var c = bee.color;
                c.a = pulse;
                bee.glow.color = c;
            }
        }

        if (Time.unscaledTime >= pulseAt)
        {
            pulseAt = Time.unscaledTime + 2f;
            GameEvents.PlaySound("ui_hover");
        }
    }

    private Enemy FindTarget(Dictionary<Enemy, int> assigned)
    {
        Enemy best = null;
        float bestD = float.MaxValue;
        var cache = new List<Enemy>();
        foreach (var kv in marks) if (kv.Key != null && Time.time <= kv.Value + markGrace) cache.Add(kv.Key);
        foreach (var e in cache)
        {
            if (e.Health == null || !e.Health.IsAlive || !e.gameObject.activeInHierarchy) continue;
            if (assigned.TryGetValue(e, out int n) && n >= maxAttackersPerEnemy) continue;
            float d = Vector2.Distance(e.transform.position, transform.position);
            if (d < bestD) { bestD = d; best = e; }
        }
        return bestD <= attackRadius ? best : null;
    }

    private void visible(bool on)
    {
        foreach (var bee in bees) if (bee.body != null) bee.body.gameObject.SetActive(on);
    }
}

/// <summary>Holds the generated bee sprites. Real sprites are assigned in scene wiring; until then bees are invisible.</summary>
public class BeeSprite : MonoBehaviour
{
    public static BeeSprite Instance { get; private set; }
    public Sprite[] frames;
    void Awake() { if (Instance == null) Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }
    public Sprite Pick() => frames != null && frames.Length > 0 ? frames[Random.Range(0, frames.Length)] : null;
}
