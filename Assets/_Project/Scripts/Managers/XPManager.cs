using UnityEngine;

public class XPManager : MonoBehaviour {
    public static XPManager Instance { get; private set; }

    [Header("XP Settings")]
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int xpToNextLevel = 60;
    [SerializeField] private float xpMultiplier = 1.22f; // Tuned via tools/balance_model.py: level 10 lands ~room 10, 20 ~room 22
    [SerializeField] private bool debugMode = false;

    [Header("Level Up Rewards")]
    [SerializeField] private float healPercentOnLevelUp = 0.12f; // 12% heal
    

    private Health playerHealth;

    void Awake() {
        // Singleton pattern
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        } else {
            Instance = this;
        }

        // Get player health reference
        playerHealth = FindFirstObjectByType<PlayerController>()?.GetComponent<Health>();
        xpToNextLevel = Mathf.Max(1, xpToNextLevel);
        currentLevel = Mathf.Max(1, currentLevel);
        xpMultiplier = Mathf.Max(1f, xpMultiplier);
    }

    void OnEnable() {
        // Subscribe to enemy killed event
        GameEvents.OnEnemyKilled += AwardXP;
    }

    void OnDisable() {
        // Unsubscribe to prevent memory leaks
        GameEvents.OnEnemyKilled -= AwardXP;
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        currentXP = (int)System.Math.Min(int.MaxValue, (long)currentXP + amount);

        // Check for level up
        CheckLevelUp();
        GameEvents.XPChanged(currentXP, xpToNextLevel);
    }

    private void AwardXP(Enemy enemy) {
        if (enemy == null) return;
        // Get XP value from enemy's data
        EnemyData data = enemy.EnemyData; // You'll need to add this getter
        if (data == null) {
            if (debugMode) {
                GameEvents.DebugWarning("Enemy has no data, can't award XP", DebugCategory.XPAndLeveling);
            }
            return;
        }
        
        float multiplier = enemy.XPMultiplier * (MetaProgression.Instance != null ? MetaProgression.Instance.XPMultiplier() : 1f);
        AddXP(Mathf.Max(1, Mathf.RoundToInt(data.XPValue * multiplier)));
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
        xpToNextLevel = (int)System.Math.Min(int.MaxValue,
            System.Math.Max(1d, System.Math.Round(xpToNextLevel * (double)xpMultiplier)));

        if (debugMode) GameEvents.DebugLog($"Leveled up to {currentLevel}! Next level at {xpToNextLevel} XP.", DebugCategory.XPAndLeveling);

        // Heal player
        HealPlayer();

        // Fire level up event
        GameEvents.LevelUp(currentLevel);
    }

    private void HealPlayer() {
        if (playerHealth == null && PlayerController.Instance != null) {
            PlayerController.Instance.GetHealth(out playerHealth);
        }
        if (playerHealth != null) {
            float healAmount = playerHealth.MaxHP * healPercentOnLevelUp;
            playerHealth.Heal(healAmount);
            GameEvents.DebugLog($"Healed player for {healAmount} HP on level up.", DebugCategory.General);
        }
    } 

    // Public getters for UI
    public int GetCurrentXP() => currentXP;
    public int GetXPToNextLevel() => xpToNextLevel;
    public int GetCurrentLevel() => currentLevel;

    void Start() {
        GameEvents.XPChanged(currentXP, xpToNextLevel);
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }
}
