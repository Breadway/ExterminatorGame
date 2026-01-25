# UI Terminology Guide - "Vermin-B-Gone Inc." Style

This document maps standard game terms to lore-consistent "Corporate Safety Manual" terminology.

## Core HUD Elements

| Standard Term | Company Term | Notes |
|---------------|--------------|-------|
| Health | VITALS | Medical/safety terminology |
| Player Level | CLEARANCE LEVEL | Corporate hierarchy reference |
| XP Bar | PERFORMANCE REVIEW | HR department language |
| Room Number | SECTOR | Industrial zone designation |
| Enemies | HOSTILES | Security threat classification |
| Kill Count | BIOMASS ELIMINATED | Sanitation report language |
| Boss | PRIORITY TARGET | Threat classification |

## Menu & UI Labels

| Standard Term | Company Term | Notes |
|---------------|--------------|-------|
| Game Over | CONTRACT TERMINATED | Employment language |
| Victory | CONTRACT FULFILLED | Successful completion |
| Pause Menu | WORK BREAK | Union-mandated rest |
| Settings | EQUIPMENT SETTINGS | Tool configuration |
| Continue | RESUME DUTIES | Back to work |
| Retry | RE-DEPLOY | Another attempt |
| Main Menu | DISPATCH OFFICE | Company HQ |

## Upgrades & Items

| Standard Term | Company Term | Notes |
|---------------|--------------|-------|
| Damage Boost | EFFICACY UPGRADE | Corporate efficiency speak |
| Health Pack | COMPANY COFFEE | Reference to LORE.md vending machine |
| Speed Boost | OVERTIME MODE | Working faster |
| Armor | SAFETY EQUIPMENT | OSHA compliance |
| Critical Hit | APPROVED TECHNIQUE | By-the-book method |
| Special Ability | UNAUTHORIZED MOD | Rule-breaking equipment |

## Progression & Meta

| Standard Term | Company Term | Notes |
|---------------|--------------|-------|
| Currency | PERFORMANCE BONUS | Payment for work |
| Unlock Character | NEW HIRE | Recruiting terminology |
| Upgrade Path | CAREER TRACK | Promotion system |
| Achievement | SAFETY MILESTONE | Corporate recognition |
| Leaderboard | EMPLOYEE RANKINGS | Performance metrics |
| Run Stats | INCIDENT REPORT | Post-mission paperwork |

## Status Effects & Combat

| Standard Term | Company Term | Notes |
|---------------|--------------|-------|
| Poisoned | CONTAMINATED | Exposure hazard |
| Burning | THERMAL INCIDENT | Fire safety violation |
| Slowed | ENCUMBERED | Mobility restriction |
| Stunned | SHOCKED | Electrical hazard |
| Invulnerable | UNION PROTECTED | Can't be fired |
| Bleeding | INJURED ON DUTY | Workers comp claim |

## Environmental Hazards

| Standard Term | Company Term | Notes |
|---------------|--------------|-------|
| Lava/Fire | UNAUTHORIZED FLAME | Safety violation |
| Toxic Gas | VENTILATION FAILURE | Air quality issue |
| Spikes | EXPOSED HARDWARE | OSHA violation |
| Dark Area | LIGHTING FAILURE | Maintenance required |
| Explosive Barrel | HAZMAT CONTAINER | Improper storage |

## Voice Lines / Flavor Text Style

Examples of how NPCs/UI should "speak":

- "Remember: You are replaceable. The equipment is not."
- "Employees found wrapped in cocoons will be marked 'Absent Without Leave'"
- "Congratulations on your promotion to Clearance Level 5. (No raise included.)"
- "Warning: Standing in fire constitutes a safety violation and voids your insurance."
- "Per company policy, healing items cost 200% markup."

## Color-Coded Warnings (Match USS Theme)

- **Safety Yellow** (`--safety-yellow`): Important info, labels
- **Hazard Orange** (`--hazard-orange`): Warnings, enemy presence
- **Caution Red** (`--caution-red`): Danger, low health, critical
- **Concrete Grey** (`--concrete-grey`): Neutral info, descriptions

## Usage Guidelines

1. **Be Consistent**: Always use "SECTOR" not "Room" or "Level"
2. **Corporate Tone**: Phrases should sound bureaucratic and cynical
3. **Safety Manual**: When in doubt, think "what would OSHA call this?"
4. **Darkly Comedic**: The horror is real, but the paperwork is worse

## Implementation Checklist

When creating new UI:
- [ ] Replace generic game terms with Company Terms
- [ ] Use ALL CAPS for official designations (SECTOR, VITALS, etc.)
- [ ] Apply Safety Yellow to important labels
- [ ] Include disclaimer/liability text where appropriate
- [ ] Add sarcastic tooltips in italic text

## Examples in Context

### Good ✅
```
CLEARANCE LEVEL: 5
PERFORMANCE REVIEW: 850/1000 XP
SECTOR: 12/25
WARNING: HOSTILE BIOMASS DETECTED
CONTRACT TERMINATED - REASON: FAILURE TO SURVIVE
```

### Bad ❌
```
Level: 5
XP: 850/1000
Room: 12/25
Warning: Enemies Nearby
Game Over - You Died
```

---

**Last Updated**: Phase 2 - HUD Implementation
**Reference**: See `LORE.md` for full world-building context
