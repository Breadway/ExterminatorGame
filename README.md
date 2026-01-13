# 🪲 Exterminator (Working Title)

> A top-down roguelike where you play as pest exterminators clearing bug infestations. Think Hades meets Dead Cells with a darkly comedic twist.

[![Unity Version](https://img.shields.io/badge/Unity-6.3_LTS-black.svg?style=flat&logo=unity)](https://unity.com/)
[![License](https://img.shields.io/badge/License-Proprietary-red.svg)](LICENSE)
[![Development Phase](https://img.shields.io/badge/Phase-Foundation_(Month_1--2)-blue.svg)](#development-roadmap)

---

## 📋 Table of Contents
- [Project Overview](#-project-overview)
- [Current Status](#-current-status)
- [Core Features](#-core-features)
- [Technical Stack](#-technical-stack)
- [Project Structure](#-project-structure)
- [Development Roadmap](#-development-roadmap)
- [Architecture](#-architecture)
- [Team & Timeline](#-team--timeline)
- [Getting Started](#-getting-started)
- [Workflow](#-workflow)
- [Contributing](#-contributing)
- [Design Philosophy](#-design-philosophy)

---

## 🎮 Project Overview

**Exterminator** is a top-down roguelike with Hades-style 3D visuals, procedurally generated runs, and unique character progression. Players choose from 4-5 unlockable exterminators, each with "intentionally broken" mechanics balanced by constraints.

### Core Loop
1. Clear 25 rooms with branching Slay the Spire-style paths
2. Choose 1 of 3 upgrades after each room (Power/Defense/Utility)
3. Level up through combat, unlock path abilities at levels 10/20/30
4. Defeat mid-boss (room ~12) and final boss (room 25)
5. Unlock Endless Mode for infinite scaling difficulty

### Game Pillars
- **Systemic Variation Over Raw Difficulty**: Each room forces tactical adaptation
- **Enemies as Teachers**: Every bug type teaches a specific mechanic
- **Intentionally Broken Characters**: Overpowered mechanics with meaningful constraints

---

## 📊 Current Status

### ✅ Completed (Month 1 - Foundation Phase)
- [x] Unity 6.3 LTS project setup with URP
- [x] Git repository initialized with proper .gitignore
- [x] GitHub remote configured, collaborator added
- [x] Project file structure created
- [x] Modular player architecture implemented (Manager + Movement + Aiming components)
- [x] Rigidbody-based movement system functional
- [x] Basic cursor-based aiming framework

### 🚧 In Progress
- [ ] Movement feel refinement (responsiveness, drag tuning)
- [ ] Ground raycast aiming with edge case handling
- [ ] Input normalization (diagonal movement fix)

### 📅 Next Up (Current Sprint)
- [ ] Finalize movement system (max 1-2 sessions)
- [ ] Basic enemy AI (chase player, simple pathfinding)
- [ ] Universal Health system implementation
- [ ] Player attack functionality (raycast damage)
- [ ] Enemy attack (collision/proximity-based)
- [ ] XP gain on enemy kill

### 🎯 Month 1-2 Deliverable Target
**"Kill enemies and level up"** - Functional combat loop where player can kill enemies, gain XP, and see level increase.

---

## 🎮 Core Features

### Characters (4-5 Unlockable Exterminators)
Each character has one overpowered mechanic balanced by a constraint:

| Character | Power | Constraint |
|-----------|-------|-----------|
| **Flamethrower** | Fire spreads infinitely between enemies | Takes damage in own flames |
| **Beekeeper** | Dead bugs become allies | Cannot deal direct damage |
| **Cryo-Tech** | Frozen enemies shatter and chain-freeze | Cannot damage unfrozen enemies |
| **Swatter** | Melee specialist with execute mechanic | Close-range only |
| **Sprayer** | Chemical DOT with poison immunity | Damage over time (no burst) |

### Progression Systems

#### 1. Room Upgrades (Per-Run)
- Choose 1 of 3 after each room clear
- Categories: Power (damage/weapons), Defense (HP/resistances), Utility (speed/cooldowns)
- ~25 upgrade choices per full run

#### 2. XP & Path System (Per-Run)
- **Every Level**: Auto-heal X% HP (percentage TBD)
- **Level 10**: Choose 1 of 3 major path branches (significant power spike)
- **Level 20**: Choose 1 of 2 options based on Level 10 choice
- **Level 30**: Final path choice (Endless Mode only, massive power spike)
- **Level 31+**: Permanent character-specific buffs each level (Endless only)

#### 3. Meta-Progression (Persistent)
- Earn currency from runs (sources TBD)
- Unlock new characters (after milestone achievements)
- Unlock additional character path trees
- Small meaningful upgrades (scope TBD)

### Map Structure
- **25 rooms total** with Slay the Spire-style branching paths
- **Room Types**: Combat, Shop, Elite, "Unknown" (random modifiers/events)
- **Bosses**: Mid-boss (~room 12), Final boss (room 25)
- **Endless Mode**: Unlocked after beating final boss, separate mode per map

### Enemy Design (8-15 Types)
"Enemies as teachers" - each type exists to teach a specific mechanic:

- **Roaches**: Swarm tactics (teaches AOE value)
- **Spiders**: Web traps, ceiling walkers (teaches awareness)
- **Wasps**: Flying, ranged (teaches target priority)
- **Beetles**: Armored (teaches armor-piercing value)
- **Larvae**: Evolve if not killed quickly (teaches tempo)

---

## 🛠 Technical Stack

- **Engine**: Unity 6.3 LTS
- **Rendering**: Universal Render Pipeline (URP)
- **Perspective**: 3D models, fixed camera angle (~45° Hades-style)
- **Version Control**: Git + GitHub
- **Project Management**: JIRA
- **Languages**: C# (.NET Standard 2.1)

### Key Systems
- Component-based architecture (modular MonoBehaviours)
- Event-driven design (centralized `GameEvents.cs`)
- ScriptableObject-based data (upgrades, enemies, weapons)
- Manager pattern for singleton systems (sparingly used)

---

## 📁 Project Structure

```
Assets/
└── _Project/
    ├── Scenes/
    │   ├── MainMenu.unity
    │   ├── GameplayTest.unity
    │   └── Endless.unity
    │
    ├── Scripts/
    │   ├── Managers/          # Singleton-style orchestrators
    │   │   ├── GameManager.cs
    │   │   ├── RoomManager.cs
    │   │   ├── XPManager.cs
    │   │   ├── UpgradeManager.cs
    │   │   └── AudioManager.cs
    │   │
    │   ├── Player/            # Player-specific components
    │   │   ├── PlayerController.cs      # Orchestrator
    │   │   ├── PlayerMovement.cs        # Movement logic
    │   │   ├── PlayerAiming.cs          # Aiming logic
    │   │   ├── PlayerShooting.cs        # Attack logic
    │   │   └── PlayerStats.cs           # Data class
    │   │
    │   ├── Enemies/           # Enemy components
    │   │   ├── Enemy.cs                 # Base enemy orchestrator
    │   │   ├── EnemyMovement.cs
    │   │   ├── EnemyAttack.cs
    │   │   └── Types/
    │   │       ├── Roach.cs
    │   │       ├── Spider.cs
    │   │       └── Wasp.cs
    │   │
    │   ├── Shared/            # Universal components
    │   │   ├── Health.cs                # Universal health system
    │   │   ├── IDamageable.cs           # Damage interface
    │   │   └── Destructible.cs
    │   │
    │   ├── Systems/           # Non-MonoBehaviour logic
    │   │   ├── SpawnSystem.cs
    │   │   ├── CombatCalculator.cs
    │   │   └── PoolingSystem.cs
    │   │
    │   ├── Data/              # ScriptableObject definitions
    │   │   ├── UpgradeData.cs
    │   │   ├── EnemyData.cs
    │   │   └── WeaponData.cs
    │   │
    │   ├── Events/            # Event system
    │   │   └── GameEvents.cs            # Centralized event hub
    │   │
    │   └── UI/                # UI components
    │       ├── HealthBarUI.cs
    │       ├── UpgradeUI.cs
    │       └── XPBarUI.cs
    │
    ├── Prefabs/               # Reusable GameObjects
    │   ├── Player/
    │   ├── Enemies/
    │   ├── Projectiles/
    │   └── UI/
    │
    ├── Data/                  # ScriptableObject .asset files
    │   ├── Upgrades/
    │   ├── Enemies/
    │   └── Weapons/
    │
    ├── Art/                   # Visual assets
    ├── Audio/                 # Sound/music
    └── Materials/             # Unity materials
```

---

## 🗓 Development Roadmap

**Total Timeline**: 18-24 months  
**Time to Apprenticeship**: 11 months (~280 hours)  
**Weekly Time Budget**: ~6.5 hours (30min weekdays, 2hrs weekends)

### Phase 1: Foundation (Months 1-2) ⬅️ **YOU ARE HERE**
**Goal**: Kill enemies and level up

- [x] Unity tutorials (both devs)
- [ ] Basic movement + enemy AI
- [ ] Health/damage system
- [ ] Level-up trigger
- [ ] **Deliverable**: Playable combat loop

### Phase 2: Core Loop (Months 3-5)
**Goal**: Full run start to boss

- [ ] Room clearing + transitions
- [ ] Upgrade system (choose 1 of 3)
- [ ] Path system at levels 10/20
- [ ] 15-20 rooms with variety
- [ ] First boss fight
- [ ] **Deliverable**: Complete run with upgrades and boss

### Phase 3: Content & Polish (Months 6-9)
**Goal**: Two characters, endless playable

- [ ] Second character implementation
- [ ] 8-10 enemy types total
- [ ] Room modifiers (hazards, constraints)
- [ ] Meta-progression (currency, unlocks)
- [ ] Endless mode
- [ ] **Deliverable**: Replayable game with variety

### Phase 4: Polish & Buffer (Months 10-11)
**Goal**: Ready to show publicly

- [ ] Bug fixes, UI polish
- [ ] Playtesting with external testers
- [ ] Balance pass
- [ ] **Deliverable**: Presentable vertical slice

### Phase 5: Post-Apprenticeship (Months 12-24)
**Goal**: Ship-ready product

- [ ] Remaining characters (3-4 more)
- [ ] Full enemy roster (15 types)
- [ ] Marketing (Twitter devlog, Steam page)
- [ ] Art evaluation (hire artist if needed)
- [ ] Final polish + launch

---

## 🏗 Architecture

### Design Principles

1. **Component-Based Modularity**
   - Each script has ONE responsibility
   - PlayerController orchestrates, components execute
   - Example: `PlayerMovement` handles movement, not aiming or shooting

2. **Event-Driven Communication**
   - Cross-system communication via `GameEvents.cs`
   - Decouples systems (e.g., Enemy doesn't know XPManager exists)
   - Pattern: Fire event → multiple systems react independently

3. **ScriptableObject Data**
   - All configuration in .asset files (upgrades, enemies, weapons)
   - Easy to balance/tweak without touching code
   - Designer-friendly iteration

4. **Manager Orchestration**
   - Managers coordinate high-level flow (room transitions, state changes)
   - Managers DON'T do work—they tell other systems to act
   - Keep manager count small (5-7 max)

### Example: Event Flow for Enemy Death

```
Enemy.Die() 
  → GameEvents.OnEnemyKilled?.Invoke(this)
    → XPManager.AwardXP() listens
    → RoomManager.CheckRoomClear() listens
    → AudioManager.PlayDeathSound() listens
    → UIManager.UpdateKillCount() listens
```

### Code Style Guidelines

- **Naming**: PascalCase for public, camelCase for private
- **Comments**: Explain WHY, not WHAT (code should be self-documenting)
- **Single Responsibility**: If a class does 2+ things, split it
- **Events**: Always unsubscribe in OnDisable (prevents memory leaks)
- **Commit Messages**: Clear and descriptive (`"Add enemy spawn system"` not `"update"`)

---

## 👥 Team & Timeline

### Team
- **Developer 1**: Age 15, starting electrical apprenticeship in ~335 days
  - **Focus**: Game design, UI, progression systems, balance, art (potentially)
- **Developer 2**: Age 23, IT at Bunnings, disciplined, equally invested
  - **Focus**: Architecture, enemy AI, combat systems, optimization, code reviews

### Division of Labor
- **Shared**: Bug fixes, content creation, planning, weekly syncs
- **Git Workflow**: Feature branches → pull requests → code review → merge

### Meeting Cadence
**Weekly 30min Syncs**:
1. Review last week (10min) - what worked, what blocked
2. Plan this week (15min) - 2-3 tasks each (max 4 hours per task)
3. Long-term check (5min) - still on track? still motivated?

### Success Metrics (Monthly Check-ins)
- ✅ Did we hit the deliverable?
- ✅ Do we still want to work on this?
- ✅ Are we having fun?

**If 2/3 = yes, continue. If 1/3 = reassess. If 0/3 = pivot/pause.**

---

## 🚀 Getting Started

### Prerequisites
- Unity 6.3 LTS (or later)
- Git installed
- GitHub account with repo access
- Code editor (Visual Studio / Rider / VS Code)

### Setup Instructions

1. **Clone the repository**
   ```bash
   git clone https://github.com/[your-username]/exterminator-game.git
   cd exterminator-game
   ```

2. **Open in Unity**
   - Open Unity Hub
   - Click "Add" → navigate to cloned folder
   - Open project (Unity will import assets)

3. **Verify setup**
   - Open `Scenes/GameplayTest.unity`
   - Press Play
   - WASD to move, mouse to aim
   - Should see player capsule moving/rotating

4. **Create your feature branch**
   ```bash
   git checkout dev
   git pull origin dev
   git checkout -b feature/your-feature-name
   ```

---

## 🔄 Workflow

### Git Branching Strategy

```
main          # Stable, working builds only (NEVER commit directly)
  ↑
dev           # Integration branch (merge features here first)
  ↑
feature/task-name    # Your work-in-progress branches
```

### Daily Workflow

1. **Start of session**
   ```bash
   git checkout dev
   git pull origin dev
   git checkout -b feature/add-enemy-ai
   ```

2. **During work**
   - Commit often (every 30-60min or logical stopping point)
   - Push regularly (don't wait days)
   ```bash
   git add .
   git commit -m "Add basic enemy chase behavior"
   git push origin feature/add-enemy-ai
   ```

3. **End of session**
   - Create Pull Request on GitHub
   - Assign to other dev for review
   - **Don't merge your own PRs** (unless emergency)

### Commit Message Format

✅ **Good**:
- `"Add enemy spawn system"`
- `"Fix diagonal movement speed bug"`
- `"Implement upgrade choice UI"`

❌ **Bad**:
- `"update"`
- `"fix stuff"`
- `"changes"`

### Task Sizing
- **Max 4 hours per task**
- If bigger → break it down into sub-tasks
- Commit broken code with `[WIP]` tag if needed (don't wait for perfection)

---

## 🤝 Contributing

### Code Review Checklist

Before creating PR:
- [ ] Code compiles without errors/warnings
- [ ] No debug logs left in (or clearly marked as temporary)
- [ ] Components follow single responsibility principle
- [ ] Events unsubscribed in OnDisable if subscribed in OnEnable
- [ ] ScriptableObjects used for data (not hardcoded values)
- [ ] Tested in Play mode (doesn't crash immediately)

When reviewing:
- [ ] Does it solve the stated problem?
- [ ] Is the approach reasonable given our timeline?
- [ ] Any obvious performance concerns?
- [ ] Does it follow our architecture patterns?

**Review Goal**: Catch obvious issues, NOT nitpick perfection. We have 280 hours.

---

## 🎨 Design Philosophy

### Room Design: "Systemic Variation Over Raw Difficulty"
- Each room introduces constraint/modifier/risk-reward
- Forces tactical adaptation (not just "same fight but harder")
- Examples: hazards (fire/electricity), arena shrink, darkness, swarm rooms

### Enemy Design: "Enemies as Teachers"
- Each enemy type teaches a specific mechanic or punishes a mistake
- NOT just stat variations
- Design question: "What does this enemy force the player to learn?"

### Character Design: "Intentionally Broken"
- Every character has ONE overpowered mechanic
- Balanced by ONE meaningful constraint
- Goal: Make each character feel like discovering an exploit

### Difficulty Curve
- Early rooms: Moderately easy (not boring, not too hard)
- Smooth curve with spikes at bosses/elites
- Occasional surprise difficulty moments (keeps player alert)
- Never "too hard" at start, never "too easy" late game

---

## 🎯 Risk Management

### Known Risk: Burst-and-Crash Pattern
Developer 1 has a history of hyperfocusing for 40 hours → burning out → abandoning projects.

### Mitigation Strategies
- ⏱ **Time-box sessions**: 2hr max with breaks
- 💾 **Commit broken code**: Use `[WIP]` tags, don't need to finish features in one session
- 🎉 **Celebrate small wins**: Don't wait for "finished feature"
- 🎯 **One feature per week maximum**
- 🤝 **Brother accountability**: Weekly check-ins

### Burst Guidelines (When Hyperfocused)
✅ **DO**:
- Finish small tasks completely
- Test and commit working code
- Document clearly

❌ **DON'T**:
- Start 10 features at once
- Leave broken code for brother to fix
- Touch systems brother is working on

---

## 📝 Open Questions / TBD

### Combat
- [ ] Final movement speed value?
- [ ] Dodge mechanic specifics (dash? roll? i-frames?)
- [ ] Attack patterns per character?
- [ ] Damage feedback (screen shake, hit stop, particles)?

### Balance
- [ ] Heal % on level-up?
- [ ] Upgrade power levels (+10% damage? +20%?)
- [ ] XP curve (linear or exponential?)
- [ ] Enemy health/damage scaling?

### Economy
- [ ] Currency sources (performance bonuses? found treasure?)
- [ ] Shop frequency and inventory?
- [ ] Unlock pricing?

### Systems
- [ ] Health script architecture (events vs inheritance)?
- [ ] Player death behavior?
- [ ] Enemy death drops (loot system)?
- [ ] Room modifier types and selection?

---

## 📈 Marketing & Publishing (Future)

### Realistic Outcome Scenarios

**A) Passion Project (Most Likely)**  
200-500 copies, $500-$2k revenue, massive learning experience

**B) Modest Success (5-10% chance with good execution + luck)**  
5k-20k copies, $35k-$200k revenue

**C) Breakout Hit (<1% chance)**  
100k+ copies, $500k-$2M+ revenue

**Don't count on C, work toward B, expect A.**

### Marketing Plan (Start 6+ months before launch)
- Twitter devlog (GIFs of progress, #indiedev #roguelike)
- Weekly blog/YouTube updates
- Steam wishlists (10k+ is good)
- Streamer outreach
- Community building (Discord, Reddit)

### Art Strategy
- Start with programmer art
- Evaluate at Month 6
- If needed, hire artist ($1k-$3.5k budget post-apprenticeship)

---

## 📄 License

**Proprietary** - All rights reserved. This is a closed-source project for learning purposes.

---

## 🙏 Acknowledgments

- Inspired by: Hades, Dead Cells, Slay the Spire, Enter the Gungeon
- Architecture guidance: Unity best practices, GDC talks
- Mentorship: Claude AI (seriously, it's been helpful)

---

## 📞 Contact

For questions or collaboration inquiries:
- **GitHub Issues**: [Project Issues](https://github.com/[your-username]/exterminator-game/issues)
- **Discussions**: [Project Discussions](https://github.com/[your-username]/exterminator-game/discussions)

---

**Last Updated**: January 2026  
**Current Phase**: Month 1 - Foundation  
**Next Milestone**: Functional combat loop (kill enemies, gain XP, level up)

---

*"Exterminate with style. 🪲🔫"*
