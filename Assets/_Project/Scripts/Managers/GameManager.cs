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
        Time.timeScale = 1f;
    }
    void OnEnable()
    {
        GameEvents.OnRunEnded += HandleRunEnded;
        GameEvents.OnRunStarted += RunStarted;
    }
    void OnDisable()
    {
        GameEvents.OnRunEnded -= HandleRunEnded;
        GameEvents.OnRunStarted -= RunStarted;
    }

    void RunStarted()
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
            Debug.Log("Game Over! Displaying Game Over Screen...");
            Time.timeScale = 0f;
            // Show Game Over UI
            gameState = GameState.GameOver;
        }
        else
        {
            Debug.Log("Run Completed! Displaying Victory Screen...");
            // Show Victory UI
            gameState = GameState.Victory;
        }
    }
}