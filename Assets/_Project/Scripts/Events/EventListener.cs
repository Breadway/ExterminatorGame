using UnityEngine;
public class EventListener : MonoBehaviour {
    // Listen for events placeholder
    void OnEnable()
    {
        GameEvents.OnEnemyKilled += (Enemy enemy) => Debug.Log("Enemy killed event received in EventListener.");
    }

    void OnDisable()
    {
        GameEvents.OnEnemyKilled -= (Enemy enemy) => Debug.Log("Enemy killed event received in EventListener.");
    }
}