using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controls the main gameplay HUD using UI Toolkit.
/// Subscribes to GameEvents to update health, XP, level, etc.
/// Theme: Industrial "Company Safety Manual" style.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class GameplayHUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;
    
    [Header("Settings")]
    [SerializeField] private float lowHealthThreshold = 0.3f; // 30% HP = low health warning
    [SerializeField] private float notificationDuration = 2f; // How long notifications stay visible
    
    // UI Element References
    private VisualElement root;
    
    // Health Bar
    private VisualElement healthBarFill;
    private Label healthText;
    
    // XP Bar
    private VisualElement xpBarFill;
    private Label xpText;
    
    // Level Display
    private Label levelValue;
    
    // Room Info
    private Label roomValue;
    private Label enemiesValue;
    
    // Notification
    private VisualElement notificationPanel;
    private Label notificationText;
    private float notificationTimer = 0f;
    
    // Cached Values
    private float currentHealth = 100f;
    private float maxHealth = 100f;
    private int currentXP = 0;
    private int requiredXP = 100;
    private int currentLevel = 1;
    private int currentRoom = 1;
    private int enemiesRemaining = 0;
    
    
    // ========================================
    // INITIALIZATION
    // ========================================
    
    void Awake()
    {
        // Auto-assign if not set
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }
    }
    
    void OnEnable()
    {
        // Wait one frame for UI to be fully initialized
        Invoke(nameof(InitializeUI), 0.1f);
        
        // Subscribe to events
        GameEvents.OnHealthChanged += UpdateHealth;
        GameEvents.OnXPChanged += UpdateXP;
        GameEvents.OnLevelUp += UpdateLevel;
        GameEvents.OnRoomEntered += UpdateRoom;
        GameEvents.OnRoomCleared += OnRoomCleared;
        GameEvents.OnEnemyKilled += OnEnemyKilled;
    }
    
    void OnDisable()
    {
        // Unsubscribe from events
        GameEvents.OnHealthChanged -= UpdateHealth;
        GameEvents.OnXPChanged -= UpdateXP;
        GameEvents.OnLevelUp -= UpdateLevel;
        GameEvents.OnRoomEntered -= UpdateRoom;
        GameEvents.OnRoomCleared -= OnRoomCleared;
        GameEvents.OnEnemyKilled -= OnEnemyKilled;
    }
    
    void InitializeUI()
    {
        if (uiDocument == null || uiDocument.rootVisualElement == null)
        {
            GameEvents.DebugLog("GameplayHUDController: UIDocument or root element is null!", DebugCategory.UI);
            return;
        }
        
        root = uiDocument.rootVisualElement;
        
        // Get references to UI elements
        healthBarFill = root.Q<VisualElement>("health-bar-fill");
        healthText = root.Q<Label>("health-text");
        
        xpBarFill = root.Q<VisualElement>("xp-bar-fill");
        xpText = root.Q<Label>("xp-text");
        
        levelValue = root.Q<Label>("level-value");
        
        roomValue = root.Q<Label>("room-value");
        enemiesValue = root.Q<Label>("enemies-value");
        
        notificationPanel = root.Q<VisualElement>("notification-panel");
        notificationText = root.Q<Label>("notification-text");
        
        // Validate all elements exist
        if (healthBarFill == null || healthText == null || xpBarFill == null || 
            xpText == null || levelValue == null || roomValue == null || 
            enemiesValue == null || notificationPanel == null || notificationText == null)
        {
            GameEvents.DebugError("GameplayHUDController: Failed to find one or more UI elements!");
            return;
        }
        
        // Initialize with default values
        UpdateHealthBar();
        UpdateXPBar();
        UpdateLevelDisplay();
        UpdateRoomDisplay();
        UpdateEnemiesDisplay();
        
        GameEvents.DebugLog("GameplayHUDController: UI initialized successfully.", DebugCategory.UI);
    }
    
    
    // ========================================
    // EVENT HANDLERS
    // ========================================
    
    private void UpdateHealth(float current, float max)
    {
        currentHealth = current;
        maxHealth = max;
        UpdateHealthBar();
    }
    
    private void UpdateXP(int current, int required)
    {
        currentXP = current;
        requiredXP = required;
        UpdateXPBar();
    }
    
    private void UpdateLevel(int newLevel)
    {
        currentLevel = newLevel;
        UpdateLevelDisplay();
        
        // Show level up notification
        ShowNotification($"CLEARANCE LEVEL {newLevel}");
    }
    
    private void UpdateRoom(int roomIndex)
    {
        currentRoom = roomIndex + 1; // Convert 0-based to 1-based
        UpdateRoomDisplay();
    }
    
    private void OnRoomCleared(int roomIndex)
    {
        ShowNotification("SECTOR CLEARED");
    }
    
    private void OnEnemyKilled(Enemy enemy)
    {
        // Decrement enemy counter
        if (enemiesRemaining > 0)
        {
            enemiesRemaining--;
            UpdateEnemiesDisplay();
        }
    }
    
    
    // ========================================
    // UI UPDATE METHODS
    // ========================================
    
    private void UpdateHealthBar()
    {
        if (healthBarFill == null || healthText == null) return;
        
        // Calculate percentage
        float healthPercent = maxHealth > 0 ? currentHealth / maxHealth : 0f;
        healthPercent = Mathf.Clamp01(healthPercent);
        
        // Update bar width
        healthBarFill.style.width = Length.Percent(healthPercent * 100f);
        
        // Update text
        healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        
        // Apply low health warning style
        if (healthPercent <= lowHealthThreshold)
        {
            healthBarFill.AddToClassList("low-health");
        }
        else
        {
            healthBarFill.RemoveFromClassList("low-health");
        }
    }
    
    private void UpdateXPBar()
    {
        if (xpBarFill == null || xpText == null) return;
        
        // Calculate percentage
        float xpPercent = requiredXP > 0 ? (float)currentXP / requiredXP : 0f;
        xpPercent = Mathf.Clamp01(xpPercent);
        
        // Update bar width
        xpBarFill.style.width = Length.Percent(xpPercent * 100f);
        
        // Update text
        xpText.text = $"{currentXP} / {requiredXP}";
    }
    
    private void UpdateLevelDisplay()
    {
        if (levelValue == null) return;
        levelValue.text = currentLevel.ToString();
    }
    
    private void UpdateRoomDisplay()
    {
        if (roomValue == null) return;
        roomValue.text = $"{currentRoom} / 25";
    }
    
    private void UpdateEnemiesDisplay()
    {
        if (enemiesValue == null) return;
        enemiesValue.text = enemiesRemaining.ToString();
    }
    
    
    // ========================================
    // NOTIFICATION SYSTEM
    // ========================================
    
    /// <summary>
    /// Show a notification in the center of the screen.
    /// </summary>
    public void ShowNotification(string message)
    {
        if (notificationPanel == null || notificationText == null) return;
        
        notificationText.text = message;
        notificationPanel.AddToClassList("visible");
        notificationTimer = notificationDuration;
    }
    
    void Update()
    {
        // Handle notification timer
        if (notificationTimer > 0f)
        {
            notificationTimer -= Time.deltaTime;
            
            if (notificationTimer <= 0f && notificationPanel != null)
            {
                notificationPanel.RemoveFromClassList("visible");
            }
        }
    }
    
    
    // ========================================
    // PUBLIC API (For manual updates)
    // ========================================
    
    /// <summary>
    /// Manually set the enemy count (useful for wave spawners).
    /// </summary>
    public void SetEnemyCount(int count)
    {
        enemiesRemaining = count;
        UpdateEnemiesDisplay();
    }
    
    /// <summary>
    /// Force refresh all UI elements with current cached values.
    /// </summary>
    public void RefreshAll()
    {
        UpdateHealthBar();
        UpdateXPBar();
        UpdateLevelDisplay();
        UpdateRoomDisplay();
        UpdateEnemiesDisplay();
    }
}
