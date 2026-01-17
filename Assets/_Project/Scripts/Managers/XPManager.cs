using UnityEngine;

public class XPManager : MonoBehaviour {
    public static XPManager Instance { get; private set; }

    [Header("XP Settings")]
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int xpToNextLevel = 100;
    [SerializeField] private float xpMultiplier = 1.5f; // How much XP increases per level

    [Header("Level Up Rewards")]
    [SerializeField] private float healPercentOnLevelUp = 0.1f; // 10% heal

    private Health playerHealth;

    void Awake() {
        // Singleton pattern
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
        } else {
            Instance = this;
        }

        // Get player health reference
        playerHealth = FindFirstObjectByType<PlayerController>()?.GetComponent<Health>();
    }

    void OnEnable() {
        // Subscribe to enemy killed event
        GameEvents.OnEnemyKilled += AwardXP;
    }

    void OnDisable() {
        // Unsubscribe to prevent memory leaks
        GameEvents.OnEnemyKilled -= AwardXP;
    }

    private void AwardXP(Enemy enemy) {
        // Get XP value from enemy's data
        EnemyData data = enemy.EnemyData; // You'll need to add this getter
        if (data == null) {
            Debug.LogWarning("Enemy has no data, can't award XP");
            return;
        }

        int xpGained = data.XPValue;
        currentXP += xpGained;

        Debug.Log($"Gained {xpGained} XP! Total: {currentXP}/{xpToNextLevel}");

        // Fire event so UI can update
        GameEvents.XPChanged(currentXP, xpToNextLevel);

        // Check if leveled up
        CheckLevelUp();
    }

    private void CheckLevelUp() {
        while (currentXP >= xpToNextLevel) {
            LevelUp();
        }
    }

    private void LevelUp() {
        currentLevel++;
        currentXP -= xpToNextLevel;

        // Calculate next level requirement (exponential curve)
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpMultiplier);

        Debug.Log($"LEVEL UP! Now level {currentLevel}. Next level requires {xpToNextLevel} XP.");

        // Heal player
        HealPlayer();

        // Fire level up event
        GameEvents.LevelUp(currentLevel);
    }

    private void HealPlayer() {
        if (playerHealth != null) {
            float healAmount = playerHealth.MaxHP * healPercentOnLevelUp;
            playerHealth.Heal(healAmount);
            Debug.Log($"Healed {healAmount} HP on level up!");
        }
    }

    // Public getters for UI
    public int GetCurrentXP() => currentXP;
    public int GetXPToNextLevel() => xpToNextLevel;
    public int GetCurrentLevel() => currentLevel;
}