using UnityEngine;
using System;
using System.Collections.Generic;

public class UpgradeDebug : MonoBehaviour, IDebugFunction
{
    [SerializeField] private UpgradeData upgradeData;
    private UpgradeManager upgradeManager;
    private List<UpgradeData> currentUpgrades = new List<UpgradeData>();

    void Awake()
    {
        upgradeManager = UpgradeManager.Instance;
    }
    private void Update()
    {
        
    }
    public void ExecuteDebug()
    {
        // Ensure we have a valid UpgradeManager instance
        if (upgradeManager == null)
        {
            upgradeManager = UpgradeManager.Instance;
            if (upgradeManager == null)
            {
                GameEvents.DebugLog("UpgradeManager instance is null in UpgradeDebug.ExecuteDebug()", DebugCategory.Upgrades);
                return;
            }
        }

        // Clear any previous results and request upgrades
        currentUpgrades.Clear();
        upgradeManager.GetUpgradesByTypeAndCount(UpgradeType.Utility, 3, currentUpgrades);

        if (currentUpgrades == null || currentUpgrades.Count == 0)
        {
            GameEvents.DebugLog("No upgrades returned by GetUpgradesByTypeAndCount()", DebugCategory.Upgrades);
            return;
        }

        foreach (var upgrade in currentUpgrades)
        {
            if (upgrade == null) continue;
            GameEvents.DebugLog($"Applying Upgrade: {upgrade.upgradeName}, {upgrade.description}", DebugCategory.Upgrades);
            upgradeManager.ApplyUpgrade(upgrade);
        }
    }
}