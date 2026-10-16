using UnityEngine;

/// <summary>
/// Boss behaviour layered onto an Enemy: periodic summons, projectile rings and an enrage phase.
/// All numbers come from EnemyData so a new boss needs only an asset, not code.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class BossController : MonoBehaviour
{
    private Enemy enemy;
    private float nextSummon;
    private float nextVolley;
    private bool enraged;
    private bool announced;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    void OnEnable()
    {
        enraged = false;
        announced = false;
        ScheduleFromData();
    }

    private void ScheduleFromData()
    {
        var d = enemy != null ? enemy.EnemyData : null;
        if (d == null) return;
        nextSummon = Time.time + Mathf.Max(1f, d.summonInterval);
        nextVolley = Time.time + Mathf.Max(1f, d.volleyInterval) * 0.6f;
    }

    void Update()
    {
        var d = enemy.EnemyData;
        if (d == null || !d.isBoss || enemy.Health == null || !enemy.Health.IsAlive || Time.timeScale <= 0f) return;

        if (!announced)
        {
            announced = true;
            ScheduleFromData();
            GameEvents.BossSpawned(enemy);
        }

        if (!enraged && enemy.Health.HealthPercent <= d.enrageBelowHealth)
        {
            enraged = true;
            enemy.SetWaveSpeedMultiplier(d.enrageSpeedMultiplier);
            GameEvents.ScreenShake(0.7f);
            GameEvents.ShowNotification("WARNING - SUBJECT ENRAGED");
        }

        float tempo = enraged ? 0.6f : 1f;

        if (d.summonInterval > 0f && d.summonType != null && Time.time >= nextSummon)
        {
            nextSummon = Time.time + d.summonInterval * tempo;
            WaveSpawner.Instance?.SpawnReinforcements(d.summonCount, d.summonType);
        }

        if (d.volleyInterval > 0f && d.projectilePrefab != null && Time.time >= nextVolley)
        {
            nextVolley = Time.time + d.volleyInterval * tempo;
            FireRing(d, enraged ? Mathf.RoundToInt(d.volleyCount * 1.5f) : d.volleyCount);
        }
    }

    private void FireRing(EnemyData d, int count)
    {
        var playerMask = (LayerMask)(1 << LayerMask.NameToLayer("Player"));
        float offset = Random.value * 360f;
        for (int i = 0; i < count; i++)
        {
            float angle = (offset + 360f * i / count) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            GameObject go = PoolingSystem.Instance != null
                ? PoolingSystem.Instance.Get(d.projectilePrefab, (Vector2)transform.position + dir * 0.8f, Quaternion.identity)
                : Instantiate(d.projectilePrefab, (Vector2)transform.position + dir * 0.8f, Quaternion.identity);
            go?.GetComponent<Projectile>()?.Fire(dir, d.projectileSpeed, d.attackDamage * 0.6f, d.projectileLifetime, playerMask);
        }
    }
}
