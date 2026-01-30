using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class XPBarUI : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI xpText; // Optional

    void OnEnable() {
        GameEvents.OnXPChanged += UpdateXPBar;
    }

    void OnDisable() {
        GameEvents.OnXPChanged -= UpdateXPBar;
    }

    void Start() {
        // Initialize with current values
        if (XPManager.Instance != null) {
            UpdateXPBar(XPManager.Instance.GetCurrentXP(), XPManager.Instance.GetXPToNextLevel());
        }
    }

    private void UpdateXPBar(int currentXP, int xpRequired) {
        if (fillImage != null) {
            float fillAmount = (float)currentXP / xpRequired;
            fillImage.fillAmount = fillAmount;
        }

        if (xpText != null) {
            xpText.text = $"{currentXP} / {xpRequired}";
        }
    }
}