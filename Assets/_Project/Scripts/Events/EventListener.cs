using UnityEngine;
public class EventListener : MonoBehaviour {
    // Listen for events placeholder
    private System.Action onPlayerDiedHandler;
    private System.Action<Enemy> onEnemyKilledHandler;
    private System.Action<int> onLevelUpHandler;
    private System.Action<int, int> onXPChangedHandler;

    void Awake()
    {
        onPlayerDiedHandler = () => Debug.Log("Player died event received in EventListener.");
        onEnemyKilledHandler = (Enemy enemy) => Debug.Log("Enemy killed event received in EventListener.");
        onLevelUpHandler = (int newLevel) => Debug.Log($"Level up event received in EventListener. New level: {newLevel}");
        onXPChangedHandler = (int currentXP, int xpRequired) => Debug.Log($"XP changed event received in EventListener. XP: {currentXP}/{xpRequired}");
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