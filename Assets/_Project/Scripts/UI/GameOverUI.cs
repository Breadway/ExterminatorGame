using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour {
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject restartButton;

    void Awake()
    {
        // Keep this component enabled; only hide the panel content
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        if (restartButton != null)
        {
            restartButton.SetActive(false);
        }
    }

    private void OnEnable() {
        GameEvents.OnRunEnded += ShowGameOver;
        if (restartButton != null)
        {
            UnityEngine.UI.Button btn = restartButton.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(RestartGame);
            }
        }
    }

    private void OnDisable() {
        GameEvents.OnRunEnded -= ShowGameOver;
        if (restartButton != null)
        {
            UnityEngine.UI.Button btn = restartButton.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(RestartGame);
            }
        }
    }

    void ShowGameOver(bool isVictory) {
        if (!isVictory)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            if (restartButton != null)
            {
                restartButton.SetActive(true);
            }
        }
        else
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
            if (restartButton != null)
            {
                restartButton.SetActive(false);
            }
        }
    }

    public void RestartGame()
    {
        GameEvents.StartRun();
    }
}