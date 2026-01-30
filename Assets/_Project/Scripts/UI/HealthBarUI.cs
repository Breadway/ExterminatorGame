using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarUI : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI healthText; // Optional

    void OnEnable() {
        GameEvents.OnHealthChanged += UpdateHealthBar;
    }

    void OnDisable() {
        GameEvents.OnHealthChanged -= UpdateHealthBar;
    }

    void Start() {
        // Initialize with current values
        if (PlayerController.Instance != null) {
            Health playerHealth;
            PlayerController.Instance.GetHealth(out playerHealth);   
            UpdateHealthBar(playerHealth.CurrentHP, playerHealth.MaxHP);
        }
    }

    private void UpdateHealthBar(float currentHP, float maxHP) {
        if (fillImage != null) {
            float fillAmount = (float)currentHP / maxHP;
            fillImage.fillAmount = fillAmount;
        }

        if (healthText != null) {
            healthText.text = $"{currentHP} / {maxHP}";
        }
    }
}