# AGENTS.md - Coding Agent Guidelines for Exterminator Game

This document provides essential information for AI coding agents working in this Unity project.

---

## 1. Quick Reference: Build/Lint/Test Commands

### Running the Game
```
Unity Editor → Open Assets/_Project/Scenes/TestScene.unity → Press Play button
Controls: WASD (move), Mouse (aim), Left Click (shoot)
```

### Building the Project
```
Unity Editor → File → Build Settings → Build
Target Platform: Windows Standalone (64-bit)
```

### Running Tests
```
Unity Editor → Window → General → Test Runner
Status: No test assemblies exist yet (Phase 2 priority)
```

### Linting/Code Analysis
```
Status: No linting configuration (.editorconfig, .ruleset) exists yet
IDE: Use Visual Studio, Rider, or VS Code with C# extensions
```

---

## 2. Architecture Principles (STRICT - Must Follow)

This codebase follows three core architectural patterns. **Deviation from these patterns is not permitted** without explicit justification.

### A. Component-Based Modularity

**Rule**: Each MonoBehaviour script has ONE responsibility.

**Pattern**:
- Create an orchestrator (e.g., `PlayerController`, `Enemy`) to coordinate components
- Delegate actual logic to specialized components (e.g., `PlayerMovement`, `PlayerAiming`, `EnemyAttack`)
- Use `[RequireComponent(typeof(T))]` to enforce dependencies

**Example**:
```csharp
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAiming))]
public class PlayerController : MonoBehaviour {
    private Health health;
    private PlayerMovement movement;
    private PlayerAiming aiming;
    
    void Awake() {
        health = GetComponent<Health>();
        movement = GetComponent<PlayerMovement>();
        aiming = GetComponent<PlayerAiming>();
    }
}
```

**Anti-Pattern**: Don't create monolithic scripts that handle multiple concerns (movement + shooting + UI + inventory).

### B. Event-Driven Communication (GameEvents.cs)

**Rule**: Systems must NOT reference each other directly if cross-system communication is needed. Use `GameEvents.cs` instead.

**Pattern**:
```
Source System → GameEvents.OnEventName?.Invoke(data) → Target System(s)
```

**Event Subscription Lifecycle** (CRITICAL):
```csharp
void OnEnable() {
    GameEvents.OnEnemyKilled += HandleEnemyKilled;
}

void OnDisable() {
    // ALWAYS unsubscribe to prevent memory leaks
    GameEvents.OnEnemyKilled -= HandleEnemyKilled;
}
```

**Safe Invocation**:
- Use `?.Invoke()` for null safety: `GameEvents.OnPlayerDamaged?.Invoke(damage, currentHP, maxHP);`
- For complex invocations, use try-catch wrappers (see GameEvents.cs:300+)

**When to Use Events**:
- XP/Level systems reacting to enemy kills
- UI updating based on health changes
- Audio playing on damage/death
- Room manager checking clear status

**When NOT to Use Events**:
- Component communication within the same GameObject (use direct references)
- Single-responsibility components (e.g., PlayerMovement doesn't need events internally)

### C. Data-Driven Design (ScriptableObjects)

**Rule**: All game configuration data MUST live in ScriptableObject assets, not hardcoded in scripts.

**Data Types**:
- Enemy stats → `EnemyData.cs` (ScriptableObject definition)
- Weapon stats → `WeaponData.cs`
- Upgrade definitions → `UpgradeData.cs`
- Player stats → `PlayerStats.cs`

**Storage Location**:
- Class definitions: `Assets/_Project/Scripts/Data/`
- Asset instances (.asset files): `Assets/_Project/Data/Enemies/`, `/Upgrades/`, `/Weapons/`

**Example**:
```csharp
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Exterminator/Enemy Data")]
public class EnemyData : ScriptableObject {
    public string enemyName;
    public float maxHealth;
    public float moveSpeed;
    public float damage;
}
```

**Anti-Pattern**: Don't hardcode values like `public float moveSpeed = 5f;` when they should be configurable.

### D. Sparse Manager Pattern

**Rule**: Only use Managers for high-level state orchestration. Current managers:
- `GameManager` - Game state, scene transitions
- `RoomManager` - Room clearing, progression
- `XPManager` - Experience and leveling
- `UpgradeManager` - Upgrade selection and application
- `AudioManager` - Sound/music coordination

**What Managers Do**: Coordinate systems, listen to events, trigger high-level state changes
**What Managers Don't Do**: Execute heavy logic, handle frame-by-frame updates

---

## 3. Code Style Guidelines

### Imports
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
// No explicit namespaces used in this project (all global namespace)
```

### Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Classes/Methods/Properties | PascalCase | `PlayerController`, `TakeDamage()`, `MaxHP` |
| Public Fields | PascalCase | `public float MaxHP;` |
| Private Fields | camelCase | `private float currentHP;` |
| Serialized Private Fields | camelCase | `[SerializeField] private float maxHP;` |
| Constants | UPPER_CASE | `private const float MAX_SPEED = 10f;` |
| Events | PascalCase with "On" prefix | `public static Action OnPlayerDied;` |

### Type Usage

**Serialization** (PREFER this):
```csharp
[SerializeField] private float maxHP = 100f;  // Visible in Inspector, encapsulated
```

**NOT this**:
```csharp
public float maxHP = 100f;  // Exposes field unnecessarily
```

**Properties** (when external read access needed):
```csharp
public float CurrentHP => currentHP;  // Read-only property
public float MaxHP { get; private set; }  // Read-only externally
```

**Events** (use Actions):
```csharp
public event Action<float, Vector3> OnDamaged;  // Standard event pattern
public static Action<Enemy> OnEnemyKilled;       // Static for global events
```

### Formatting

**Inspector Organization**:
```csharp
[Header("Health Stats")]
[SerializeField] private float maxHP = 100f;
[SerializeField] private float regenRate = 0f;

[Header("Settings")]
[SerializeField] private bool isInvulnerable = false;
```

**Attribute Order**:
```csharp
[Header("Category")]
[SerializeField]
[Tooltip("Description")]
private float value;
```

### Error Handling

**Event Invocation** (safe pattern):
```csharp
GameEvents.OnPlayerDamaged?.Invoke(damage, currentHP, maxHP);
```

**Complex Event Invocation** (with try-catch):
```csharp
public static void SafeInvokeEnemyKilled(Enemy enemy) {
    try {
        OnEnemyKilled?.Invoke(enemy);
    } catch (Exception e) {
        Debug.LogError($"Error invoking OnEnemyKilled: {e.Message}");
    }
}
```

**Null Checks**:
```csharp
if (!IsAlive || isInvulnerable) return;  // Early return pattern
```

### Comments

**Focus on WHY, not WHAT**:

Good:
```csharp
// Health doesn't know HOW to die (player vs enemy vs destructible)
// It just announces "I died" via event
void Die() {
    OnDied?.Invoke();
}
```

Bad:
```csharp
// This function makes the object die
void Die() {
    OnDied?.Invoke();  // Invoke the died event
}
```

**Document Event Flows**:
```csharp
/// <summary>
/// Fired when player takes damage.
/// Parameters: damage amount, current HP, max HP
/// Listeners: HealthBarUI (update bar), ScreenEffects (flash red), AudioManager (hurt sound)
/// </summary>
public static Action<float, float, float> OnPlayerDamaged;
```

---

## 4. File Organization

```
Assets/_Project/
├── Scripts/
│   ├── Managers/          # GameManager, RoomManager, XPManager, UpgradeManager, AudioManager
│   ├── Player/            # PlayerController, PlayerMovement, PlayerAiming, PlayerAttack
│   ├── Enemies/           # Enemy.cs, EnemyAttack.cs, Movement/, Attack/, Types/
│   ├── Shared/            # Health.cs, IDamageable.cs, IWeaponAttack.cs
│   ├── Systems/           # CombatCalculator.cs, PoolingSystem.cs, WaveSpawner.cs
│   ├── Data/              # EnemyData.cs, WeaponData.cs, UpgradeData.cs (definitions)
│   ├── Events/            # GameEvents.cs (centralized event hub)
│   ├── UI/                # HealthBarUI.cs, XPBarUI.cs, UpgradeUI.cs
│   └── Utilities/         # PerformanceUtils.cs, helper functions
├── Prefabs/               # Player/, Enemies/, Projectiles/, UI/
├── Data/                  # .asset files (Enemies/, Upgrades/, Weapons/)
├── Scenes/                # TestScene.unity (primary gameplay scene)
├── Art/                   # 3D Models, Materials, Particles
└── Audio/                 # Sound effects, music
```

**Where to Place New Files**:
- New enemy type → `Scripts/Enemies/Types/EnemyName.cs`
- New player ability → `Scripts/Player/PlayerAbilityName.cs`
- New UI element → `Scripts/UI/ElementNameUI.cs`
- New system → `Scripts/Systems/SystemName.cs`
- New interface → `Scripts/Shared/IInterfaceName.cs`

---

## 5. Git Workflow

### Branch Strategy
```
main          # Stable builds (protected, no direct commits)
  ↑
dev           # Integration branch
  ↑
feature/*     # Work-in-progress branches
```

### Commit Messages

**Good**:
- "Add enemy spawn system"
- "Fix diagonal movement speed bug"
- "Implement XP gain on enemy kill"

**Bad**:
- "update"
- "fix stuff"
- "changes"

**Format**: Imperative mood, describe WHAT changed, not HOW

---

## 6. Pre-Commit Checklist

Before committing code, verify:

- [ ] Code compiles without errors or warnings
- [ ] No debug logs (or marked `// TODO: remove`)
- [ ] Each component has single responsibility
- [ ] Event subscriptions have matching unsubscriptions in OnDisable()
- [ ] Game data uses ScriptableObjects (no hardcoded values)
- [ ] RequireComponent attributes added for dependencies
- [ ] Tested in Play mode (doesn't crash, behaves as expected)

---

## 7. Common Patterns & Anti-Patterns

### Creating a New Enemy

**Correct**:
1. Create `Scripts/Enemies/Types/EnemyName.cs` inheriting from `Enemy`
2. Create `Scripts/Data/EnemyNameData.cs` if unique stats needed
3. Create prefab in `Prefabs/Enemies/EnemyName.prefab`
4. Create ScriptableObject asset in `Data/Enemies/EnemyName.asset`

### Adding a New Game Event

**Correct**:
1. Add event to `GameEvents.cs` with XML summary
2. Document listeners in summary
3. Add to `ClearAllListeners()` method at bottom of GameEvents.cs
4. Subscribe in OnEnable(), unsubscribe in OnDisable()

### Accessing Player from Any Script

**Correct**:
```csharp
PlayerController player = PlayerController.Instance;
```

**Incorrect**:
```csharp
GameObject.FindObjectOfType<PlayerController>();  // Too slow, don't use in Update()
```

---

## 8. Project Context

**Project Type**: Unity 6.3 LTS roguelike (Hades-style top-down 3D)
**Target Platform**: Windows Standalone (64-bit)
**Rendering**: Universal Render Pipeline (URP) 17.3.0
**Language**: C# (.NET Standard 2.1, LangVersion 9.0)
**Input**: New Input System (com.unity.inputsystem 1.17.0)

**Current Phase**: Phase 2 (Core Loop) - Room clearing, upgrades, path progression
**Team**: 2 developers, 20-month timeline, ~6.5 hours/week

**Key Documentation**:
- `README.md` - Comprehensive project overview (536 lines)
- `GEMINI.md` - Architecture and workflow guidance
- `GAMEDESIGN.md` - Character design philosophy
- `LORE.md` - Game narrative and world

---

## 9. Testing (Future)

**Status**: No test assemblies exist yet (Phase 2 priority)

**When Tests Are Added**:
- Create `Assets/_Project/Scripts/Tests/` folder
- Create `.asmdef` files for test assemblies
- Use Unity Test Framework (already installed)
- Run via: Unity Editor → Window → General → Test Runner

---

**Last Updated**: January 2026
**Current Unity Version**: 6000.3.3f1
**Primary Scene**: Assets/_Project/Scenes/TestScene.unity
