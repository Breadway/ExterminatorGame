# GEMINI.md - Context & Instructions for Exterminator Game

## 1. Project Overview
**Exterminator** is a top-down 3D roguelike developed in **Unity 6 (6000.3.3f1)** using the **Universal Render Pipeline (URP)**. The game features Hades-style combat, procedural room generation, and unique character progression mechanics.

**Core Pillars:**
*   **Systemic Variation:** Rooms force tactical adaptation.
*   **Enemies as Teachers:** Each enemy type teaches a specific mechanic.
*   **Intentionally Broken Characters:** Overpowered mechanics balanced by meaningful constraints.

## 2. Technical Stack
*   **Engine:** Unity 6000.3.3f1 (LTS)
*   **Language:** C# (.NET Standard 2.1)
*   **Pipeline:** URP 17.3.0
*   **Key Packages:**
    *   `com.unity.inputsystem` (New Input System)
    *   `com.unity.cinemachine`
    *   `com.unity.ai.navigation`

## 3. Architecture & Patterns
Strictly adhere to these architectural patterns when modifying code.

### A. Component-Based Modularity
*   **Single Responsibility:** Each script handles ONE logical unit (e.g., `PlayerMovement` handles moving, `PlayerAiming` handles rotation).
*   **Orchestrators:** Use a main controller (e.g., `PlayerController`) to initialize and coordinate components, but delegate actual logic to them.
*   **Dependencies:** Use `[RequireComponent(typeof(T))]` to enforce dependencies.

### B. Event-Driven Communication (`GameEvents.cs`)
*   **Decoupling:** Systems must NOT reference each other directly if possible. Use `GameEvents` static actions.
*   **Pattern:** `Source System` -> `GameEvents.Invoke()` -> `Target System(s)`
*   **Safety:** Always check for null before invoking (`?.Invoke()`).
*   **Lifecycle:** **ALWAYS** unsubscribe from events in `OnDisable()` to prevent memory leaks.

### C. Data-Driven Design (ScriptableObjects)
*   **Configuration:** Use `ScriptableObject` for all game data (Enemies, Weapons, Upgrades).
*   **Avoid Hardcoding:** Do not hardcode values in scripts. Expose them as `[SerializeField]` or load from data assets.
*   **Location:** Store data assets in `Assets/_Project/Data/`.

### D. Managers (Singletons)
*   **Role:** Only use Managers for high-level state orchestration (e.g., `GameManager`, `RoomManager`).
*   **Restriction:** Managers should delegate work, not perform heavy logic.

## 4. Directory Structure
All custom project files reside in `Assets/_Project/`. Do not modify root `Assets/` unless installing third-party tools.

```text
Assets/_Project/
├── Scripts/
│   ├── Managers/          # High-level orchestrators (Singleton-ish)
│   ├── Player/            # Player logic (Controller, Movement, etc.)
│   ├── Enemies/           # Enemy logic
│   ├── Shared/            # Interfaces, Utilities (Health, IDamageable)
│   ├── Events/            # GameEvents.cs
│   ├── Data/              # ScriptableObject definitions (classes)
│   └── UI/                # UI scripts
├── Prefabs/               # GameObjects
├── Data/                  # .asset files (instances of ScriptableObjects)
├── Art/                   # Models, Textures, Materials
└── Scenes/                # Unity Scenes
```

## 5. Coding Standards & Conventions
*   **Naming:**
    *   Classes/Methods/Public Fields: `PascalCase`
    *   Private Fields/Variables: `camelCase`
    *   Constants: `UPPER_CASE`
*   **Serialization:** Prefer `[SerializeField] private` over `public` for Inspector exposure.
*   **Headers:** Use `[Header("Category")]` to organize Inspector fields.
*   **Comments:** Focus on *WHY*, not *WHAT*. Document complex algorithms or event flows.

## 6. Common Workflows

### Adding a New Game Event
1.  Define the `Action` or `Action<T>` in `GameEvents.cs`.
2.  Add a static helper method (optional but recommended) for safe invocation with error handling (try-catch).
3.  Add the `ClearAllListeners` cleanup logic in `GameEvents.ClearAllListeners()`.

### creating a New Enemy
1.  Create a new script in `Scripts/Enemies/Types/` inheriting from `Enemy`.
2.  Create a `ScriptableObject` data class in `Scripts/Data/` if needed (or reuse `EnemyData`).
3.  Create the prefab in `Prefabs/Enemies/`.

## 7. Version Control (Git)
*   **Branching:**
    *   `main`: Stable releases only.
    *   `dev`: Integration branch.
    *   `feature/*`: Active development.
*   **Commit Messages:** Imperative mood, descriptive (e.g., "Add enemy spawn system", not "update").

## 8. Build & Run
*   **Editor:** Open `Scenes/GameplayTest.unity` to test core mechanics.
*   **Build:** Standard Unity Build Settings (Target Platform: Windows/PC).
