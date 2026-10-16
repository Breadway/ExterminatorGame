using UnityEngine;

/// <summary>
/// Drops healing from defeated enemies. Chance comes from UpgradeEffects (Scavenger raises it) and
/// bosses always drop a large one.
/// </summary>
public class PickupSpawner : MonoBehaviour
{
    [SerializeField] private GameObject healPrefab;
    [SerializeField] [Range(0f, 1f)] private float baseDropChance = 0.04f;
    [SerializeField] private float healFraction = 0.15f;
    [SerializeField] private float bossHealFraction = 0.4f;

    void OnEnable() { GameEvents.OnEnemyKilled += HandleKill; }
    void OnDisable() { GameEvents.OnEnemyKilled -= HandleKill; }

    private void HandleKill(Enemy enemy)
    {
        if (healPrefab == null || enemy == null) return;
        bool boss = enemy.IsBoss;
        float chance = Mathf.Max(baseDropChance, UpgradeEffects.HealDropChance());
        if (!boss && Random.value > chance) return;

        var go = Instantiate(healPrefab, enemy.transform.position, Quaternion.identity);
        var pickup = go.GetComponent<Pickup>();
        if (pickup != null)
        {
            pickup.kind = PickupKind.Heal;
            pickup.amount = boss ? bossHealFraction : healFraction;
        }
    }
}
