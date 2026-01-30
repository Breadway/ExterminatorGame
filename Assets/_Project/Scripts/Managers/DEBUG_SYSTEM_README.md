# Debug System Usage Guide

## Overview
The new debug system uses category-based filtering for granular control over what types of debug messages are displayed.

## Components

### DebugManager
- **Location**: `Assets/_Project/Scripts/Managers/DebugManager.cs`
- **Purpose**: Central configuration for debug logging categories
- **Singleton**: Access via `DebugManager.Instance`
- **Persistent**: Uses `DontDestroyOnLoad()` to persist across scenes

### DebugCategory Enum
Available categories:
- `Combat` - Weapon attacks, damage, hit detection
- `PlayerStats` - Stat changes, upgrades applied to player
- `EnemyAI` - Enemy behavior, pathfinding, state changes
- `Upgrades` - Upgrade selection, application
- `XPAndLeveling` - XP gain, level ups
- `Rooms` - Room clearing, progression
- `Audio` - Sound effects, music
- `UI` - UI interactions, updates
- `Input` - Input events, controls
- `Performance` - Performance metrics, optimization
- `General` - Miscellaneous logs

## Usage

### Basic Logging
```csharp
// Old way (deprecated)
GameEvents.DebugLog("Something happened");

// New way (with category)
GameEvents.DebugLog("Player took damage", DebugCategory.Combat);
GameEvents.DebugLog("Speed increased to 12", DebugCategory.PlayerStats);
GameEvents.DebugLog("Enemy state changed to Chase", DebugCategory.EnemyAI);
```

### Runtime Category Control
```csharp
// Enable/disable specific categories
DebugManager.Instance.SetCategoryEnabled(DebugCategory.Combat, false);
DebugManager.Instance.SetCategoryEnabled(DebugCategory.EnemyAI, true);

// Enable/disable all categories
DebugManager.Instance.EnableAllCategories();
DebugManager.Instance.DisableAllCategories();

// Toggle master debug
DebugManager.Instance.SetMasterDebugEnabled(false);
```

### Checking if Category is Enabled
```csharp
if (DebugManager.Instance.IsCategoryEnabled(DebugCategory.Performance))
{
    // Only calculate expensive debug info if logging is enabled
    string debugInfo = CalculateExpensiveDebugData();
    GameEvents.DebugLog(debugInfo, DebugCategory.Performance);
}
```

## Setup

1. Add `DebugManager` component to a GameObject in your scene (or create a dedicated "Managers" GameObject)
2. Configure which categories are enabled by default in the Inspector
3. The manager will persist across scenes automatically

## Inspector Configuration

In the DebugManager Inspector, you'll see:
- **Master Debug Enabled**: Global on/off switch
- **Debug Categories**: Individual toggles for each category
  - Combat
  - Player Stats
  - Enemy AI
  - Upgrades
  - XP and Leveling
  - Rooms
  - Audio
  - UI
  - Input
  - Performance
  - General

## Log Format

Logs are automatically prefixed with the category:
```
[Combat] [FlamethrowerAttack] Dealing 15.2 damage to 3 targets
[PlayerStats] === UPGRADE APPLIED ===
[EnemyAI] [Enemy] Roach took 12 damage. HP: 38/50
```

## Migration from Old System

The old `PlayerController.debugMode` boolean has been removed. Replace any direct checks with:

```csharp
// Old
if (PlayerController.Instance != null && PlayerController.Instance.debugMode)
{
    Debug.Log("Something");
}

// New
GameEvents.DebugLog("Something", DebugCategory.General);
```

The category filtering is now automatic - no manual checks needed!

## Best Practices

1. **Always specify a category** - Makes logs easier to filter
2. **Use appropriate categories** - Helps with debugging specific systems
3. **Disable expensive categories in production** - Performance and EnemyAI can be verbose
4. **Group related logs** - Use the same category for related debug messages
5. **Use descriptive prefixes** - Include system name in brackets like `[FlamethrowerAttack]`

## Adding New Categories

To add a new debug category:

1. Add to `DebugCategory` enum in `DebugManager.cs`
2. Add a serialized bool field in `DebugManager` (e.g., `logNewCategory`)
3. Update `InitializeCategoryStates()` to include the new category
4. Update `SetCategoryEnabled()` switch statement

Example:
```csharp
// In DebugCategory enum
public enum DebugCategory
{
    // ... existing categories
    Inventory,  // NEW
}

// In DebugManager
[SerializeField] private bool logInventory = true;  // NEW

// In InitializeCategoryStates()
{ DebugCategory.Inventory, logInventory },  // NEW

// In SetCategoryEnabled() switch
case DebugCategory.Inventory: logInventory = enabled; break;  // NEW
```
