using UnityEngine;
public class EventListener : MonoBehaviour {
    // Listen for events placeholder
    private System.Action onPlayerDiedHandler;
    private System.Action<Enemy> onEnemyKilledHandler;
    private System.Action<int> onLevelUpHandler;
    private System.Action<int, int> onXPChangedHandler;

    void Awake()
    {
        onPlayerDiedHandler = () => GameEvents.DebugLog("Player died event received in EventListener.", DebugCategory.General);
        onEnemyKilledHandler = (Enemy enemy) => GameEvents.DebugLog("Enemy killed event received in EventListener.", DebugCategory.General);
        onLevelUpHandler = (int newLevel) => GameEvents.DebugLog($"Level up event received in EventListener. New level: {newLevel}", DebugCategory.XPAndLeveling);
        onXPChangedHandler = (int currentXP, int xpRequired) => GameEvents.DebugLog($"XP changed event received in EventListener. XP: {currentXP}/{xpRequired}", DebugCategory.XPAndLeveling);
    }

    void OnEnable()
    {
        GameEvents.OnPlayerDied += onPlayerDiedHandler;
        GameEvents.OnEnemyKilled += onEnemyKilledHandler;
        GameEvents.OnLevelUp += onLevelUpHandler;
        GameEvents.OnXPChanged += onXPChangedHandler;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerDied -= onPlayerDiedHandler;
        GameEvents.OnEnemyKilled -= onEnemyKilledHandler;
        GameEvents.OnLevelUp -= onLevelUpHandler;
        GameEvents.OnXPChanged -= onXPChangedHandler;
    }
}