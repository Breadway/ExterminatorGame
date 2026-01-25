# 🎮 Game Design Document

## 🎨 Aesthetic & Visual Theme

### "Gritty Industrial Satire"
The game visualizes the mundane horror of minimum-wage pest control in a world where bugs are dog-sized.

*   **Visual Style:** **Lo-Fi Industrial (PSX/Early-PC)**. Low-poly models with flat shading or pixelated textures. "Crusty" aesthetics—rust, duct tape, stained concrete, and flickering fluorescent lights.
*   **Color Palette (The 'Safety' Palette):**
    *   **Primary:** "Hazard Orange" & "Safety Yellow" (Player gear, UI warnings).
    *   **Secondary:** "Industrial Grey" & "Rust Red" (Environments, machinery).
    *   **Accent:** "Toxic Purple" or "Neon Green" (Bug ichor, chemical sprays).
*   **UI Philosophy:** **Diegetic Corporate Paperwork**.
    *   The HUD resembles a digital clipboard or retro safety terminal.
    *   Damage numbers print out like receipts.
    *   Health bars look like pressure gauges or liquid fill levels.
    *   Terminology is bureaucratic (e.g., "Performance Review" instead of "Level Up").

---

## 👥 Characters (The Exterminators)

Each character follows the "Intentionally Broken" design philosophy: One overpowered mechanic balanced by a meaningful, gameplay-defining constraint.

### 1. The Pyrotechnic ("The Firebug")
> *"I love the smell of napalm in the morning. It smells like... overtime."*

*   **Role:** AOE / Aggressive Kiting
*   **Weapon:** **Leak-Prone Flamethrower**
*   **The Power:** **"Inferno."** Fire spreads aggressively between enemies and persists on surfaces for 8 seconds. Enemies ignited spread fire to others on contact. Creates cascading chain reactions.
*   **The Constraint:** **"Backdraft."** The fuel tank leaks behind you as you move. Standing still for >1 second causes a fuel puddle to ignite beneath you. You must keep moving or take rapid damage, but your movement leaves a trail of fire hazards.

### 2. The Apiarist ("The Beekeeper")
> *"They don't pay me enough to touch the bugs myself."*

*   **Role:** Summoner / Horde Management
*   **Weapon:** **Pheromone Dart Gun** (Low damage, marks targets)
*   **The Power:** **"The Hive."** Killing marked enemies spawns Ghost Bees (max 30). Bees automatically attack nearby threats. Each bee adds +2% damage to your weapon. Your swarm is your scaling mechanic.
*   **The Constraint:** **"Shared Fate."** Your HP is capped at 50 and cannot be healed traditionally. When you take damage, you lose bees equal to damage taken (1 bee = 1 HP). If you have no bees, damage goes to your base HP pool. At 0 HP with 0 bees, you die. You must maintain your swarm aggressively or become fragile.

### 3. The Custodian ("The Janitor")
> *"Clean up on Aisle 4. And 5. And everywhere."*

*   **Role:** Precision Melee / Spacing
*   **Weapon:** **The Mop** (Halberd-style reach)
*   **The Power:** **"Sanitize."** The mop has extended reach. Hitting enemies at max range (the outer third of your swing arc) deals 3x damage and launches them into walls for "Splat" bonus damage.
*   **The Constraint:** **"Safety Violation."** Enemies inside your minimum range (50% of mop reach) take 0 damage. A danger zone indicator shows when enemies breach your guard. You must control spacing or become helpless.

### 4. The Intern ("The Heavy")
> *"My back hurts and I'm not even getting paid."*

*   **Role:** Mobile Tank / Ablative Armor
*   **Weapon:** **Industrial Foam Sprayer** (Short-range cone)
*   **The Power:** **"Foam Fortress."** Your weapon coats you in hardening foam, building up damage-absorbing armor layers (max 200 armor). The foam also slows and damages enemies caught in the spray.
*   **The Constraint:** **"Overencumbered."** Your movement speed decreases as your armor increases (100 armor = 50% slower). At max armor you're a slow juggernaut. Taking damage removes armor and speeds you up. You oscillate between fast/fragile and slow/tanky.

### 5. The Electrician ("Sparky")
> *"It's not a safety violation if it works."*

*   **Role:** Chain DPS / Risk Management
*   **Weapon:** **Jumper Cables** (Chain Lightning)
*   **The Power:** **"The Circuit."** Chain lightning arcs between enemies within range. Each additional arc in the chain increases damage by 25%. Killing an enemy in a chain causes an electrical explosion.
*   **The Constraint:** **"Overload."** You have no ammo limit, but your weapon builds heat. At 100% heat, your weapon force-discharges, dealing damage to you equal to 50% of max HP and stunning you for 1 second. You must manage your heat by toggling fire carefully or risk catastrophic failure.

### 6. The Scientist ("Dr. Overkill")
> *"The pest control is experimental. The lawsuits are real."*

*   **Role:** Precision Burst / Glass Cannon
*   **Weapon:** **Prototype Particle Beam** (Charge-up laser)
*   **The Power:** **"Molecular Destabilization."** Fully charged shots pierce through all enemies and walls, dealing massive damage. Uncharged rapid shots do minimal damage.
*   **The Constraint:** **"Critical Instability."** Charging your weapon above 75% has a 5% chance per second to cause a critical failure, dealing 30% of your HP as self-damage and stunning you. The longer you charge, the more you gamble.
