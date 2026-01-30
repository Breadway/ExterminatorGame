using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class DebugMenuUI : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;
    private PlayerControls inputActions;

    private VisualElement root;
    private VisualElement debugRoot;
    private VisualElement dragHandle;
    
    // Tabs
    private Button tabStats;
    private Button tabUpgrades;
    private Button tabWave;
    private Button tabActions;
    
    // Content Areas
    private VisualElement contentStats;
    private VisualElement contentUpgrades;
    private VisualElement contentWave;
    private VisualElement contentActions;
    
    // Stats Elements
    private VisualElement statsList;
    private Button btnApplyStats;
    
    // Upgrade Elements
    private VisualElement upgradesList;
    
    // Wave Elements
    private TextField valWaveIndex;
    private TextField valEnemiesAlive;
    private Label valWaveState;
    private Button btnApplyWave;
    
    // Actions
    private Button btnLevelUp;
    private Button btnKillAll;
    private Button btnHeal;
    private Button btnAddXP;
    private Button btnAddRandomUpgrade;

    private bool isVisible = false;
    private float updateTimer = 0f;
    private const float UPDATE_INTERVAL = 0.5f;
    
    // Dragging state
    private bool isDragging = false;
    private Vector2 dragStartMousePos;
    private Vector2 dragStartPanelPos;
    
    // Stat input field references for applying changes
    private Dictionary<string, TextField> statInputFields = new Dictionary<string, TextField>();

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            GameEvents.DebugError("DebugMenuUI: No UIDocument found.");
            return;
        }

        root = uiDocument.rootVisualElement;
        debugRoot = root.Q<VisualElement>("DebugRoot");
        dragHandle = root.Q<VisualElement>("DragHandle");
        
        // Find Tabs
        tabStats = root.Q<Button>("TabStats");
        tabUpgrades = root.Q<Button>("TabUpgrades");
        tabWave = root.Q<Button>("TabWave");
        tabActions = root.Q<Button>("TabActions");
        
        // Find Contents
        contentStats = root.Q<VisualElement>("ContentStats");
        contentUpgrades = root.Q<VisualElement>("ContentUpgrades");
        contentWave = root.Q<VisualElement>("ContentWave");
        contentActions = root.Q<VisualElement>("ContentActions");
        
        // Find Data Containers
        statsList = root.Q<VisualElement>("StatsList");
        upgradesList = root.Q<VisualElement>("UpgradesList");
        btnApplyStats = root.Q<Button>("BtnApplyStats");
        
        valWaveIndex = root.Q<TextField>("ValWaveIndex");
        valEnemiesAlive = root.Q<TextField>("ValEnemiesAlive");
        valWaveState = root.Q<Label>("ValWaveState");
        btnApplyWave = root.Q<Button>("BtnApplyWave");
        
        // Find Actions
        btnLevelUp = root.Q<Button>("BtnLevelUp");
        btnKillAll = root.Q<Button>("BtnKillAll");
        btnHeal = root.Q<Button>("BtnHeal");
        btnAddXP = root.Q<Button>("BtnAddXP");
        btnAddRandomUpgrade = root.Q<Button>("BtnAddRandomUpgrade");
        
        // Bind Tab Events
        tabStats.clicked += () => SwitchTab(0);
        tabUpgrades.clicked += () => SwitchTab(1);
        tabWave.clicked += () => SwitchTab(2);
        tabActions.clicked += () => SwitchTab(3);
        
        // Bind Action Events
        btnLevelUp.clicked += OnLevelUpClicked;
        btnKillAll.clicked += OnKillAllClicked;
        btnHeal.clicked += OnHealClicked;
        btnAddXP.clicked += OnAddXPClicked;
        btnAddRandomUpgrade.clicked += OnAddRandomUpgradeClicked;
        
        // Bind Apply buttons
        btnApplyStats.clicked += OnApplyStatsClicked;
        btnApplyWave.clicked += OnApplyWaveClicked;
        
        // Setup Dragging
        SetupDragging();
        
        // Input
        inputActions = new PlayerControls();
        inputActions.Player.DebugMenu.Enable();
        inputActions.Player.DebugMenu.performed += ctx => ToggleVisibility();
        
        // Init State
        isVisible = false;
        debugRoot?.AddToClassList("hidden");
    }
    
    private void SetupDragging()
    {
        if (dragHandle == null) return;
        
        dragHandle.RegisterCallback<PointerDownEvent>(OnDragStart);
        dragHandle.RegisterCallback<PointerMoveEvent>(OnDragMove);
        dragHandle.RegisterCallback<PointerUpEvent>(OnDragEnd);
        dragHandle.RegisterCallback<PointerCaptureOutEvent>(OnDragCaptureOut);
    }
    
    private void OnDragStart(PointerDownEvent evt)
    {
        isDragging = true;
        dragStartMousePos = evt.position;
        dragStartPanelPos = new Vector2(debugRoot.style.left.value.value, debugRoot.style.top.value.value);
        dragHandle.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }
    
    private void OnDragMove(PointerMoveEvent evt)
    {
        if (!isDragging) return;
        
        Vector2 delta = (Vector2)evt.position - dragStartMousePos;
        debugRoot.style.left = dragStartPanelPos.x + delta.x;
        debugRoot.style.top = dragStartPanelPos.y + delta.y;
        evt.StopPropagation();
    }
    
    private void OnDragEnd(PointerUpEvent evt)
    {
        if (!isDragging) return;
        
        isDragging = false;
        dragHandle.ReleasePointer(evt.pointerId);
        evt.StopPropagation();
    }
    
    private void OnDragCaptureOut(PointerCaptureOutEvent evt)
    {
        isDragging = false;
    }

    private void OnDisable()
    {
        // Cleanup dragging
        if (dragHandle != null)
        {
            dragHandle.UnregisterCallback<PointerDownEvent>(OnDragStart);
            dragHandle.UnregisterCallback<PointerMoveEvent>(OnDragMove);
            dragHandle.UnregisterCallback<PointerUpEvent>(OnDragEnd);
            dragHandle.UnregisterCallback<PointerCaptureOutEvent>(OnDragCaptureOut);
        }
        
        if (inputActions != null)
        {
            inputActions.Player.DebugMenu.performed -= ctx => ToggleVisibility();
            inputActions.Player.DebugMenu.Disable();
            inputActions.Dispose();
        }

        // Unsubscribe UI button events
        if (tabStats != null) tabStats.clicked -= () => SwitchTab(0);
        if (tabUpgrades != null) tabUpgrades.clicked -= () => SwitchTab(1);
        if (tabWave != null) tabWave.clicked -= () => SwitchTab(2);
        if (tabActions != null) tabActions.clicked -= () => SwitchTab(3);

        if (btnLevelUp != null) btnLevelUp.clicked -= OnLevelUpClicked;
        if (btnKillAll != null) btnKillAll.clicked -= OnKillAllClicked;
        if (btnHeal != null) btnHeal.clicked -= OnHealClicked;
        if (btnAddXP != null) btnAddXP.clicked -= OnAddXPClicked;
        if (btnAddRandomUpgrade != null) btnAddRandomUpgrade.clicked -= OnAddRandomUpgradeClicked;
        if (btnApplyStats != null) btnApplyStats.clicked -= OnApplyStatsClicked;
        if (btnApplyWave != null) btnApplyWave.clicked -= OnApplyWaveClicked;
    }

    private void Update()
    {

        if (isVisible)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= UPDATE_INTERVAL)
            {
                updateTimer = 0f;
                RefreshActiveTab();
            }
        }
    }

    private void ToggleVisibility()
    {
        isVisible = !isVisible;
        if (isVisible)
        {
            debugRoot.RemoveFromClassList("hidden");
            RefreshActiveTab(); // Immediate refresh on open
        }
        else
        {
            debugRoot.AddToClassList("hidden");
        }
    }

    private void SwitchTab(int index)
    {
        // Reset Tabs
        tabStats.RemoveFromClassList("active");
        tabUpgrades.RemoveFromClassList("active");
        tabWave.RemoveFromClassList("active");
        tabActions.RemoveFromClassList("active");
        
        // Reset Content
        contentStats.RemoveFromClassList("active");
        contentUpgrades.RemoveFromClassList("active");
        contentWave.RemoveFromClassList("active");
        contentActions.RemoveFromClassList("active");
        
        // Set Active
        switch (index)
        {
            case 0:
                tabStats.AddToClassList("active");
                contentStats.AddToClassList("active");
                RefreshStats();
                break;
            case 1:
                tabUpgrades.AddToClassList("active");
                contentUpgrades.AddToClassList("active");
                RefreshUpgrades();
                break;
            case 2:
                tabWave.AddToClassList("active");
                contentWave.AddToClassList("active");
                RefreshWave();
                break;
            case 3:
                tabActions.AddToClassList("active");
                contentActions.AddToClassList("active");
                break;
        }
    }

    private void RefreshActiveTab()
    {
        // Only auto-refresh read-only tabs (Upgrades)
        // Stats and Wave tabs have editable fields - don't overwrite user input
        if (contentUpgrades.ClassListContains("active")) RefreshUpgrades();
    }

    // ========================================
    // REFRESH LOGIC
    // ========================================
    
    private void RefreshStats()
    {
        statsList.Clear();
        statInputFields.Clear();
        
        var player = PlayerController.Instance;
        if (player == null || player.stats == null)
        {
            AddReadOnlyRow(statsList, "Player", "Only available in Play Mode with Player");
            return;
        }
        
        var s = player.stats;
        AddEditableRow(statsList, "MaxHP", "Max HP", s.currentMaxHP.ToString("F0"));
        AddEditableRow(statsList, "Damage", "Damage", s.currentDamage.ToString("F1"));
        AddEditableRow(statsList, "Speed", "Speed", s.currentSpeed.ToString("F1"));
        AddEditableRow(statsList, "AtkSpeed", "Atk Speed", s.currentAttackSpeed.ToString("F2"));
        AddEditableRow(statsList, "CritChance", "Crit Chance %", (s.currentCritChance * 100f).ToString("F1"));
        AddEditableRow(statsList, "CritMult", "Crit Mult", s.currentCritMultiplier.ToString("F1"));
        AddEditableRow(statsList, "Armor", "Armor", s.currentArmor.ToString("F0"));
        AddEditableRow(statsList, "DashCD", "Dash CD", s.currentDashCooldown.ToString("F2"));
        
        // Position is read-only (can't edit transform easily via text)
        AddReadOnlyRow(statsList, "Position", player.transform.position.ToString());
    }

    private void RefreshUpgrades()
    {
        upgradesList.Clear();
        
        var player = PlayerController.Instance;
        var upgradeManager = UpgradeManager.Instance;
        
        if (player == null || upgradeManager == null) return;
        
        var allUpgrades = upgradeManager.GetAllUpgrades();
        bool anyFound = false;
        
        foreach (var u in allUpgrades)
        {
            if (player.stats.HasUpgrade(u))
            {
                AddReadOnlyRow(upgradesList, u.name, u.rarity.ToString());
                anyFound = true;
            }
        }
        
        if (!anyFound)
        {
            AddReadOnlyRow(upgradesList, "Status", "No upgrades applied.");
        }
    }

    private void RefreshWave()
    {
        var spawner = FindFirstObjectByType<WaveSpawner>();
        if (spawner == null)
        {
            valWaveIndex.value = "N/A";
            valEnemiesAlive.value = "N/A";
            valWaveState.text = "Spawner Not Found";
            return;
        }
        
        valWaveIndex.value = spawner.CurrentWave.ToString();
        valEnemiesAlive.value = spawner.EnemiesAlive.ToString();
        valWaveState.text = spawner.WaveInProgress ? "COMBAT" : "PREPARING";
        valWaveState.style.color = spawner.WaveInProgress ? new StyleColor(new Color(1f, 0.4f, 0f)) : new StyleColor(new Color(0.2f, 0.8f, 0.2f));
    }
    
    private void AddEditableRow(VisualElement container, string key, string label, string value)
    {
        var row = new VisualElement();
        row.AddToClassList("data-row");
        
        var lbl = new Label(label);
        lbl.AddToClassList("data-label");
        
        var input = new TextField();
        input.value = value;
        input.AddToClassList("data-input");
        
        row.Add(lbl);
        row.Add(input);
        container.Add(row);
        
        statInputFields[key] = input;
    }
    
    private void AddReadOnlyRow(VisualElement container, string label, string value)
    {
        var row = new VisualElement();
        row.AddToClassList("data-row");
        
        var lbl = new Label(label);
        lbl.AddToClassList("data-label");
        
        var val = new Label(value);
        val.AddToClassList("data-value");
        
        row.Add(lbl);
        row.Add(val);
        container.Add(row);
    }
    
    // ========================================
    // ACTIONS
    // ========================================
    
    private void OnLevelUpClicked()
    {
        GameEvents.DebugLog("Debug: Force Level Up", DebugCategory.XPAndLeveling);
        
        var xpManager = FindFirstObjectByType<XPManager>(); // Assuming it exists
        if (xpManager != null)
        {
            GameEvents.DebugWarning("Auto-level requires XPManager logic knowledge. Attempting event bypass.", DebugCategory.XPAndLeveling);
            
            GameEvents.OnLevelUp?.Invoke(999); // Sending arbitrary level for testing
        }
    }
    
    private void OnKillAllClicked()
    {
        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            
            // Try to find IDamageable
            var damageable = e.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(99999f, Vector2.zero, Vector2.zero);
            }
            else
            {
                // Fallback
                GameEvents.OnEnemyKilled?.Invoke(e);
                Destroy(e.gameObject);
            }
        }
    }
    
    private void OnHealClicked()
    {
         var player = PlayerController.Instance;
         if (player != null)
         {
             var health = player.GetComponent<Health>();
             if (health != null)
             {
                 health.Heal(999f);
             }
         }
    }
    
    private void OnAddXPClicked()
    {
        var xpManager = XPManager.Instance;
        if (xpManager != null)
        {
            xpManager.AddXP(100);
            GameEvents.DebugLog("Debug: Added 100 XP", DebugCategory.XPAndLeveling);
        }
    }
    
    private void OnAddRandomUpgradeClicked()
    {
        var player = PlayerController.Instance;
        var manager = UpgradeManager.Instance;
        if (player != null && manager != null)
        {
            var upgrades = manager.GetAllUpgrades();
            if (upgrades.Length > 0)
            {
                var randomUpgrade = upgrades[UnityEngine.Random.Range(0, upgrades.Length)];
                // UpgradeManager usually applies via event? 
                // "OnPlayerUpgradeApplied -> Listeners: Player (apply upgrade)"
                GameEvents.OnPlayerUpgradeApplied?.Invoke(randomUpgrade);
            }
        }
    }
    
    // ========================================
    // APPLY EDITS
    // ========================================
    
    private void OnApplyStatsClicked()
    {
        var player = PlayerController.Instance;
        if (player == null || player.stats == null)
        {
            GameEvents.DebugWarning("Debug: Cannot apply stats - no player found.", DebugCategory.Player);
            return;
        }
        
        var s = player.stats;
        
        // Parse and apply each stat
        if (statInputFields.TryGetValue("MaxHP", out var maxHpField) && float.TryParse(maxHpField.value, out float maxHP))
            s.DebugSetMaxHP(maxHP);
            
        if (statInputFields.TryGetValue("Damage", out var damageField) && float.TryParse(damageField.value, out float damage))
            s.DebugSetDamage(damage);
            
        if (statInputFields.TryGetValue("Speed", out var speedField) && float.TryParse(speedField.value, out float speed))
            s.DebugSetSpeed(speed);
            
        if (statInputFields.TryGetValue("AtkSpeed", out var atkSpeedField) && float.TryParse(atkSpeedField.value, out float atkSpeed))
            s.DebugSetAttackSpeed(atkSpeed);
            
        if (statInputFields.TryGetValue("CritChance", out var critChanceField) && float.TryParse(critChanceField.value, out float critChance))
            s.DebugSetCritChance(critChance / 100f); // Convert from percentage
            
        if (statInputFields.TryGetValue("CritMult", out var critMultField) && float.TryParse(critMultField.value, out float critMult))
            s.DebugSetCritMultiplier(critMult);
            
        if (statInputFields.TryGetValue("Armor", out var armorField) && float.TryParse(armorField.value, out float armor))
            s.DebugSetArmor(armor);
            
        if (statInputFields.TryGetValue("DashCD", out var dashCDField) && float.TryParse(dashCDField.value, out float dashCD))
            s.DebugSetDashCooldown(dashCD);
        
        // Also update health component if MaxHP changed
        var health = player.GetComponent<Health>();
        if (health != null)
        {
            health.SetMaxHP(s.currentMaxHP);
        }
        
        GameEvents.DebugLog("Debug: Stats applied!", DebugCategory.PlayerStats);
    }
    
    private void OnApplyWaveClicked()
    {
        var spawner = FindFirstObjectByType<WaveSpawner>();
        if (spawner == null)
        {
            GameEvents.DebugWarning("Debug: Cannot apply wave - no spawner found.", DebugCategory.EnemyAI);
            return;
        }
        
        if (int.TryParse(valWaveIndex.value, out int waveIndex))
        {
            spawner.DebugSetWave(waveIndex);
        }
        
        GameEvents.DebugLog("Debug: Wave settings applied!", DebugCategory.EnemyAI);
    }
}
