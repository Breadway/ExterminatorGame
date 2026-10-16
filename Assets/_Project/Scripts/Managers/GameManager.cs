using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }
    public GameState gameState;
    [SerializeField] private string gameSceneName = "TestScene";

    void Awake() {
        Instance = this;
        Time.timeScale = 1f;
        gameState = GameState.Playing;
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    void OnEnable() {
        GameEvents.OnRunEnded += HandleRunEnded;
        GameEvents.OnRunStarted += HandleRunStarted;
        GameEvents.OnGamePaused += HandlePause;
        GameEvents.OnGameStateChanged += HandleStateChanged;
    }

    void OnDisable() {
        GameEvents.OnRunEnded -= HandleRunEnded;
        GameEvents.OnRunStarted -= HandleRunStarted;
        GameEvents.OnGamePaused -= HandlePause;
        GameEvents.OnGameStateChanged -= HandleStateChanged;
    }

    void HandleRunStarted() {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void HandleRunEnded(bool isVictory) {
        GameEvents.ChangeGameState(isVictory ? GameState.Victory : GameState.GameOver);
    }

    void HandlePause(bool paused) {
        if (gameState != GameState.Playing && gameState != GameState.Paused) return;
        GameEvents.ChangeGameState(paused ? GameState.Paused : GameState.Playing);
    }

    void HandleStateChanged(GameState state) {
        gameState = state;
        Time.timeScale = state == GameState.Playing ? 1f : 0f;
    }
}
