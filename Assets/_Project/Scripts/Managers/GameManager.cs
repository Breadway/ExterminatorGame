using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
    public GameState gameState;
    [SerializeField] private string gameSceneName = "TestScene";

    void Awake()
    {
        // Do not reload scene here to avoid infinite loop
        gameState = GameState.Playing;
    }
    void OnEnable()
    {
        GameEvents.OnRunEnded += HandleRunEnded;
        GameEvents.OnRunStarted += HandleRunStarted;
    }
    void OnDisable()
    {
        GameEvents.OnRunEnded -= HandleRunEnded;
        GameEvents.OnRunStarted -= HandleRunStarted;
    }

    void HandleRunStarted()
    {
        // Handle run start logic here
        Time.timeScale = 1f; // Resume the game
        gameState = GameState.Playing;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    }
    void HandleRunEnded(bool isVictory)
    {
        // Handle game over logic here
        Time.timeScale = 0f; // Pause the game
        if (!isVictory)
        {
            GameEvents.DebugLog("Game Over! Displaying Game Over Screen...", DebugCategory.General);
            Time.timeScale = 0f;
            // Show Game Over UI
            gameState = GameState.GameOver;
        }
        else
        {
            GameEvents.DebugLog("Run Completed! Displaying Victory Screen...", DebugCategory.General);
            // Show Victory UI
            gameState = GameState.Victory;
        }
    }
}