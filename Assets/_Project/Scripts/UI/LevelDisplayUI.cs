using UnityEngine;
using TMPro;

public class LevelDisplayUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private string prefix = "Level: "; // Customizable prefix

    void OnEnable() {
        GameEvents.OnLevelUp += UpdateLevelDisplay;
    }

    void OnDisable() {
        GameEvents.OnLevelUp -= UpdateLevelDisplay;
    }

    void Start() {
        // Initialize with current level
        if (XPManager.Instance != null) {
            UpdateLevelDisplay(XPManager.Instance.GetCurrentLevel());
        }
    }

    private void UpdateLevelDisplay(int newLevel) {
        if (levelText != null) {
            levelText.text = $"{prefix}{newLevel}";
        }
    }
}
