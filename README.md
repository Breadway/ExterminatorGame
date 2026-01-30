# 🪲 Hazard Pay (Working Title)

> A top-down 2D roguelike where you play as pest exterminators clearing bug infestations. Think Enter the Gungeon meets Vampire Survivors with a darkly comedic twist.

[![Unity Version](https://img.shields.io/badge/Unity-6.3_LTS-black.svg?style=flat&logo=unity)](https://unity.com/)
[![License](https://img.shields.io/badge/License-Proprietary-red.svg)](LICENSE)
[![Development Phase](https://img.shields.io/badge/Phase-2_Core_Loop_&_2D_Refactor-blue.svg)](#development-roadmap)

---

## 📋 Navigation

**For Players/Game Design**: [Overview](#-game-overview) • [Features](#-game-features) • [How to Play](#how-to-play) • [Status](#-playable-status)

**For Developers**: [Getting Started](#-getting-started) • [Development](#-development-section) • [Tech Stack](#-technical-stack) • [Architecture](#-architecture--code-standards) • [Contributing](#-contributing)

---

## 🎮 Game Overview

**Hazard Pay** is a top-down 2D pixel-art roguelike featuring "intentionally broken" character progression. Players choose from 4-5 unlockable exterminators, each with overpowered mechanics balanced by meaningful constraints, to clear procedurally arranged industrial sectors.

### How to Play
1. **Navigate** through 25 procedurally arranged rooms (Sectors) with branching paths
2. **Fight** increasingly challenging bug infestations using tactical fire spread and crowd control
3. **Choose** 1 of 3 upgrades (Performance Reviews) after each room
4. **Level up** to unlock path abilities at levels 10, 20, 30
5. **Defeat** mid-boss (Sector ~12) and final boss (Sector 25)
6. **Unlock** Overtime Mode (Endless) for infinite scaling challenge

### Core Philosophy
| Pillar | What It Means |
|--------|---------|
| **Systemic Variation** | Each room forces tactical adaptation, not just higher numbers |
| **Enemies as Teachers** | Every bug type teaches a specific mechanic |
| **Intentionally Broken** | Overpowered abilities balanced by meaningful constraints |

---

## 🎮 Game Features

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

**1. Room Upgrades (Per-Run)**
- Choose 1 of 3 after each room clear
- Categories: Power (damage/weapons), Defense (HP/resistances), Utility (speed/cooldowns)
- ~25 upgrade choices per full run

**2. Level-Up Path System (Per-Run)**
- **Every Level**: Auto-heal X% HP
- **Level 10**: Choose 1 of 3 major path branches (significant power spike)
- **Level 20**: Choose 1 of 2 options based on Level 10 choice
- **Level 30**: Final path choice (Endless Mode only, massive power spike)
- **Level 31+**: Permanent character-specific buffs each level (Endless only)

**3. Meta-Progression (Persistent)**
- Earn currency from runs
- Unlock new characters (after milestone achievements)
- Unlock additional character path trees
- Small meaningful upgrades

### Map Structure
- **25 rooms total** with Slay the Spire-style branching paths
- **Room Types**: Combat, Shop, Elite, "Unknown" (random modifiers/events)
- **Bosses**: Mid-boss (~room 12), Final boss (room 25)
- **Endless Mode**: Unlocked after beating final boss, separate mode per map

### Enemy Types
"Enemies as teachers" - each type exists to teach a specific mechanic:

- **Roaches**: Swarm tactics (teaches AOE value)
- **Spiders**: Web traps, ceiling walkers (teaches awareness)
- **Wasps**: Flying, ranged (teaches target priority)
- **Beetles**: Armored (teaches armor-piercing value)
- **Larvae**: Evolve if not killed quickly (teaches tempo)

---

## � Playable Status

### What's Done (Phase 1 ✅)
- ✅ Combat system (move, aim, shoot, damage)
- ✅ Enemy AI and spawning
- ✅ Health system with visual feedback
- ✅ XP and leveling system
- ✅ Basic UI (health bar, XP bar, level-up screen)
- ✅ **Object pooling system** (2000+ enemy capacity)
- ✅ **Status effect system** with visual effects
- ✅ **Fire spread mechanics** working at scale

### Performance Achievement 🚀
- **2000+ simultaneous enemies** at stable 60 FPS
- **Zero GC allocations** during gameplay (after pool warmup)
- **Status effects with VFX** working on all enemies
- True "horde extermination" gameplay enabled

*See [PERFORMANCE.md](PERFORMANCE.md) for detailed metrics and optimizations.*

### What's Next (Phase 2 ⬅️ IN PROGRESS)
- [ ] Room clearing and transitions
- [ ] Upgrade selection UI (pick 1 of 3)
- [ ] Path branches at levels 10/20
- [ ] 15-20 varied room designs
- [ ] First boss encounter

### TBD Questions
- Movement speed tuning
- Dodge mechanic (dash/roll/i-frames?)
- Damage feedback visuals (screen shake, particles)
- Heal percentage on level-up
- Character-specific attack patterns

---

## 🚀 Getting Started

### Prerequisites
- Unity 6.3 LTS or later
- Git installed
- GitHub account with repo access
- Code editor (VS, Rider, or VS Code)

### First-Time Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/[your-username]/exterminator-game.git
   cd exterminator-game
   ```

2. **Open in Unity**
   - Open Unity Hub
   - Click "Add" → select cloned folder
   - Unity automatically imports assets

3. **Test the setup**
   - Open `Scenes/GameplayTest.unity`
   - Press Play
   - Controls: WASD to move, mouse to aim
   - Should see player capsule moving smoothly

4. **Create a feature branch**
   ```bash
   git checkout dev
   git pull origin dev
   git checkout -b feature/your-feature-name
   ```

---

# 📋 Development Section

*For developers working on the project. Game designers and players can skip to the end.*

---

## 🛠 Technical Stack

| Component | Technology |
|-----------|-----------|
| **Engine** | Unity 6.3 LTS |
| **Rendering** | Universal Render Pipeline (URP) |
| **Perspective** | 2D (top-down orthographic camera) |
| **Physics** | Unity 2D Physics (Rigidbody2D, Collider2D) |
| **Language** | C# (.NET Standard 2.1) |
| **Version Control** | Git + GitHub |
| **Project Management** | JIRA |

### Architecture Approach
- **Component-based**: Modular MonoBehaviours with single responsibilities
- **Event-driven**: Centralized `GameEvents.cs` for system communication
- **Data-driven**: ScriptableObjects for all configuration (upgrades, enemies, weapons)
- **Manager orchestration**: Sparse use of singleton managers to coordinate systems

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
    │   │   ├── PlayerAttack.cs          # Attack logic
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

**Total Timeline**: 20 months | **Weekly Budget**: ~6.5 hours

### Phase 1: Foundation (Weeks 1-2) ✅ **COMPLETED**
**Deliverable**: Playable combat loop (kill enemies, gain XP, level up)
- [x] Unity setup and tutorials
- [x] Movement + enemy AI
- [x] Health/damage system
- [x] Event system wiring
- [x] Level-up system

### Phase 2: Core Loop (Months 1-3) ⬅️ **CURRENT PHASE**
**Deliverable**: Complete run with upgrades and boss
- [ ] Room clearing and transitions
- [ ] Upgrade system (1 of 3 choices)
- [ ] Path progression (levels 10/20)
- [ ] 15-20 varied rooms
- [ ] First boss fight

### Phase 3: Content & Polish (Months 4-8)
**Deliverable**: Replayable game with variety
- [ ] Second character
- [ ] 8-10 enemy types
- [ ] Room modifiers (hazards, constraints)
- [ ] Meta-progression (currency, unlocks)
- [ ] Endless mode

### Phase 4: Polish & Showcase (Months 9-10)
**Deliverable**: Presentable vertical slice
- [ ] Bug fixes and UI polish
- [ ] External playtesting
- [ ] Balance pass
- [ ] Ready for public viewing

### Phase 5: Expansion (Months 11-20)
**Deliverable**: Ship-ready product
- [ ] 3-4 additional characters
- [ ] Full enemy roster (15 types)
- [ ] Marketing (Twitter devlog, Steam page)
- [ ] Professional art evaluation
- [ ] Final polish and launch

---

## 🏗 Architecture & Code Standards

### Design Principles

**1. Component-Based Modularity**
- Each script has ONE responsibility
- `PlayerController` orchestrates; `PlayerMovement`, `PlayerAiming`, `PlayerAttack` execute
- Example: Movement doesn't handle aiming or shooting

**2. Event-Driven Communication**
- Cross-system messaging via `GameEvents.cs`
- Decouples systems (Enemy doesn't need to know about XPManager)
- Pattern: `OnEventFired → Multiple systems react independently`

**3. Data-Driven Configuration**
- All game data lives in ScriptableObjects (.asset files)
- Upgrades, enemies, weapons → designer-friendly, no code changes needed

**4. Sparse Manager Use**
- Managers coordinate high-level flow only
- Managers DON'T do work—they tell other systems to act
- Target: 5-7 managers maximum

### Example: Event Flow for Enemy Death
```
Enemy.Die()
  → GameEvents.OnEnemyKilled?.Invoke(this)
    → XPManager.AwardXP()
    → RoomManager.CheckRoomClear()
    → AudioManager.PlayDeathSound()
    → UIManager.UpdateKillCount()
```

### Code Style Guidelines
| Rule | Details |
|------|---------|
| **Naming** | `PascalCase` for public, `camelCase` for private |
| **Comments** | Explain WHY, not WHAT (code should self-document) |
| **Responsibility** | If a class does 2+ things, split it |
| **Events** | Always unsubscribe in `OnDisable()` to prevent leaks |
| **Commits** | Clear messages (`"Add enemy spawn system"`, not `"update"`)

---

## 👥 Team & Timeline

### The Team
| Role | Details | Focus |
|------|---------|-------|
| **Developer 1** | Age 15, Game design, UI, progression, balance, art |
| **Developer 2** | Age 23, Architecture, enemy AI, combat, optimization |

### Weekly Workflow

**30-minute sync meetings**:
1. **Review** (10min) — What worked? What blocked us?
2. **Plan** (15min) — 2-3 tasks each, max 4 hours per task
3. **Check-in** (5min) — Still on track? Still motivated?

### Success Metrics (Monthly)
Each month ask:
- ✅ Did we hit the deliverable?
- ✅ Do we still want to work on this?
- ✅ Are we having fun?

**Result**: 2-3/3 ✅ = continue | 1/3 ✅ = reassess | 0/3 ✅ = pivot or pause

### Risk Management: Burst-and-Crash Pattern
Developer 1 tendency: Hyperfocus for 40 hours → burnout → abandon

**Mitigation**:
- ⏱ Time-box sessions: 2 hours max with breaks
- 💾 Commit broken code: Use `[WIP]` tags
- 🎉 Celebrate small wins: Don't wait for "finished feature"
- 🎯 One feature max per week
- 🤝 Weekly accountability check-ins

---

## 🔄 Workflow

### Git Strategy
```
main          # Stable builds only (protected, no direct commits)
  ↑
dev           # Integration branch for features
  ↑
feature/*     # Your work-in-progress branches
```

### Daily Process

**Start of session**:
```bash
git checkout dev && git pull origin dev
git checkout -b feature/task-name
```

**During work** (commit every 30-60min):
```bash
git add .
git commit -m "Add feature description"
git push origin feature/task-name
```

**End of session**:
- Create Pull Request on GitHub
- Assign to other developer for review
- DON'T merge your own PRs (unless emergency)

### Commit Message Guidelines

✅ **Good**: `"Add enemy spawn system"`, `"Fix diagonal movement speed bug"`
❌ **Bad**: `"update"`, `"fix stuff"`, `"changes"`

### Task Guidelines
- **Maximum 4 hours per task** — break larger work into sub-tasks
- **Commit WIP code** — use `[WIP]` tag, don't wait for perfection
- **Push regularly** — don't hold changes locally for days

---

## 🤝 Contributing

### Pre-Pull Request Checklist
- [ ] Code compiles without errors/warnings
- [ ] No debug logs (or marked `// TODO: remove`)
- [ ] Single responsibility per component
- [ ] Event unsubscriptions in `OnDisable()` if subscribed in `OnEnable()`
- [ ] Game data in ScriptableObjects (no hardcoded values)
- [ ] Tested in Play mode (doesn't crash)

### Code Review Process
When reviewing, ask:
- Does it solve the stated problem?
- Is the approach reasonable for our timeline?
- Any obvious performance issues?
- Does it follow our architecture patterns?

**Goal**: Catch critical issues, not nitpick style. We have 280 hours total.

---

## 🎨 Design Philosophy

### Room Design: Systemic Variation Over Raw Difficulty
Each room introduces new constraints/modifiers/risk-reward scenarios that force tactical adaptation.
- ✅ Different challenge (not just higher numbers)
- ✅ Examples: hazards, arena shrinking, darkness, swarm composition
- ❌ Don't make it "same fight but HP doubled"

### Enemy Design: Enemies as Teachers
Each bug type teaches a specific mechanic or counters a strategy.
- ✅ Design question: "What does this enemy force the player to learn?"
- ✅ Examples: Roaches teach AOE value, Beetles teach armor penetration
- ❌ Don't create stat variations

### Character Design: Intentionally Broken
Each character has one overpowered mechanic balanced by a meaningful constraint.
- ✅ Makes the character feel like an exploit discovery
- ✅ Examples: Beekeeper (allies, no direct damage), Flamethrower (spreads, takes damage)
- ❌ Balanced characters feel boring

### Difficulty Progression
- **Early rooms**: Moderately easy, never boring
- **Mid-game**: Smooth difficulty curve with spikes at bosses/elites
- **Late-game**: Occasional surprises to keep players alert
- **General rule**: Never too hard at start, never too easy at end

## 📝 Open Questions & TBD

### Combat & Balance
| Question | Options |
|----------|---------|
| Movement speed | Final tuning value? |
| Dodge mechanic | Dash? Roll? I-frames? |
| Attack patterns | Per-character variations? |
| Damage feedback | Screen shake, hit-stop, particles? |
| Heal % on level-up | Percentage value? |
| Upgrade power scaling | +10%, +20%, or variable? |
| XP curve | Linear or exponential? |
| Enemy scaling | HP/damage progression formula? |

### Game Economy
- Currency sources (performance bonuses? found treasure? room completion?)
- Shop frequency and inventory size
- Unlock pricing structure

### Technical Systems
- Health system architecture (events vs inheritance)?
- Player death behavior and penalties
- Enemy death drops (loot system)?
- Room modifier types and selection algorithm

## 📈 Marketing & Long-Term Vision

### Realistic Outcome Scenarios

| Scenario | Likelihood | Copies | Revenue |
|----------|-----------|--------|---------|
| **Passion Project** | 80%+ | 200-500 | $500-$2k |
| **Modest Success** | 5-10% | 5k-20k | $35k-$200k |
| **Breakout Hit** | <1% | 100k+ | $500k-$2M+ |

**Strategy**: Don't count on C, work toward B, expect A. This is a learning project first, commercial second.

### Marketing Timeline (Start 6+ months before launch)
- **Twitter**: Regular dev logs with GIFs (#indiedev #roguelike)
- **Blog/YouTube**: Weekly progress updates
- **Steam**: Build 10k+ wishlists pre-launch
- **Outreach**: Contact streamers and communities
- **Discord**: Build community early

### Art Strategy
- Start with programmer art
- Evaluate at Month 6
- Hire artist if needed (budget: $1k-$3.5k post-apprenticeship)

---

## 📄 License & Contact

**License**: Proprietary — All rights reserved (closed-source learning project)

**Questions or feedback**:
- [GitHub Issues](https://github.com/[your-username]/exterminator-game/issues)
- [GitHub Discussions](https://github.com/[your-username]/exterminator-game/discussions)

---

## 🙏 Inspiration & Thanks

**Game Inspiration**: Hades, Dead Cells, Slay the Spire, Enter the Gungeon
**Technical Guidance**: Unity best practices, GDC talks, community resources

---

**Last Updated**: January 2026 | **Current Phase**: Month 1 — Foundation | **Next Milestone**: Functional combat loop

*"Exterminate with style. 🪲🔫"*
