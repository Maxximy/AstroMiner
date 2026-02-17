# Astrominer - Game Design Document

**Genre:** Idle / Clicker  
**Engine:** Unity  
**Platform:** TBD  
**Version:** 1.0

---

## High Concept

Astrominer is an idle game where players mine asteroids by hovering a damage circle over them. Destroyed asteroids release particles that are collected by the player's ship, converting to credits used to unlock upgrades in a tech tree. The goal is to earn enough credits per timed run to unlock progressively harder levels with rarer resources.

---

## Core Gameplay Loop

1. **Run Phase** — Timed mining session begins
2. **Mine** — Move circle with mouse, hover over asteroids to deal damage
3. **Collect** — Destroyed asteroids break into particles that fly to the ship
4. **Run Ends** — Timer expires, results screen shows credits earned
5. **Upgrade Phase** — Spend credits in tech tree to improve mining capabilities
6. **Repeat** — Start new run with upgraded stats
7. **Advance** — Once credit threshold is met, unlock and purchase next level

---

## Game View & Controls

**Layout:**
- Ship is stationary at the bottom center of the screen
- Asteroids spawn at the top and drift slowly downward
- A circular mining zone follows the mouse cursor

**Controls:**
- Mouse movement controls the mining circle position
- Number keys (1-4) activate ship skills, aimed at current circle position
- No clicks required for basic mining (skills add optional active play)

**Visuals:**
- 3D rotating asteroids with PBR materials
- Each resource type has a distinct visual appearance (e.g., copper = orange/brown, gold = shiny yellow)
- Particle effects when asteroids are destroyed

---

## Mining Mechanics

| Stat | Description | Upgradeable |
|------|-------------|-------------|
| Circle Radius | Size of the mining zone | ✓ |
| Damage Points | Damage dealt per tick | ✓ |
| Damage Rate | Time between damage ticks (seconds) | ✓ |

- Multiple asteroids can be damaged simultaneously if within the circle
- Asteroids that reach the bottom of the screen are lost (missed opportunity)

---

## Asteroid Types

### Standard Asteroids

| Tier | Resource | HP | Credit Value | Unlock Level |
|------|----------|-----|--------------|--------------|
| 1 | Iron | Low | Low | 1 |
| 1 | Copper | Low | Low | 1 |
| 2 | Silver | Medium | Medium | 2+ |
| 2 | Cobalt | Medium | Medium | 3+ |
| 3 | Gold | High | High | 4+ |
| 3 | Titanium | High | High | 5+ |

*Exact values to be defined per level in config.*

### Special Asteroids

| Type | Behavior | Reward |
|------|----------|--------|
| **Fragile** | Very low HP, brief spawn window | Bonus credits, rewards quick reactions |
| **Mega** | Very high HP, large size | High credit reward, requires sustained focus |
| **Cluster** | Breaks into 3-5 smaller asteroids when destroyed | Each fragment gives credits |

*Additional special types can be added in future updates.*

---

## Level System

- Fixed number of levels, each with a predefined configuration
- Each level has its own **drop table** determining asteroid spawn rates and types
- Higher levels introduce rarer resources and tougher asteroids (more HP)
- Asteroid movement speed remains constant; difficulty scales via HP

**Level Progression:**
1. Complete runs to accumulate credits
2. Meet credit threshold for level advancement
3. Purchase the "Advance" upgrade in tech tree
4. Unlock next level with new resources and challenges

**Upgrades carry over** between levels — no resets.

---

## Economy

**Currency:** Credits (universal, converted from all resources)

**Credit Flow:**
```
Asteroid Destroyed → Particles Collected → Credits Added to Total
```

**Display:** Running total of credits shown during gameplay

---

## Tech Tree & Upgrades

The tech tree is displayed between runs and organized into themed branches. Players choose which direction to invest in first, allowing different playstyles and strategies.

### Tech Tree Layout

```
                                    [START]
                                       │
                    ┌──────────────────┼──────────────────┐
                    │                  │                  │
                    ▼                  ▼                  ▼
              ┌──────────┐      ┌──────────┐      ┌──────────┐
              │  MINING  │      │ ECONOMY  │      │   SHIP   │
              └────┬─────┘      └────┬─────┘      └────┬─────┘
                   │                 │                  │
        ┌──────────┼──────────┐     │      ┌───────────┼───────────┐
        │          │          │     │      │           │           │
        ▼          ▼          ▼     │      ▼           ▼           ▼
    ┌───────┐ ┌───────┐ ┌───────┐   │  ┌───────┐  ┌───────┐  ┌───────┐
    │Radius │ │Damage │ │ Rate  │   │  │ Laser │  │ Chain │  │  EMP  │
    │   I   │ │   I   │ │   I   │   │  │Burst I│  │Light I│  │Pulse I│
    └───┬───┘ └───┬───┘ └───┬───┘   │  └───┬───┘  └───┬───┘  └───┬───┘
        │         │         │       │      │          │          │
        ▼         ▼         ▼       │      ▼          ▼          ▼
    ┌───────┐ ┌───────┐ ┌───────┐   │  ┌───────┐  ┌───────┐  ┌───────┐
    │Radius │ │Damage │ │ Rate  │   │  │ Laser │  │ Chain │  │  EMP  │
    │  II   │ │  II   │ │  II   │   │  │  II   │  │  II   │  │  II   │
    └───┬───┘ └───┬───┘ └───┬───┘   │  └───┬───┘  └───┬───┘  └───┬───┘
        │         │         │       │      │          │          │
        ▼         ▼         ▼       │      ▼          ▼          ▼
    ┌───────┐ ┌───────┐ ┌───────┐   │  ┌───────┐  ┌───────┐  ┌───────┐
    │Radius │ │ Crit  │ │  DoT  │   │  │ Laser │  │ Chain │  │  EMP  │
    │  III  │ │Chance │ │   I   │   │  │  III  │  │  III  │  │  III  │
    └───────┘ └───┬───┘ └───┬───┘   │  └───┬───┘  └───┬───┘  └───┬───┘
                  │         │       │      │          │          │
                  ▼         ▼       │      └────┬─────┴────┬─────┘
              ┌───────┐ ┌───────┐   │           │          │
              │ Crit  │ │  DoT  │   │           ▼          ▼
              │Multi  │ │  II   │   │      ┌─────────┐ ┌─────────┐
              └───────┘ └───────┘   │      │Overcharg│ │  Combo  │
                                    │      │    I    │ │ Mastery │
                                    │      └────┬────┘ └─────────┘
                                    │           │
                    ┌───────────────┼───────────┼───────────────┐
                    │               │           │               │
                    ▼               ▼           ▼               ▼
              ┌──────────┐   ┌──────────┐ ┌──────────┐   ┌──────────┐
              │ Resource │   │  Lucky   │ │Overcharg│   │Abundance │
              │ Multi I  │   │ Strike I │ │   II    │   │    I     │
              └────┬─────┘   └────┬─────┘ └────┬────┘   └────┬─────┘
                   │              │            │             │
                   ▼              ▼            ▼             ▼
              ┌──────────┐   ┌──────────┐ ┌──────────┐   ┌──────────┐
              │ Resource │   │  Lucky   │ │Overcharg│   │Abundance │
              │ Multi II │   │Strike II │ │   III   │   │    II    │
              └────┬─────┘   └────┬─────┘ └──────────┘   └──────────┘
                   │              │
                   ▼              ▼
              ┌──────────┐   ┌──────────┐
              │ Resource │   │  Lucky   │
              │Multi III │   │Strike III│
              └──────────┘   └──────────┘


                              [PROGRESSION BRANCH]
                              (Unlocks at tree edges)

    Level 1 ──► [Advance to Level 2] ──► Level 2 ──► [Advance to Level 3] ──► ...
                (Requires threshold)                  (Requires threshold)


                                [RUN BRANCH]
                           (Accessible from START)

                    [Level Time I] ──► [Level Time II] ──► [Level Time III]
```

### Branch Descriptions

**MINING Branch** — Core damage and circle improvements
- Circle Radius: Larger mining zone
- Damage Points: More damage per tick
- Damage Rate: Faster ticks
- Critical Hit Chance: % chance to crit
- Critical Hit Multiplier: Crit damage bonus
- Damage Over Time: Burning effect after leaving circle

**ECONOMY Branch** — Credit generation and luck
- Resource Multiplier: Global credit bonus
- Lucky Strike: Double drop chance
- Abundance: Increased asteroid spawn rate

**SHIP Branch** — Active skills and abilities
- Laser Burst: Line damage to circle position
- Chain Lightning Bomb: Chaining damage on impact
- EMP Pulse: AoE damage at circle position
- Overcharge: Temporary damage boost
- Combo Mastery: Bonus damage when using skills in quick succession (future upgrade)

**RUN Branch** — Session duration
- Level Time: Extends run duration

**PROGRESSION Branch** — Level advancement
- Advance upgrades unlock next levels (requires credit threshold)

### Upgrade Tiers

Each upgrade has multiple tiers with increasing costs and effects:

| Tier | Cost Multiplier | Effect Increase |
|------|-----------------|-----------------|
| I | 1x | Base effect |
| II | 3x | +50% |
| III | 8x | +100% |
| IV+ | Scaling | Diminishing returns |

*Exact values to be balanced during playtesting.*

---

## Ship Skills

Ship skills are active abilities that fire from the ship toward the current circle position. They must be unlocked through the Ship branch of the tech tree.

### Controls

| Key | Skill |
|-----|-------|
| 1 | Laser Burst |
| 2 | Chain Lightning Bomb |
| 3 | EMP Pulse |
| 4 | Overcharge |

### Skill Descriptions

**Laser Burst**
- Fires a beam from the ship to the circle position
- Damages all asteroids in the beam's path
- Upgrades: Damage increase, Cooldown reduction, Beam width

**Chain Lightning Bomb**
- Launches a projectile to the circle position
- On impact, lightning chains between nearby asteroids
- Upgrades: Damage increase, Cooldown reduction, Chain count, Chain range

**EMP Pulse**
- Emits a pulse centered on the circle position
- Damages all asteroids within a radius
- Upgrades: Damage increase, Cooldown reduction, Pulse radius

**Overcharge**
- Temporarily supercharges the mining circle
- Doubles damage rate for X seconds
- Upgrades: Duration increase, Cooldown reduction, Damage multiplier

### Skill Upgrade Structure

Each skill follows this upgrade path within the Ship branch:

```
[Unlock Skill] ──► [Damage+] ──► [Damage++] ──► [Special Upgrade]
                       │
                       └──► [Cooldown-] ──► [Cooldown--]
```

---

## Visual Feedback

### Damage Numbers

Floating damage numbers appear when asteroids take damage:

| Event | Style |
|-------|-------|
| Normal hit | White, small |
| Critical hit | Yellow, large, with "CRIT!" text |
| DoT tick | Orange, small, italicized |
| Skill damage | Skill-colored (blue/purple/etc.), medium |

**Behavior:**
- Numbers float upward and fade out over 0.5-1 second
- Slight random horizontal offset to prevent stacking
- Critical hits have a slight scale-up animation (pop effect)
- Large damage numbers for big hits (Mega asteroids, skill bursts)

### Particle Effects

| Event | Particle Effect |
|-------|-----------------|
| Normal damage | Small sparks at impact point |
| Critical hit | Larger burst with screen shake (subtle) |
| DoT burning | Ember particles trailing from asteroid |
| Asteroid destroyed | Explosion into collectible resource particles |
| Resource collected | Particles fly to ship with trail |
| Skill activation | Unique effect per skill (beam, lightning, pulse ring, glow) |
| Level up | Celebratory burst around ship |

### Skill Visual Effects

| Skill | Visual |
|-------|--------|
| Laser Burst | Bright beam with glow, fades quickly |
| Chain Lightning Bomb | Projectile arc → electric chains jumping between targets |
| EMP Pulse | Expanding ring from circle position |
| Overcharge | Circle glows brighter, pulsing aura, speed lines |

### Screen Feedback

- **Subtle screen shake** on critical hits and skill impacts
- **Brief flash** when collecting valuable resources (gold, titanium)
- **Edge glow** warning when timer is low (last 10 seconds)

### Tech Tree Mechanics

- Upgrades are arranged in a tree structure
- Purchasing an upgrade reveals connected upgrades
- Some upgrades may have prerequisites
- Designed to allow future prestige/rebirth system integration

---

## Session Flow

```
┌─────────────────────────────────────────────────────────┐
│                    GAME START                           │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                    RUN BEGINS                           │
│  • Timer starts                                         │
│  • Asteroids spawn and drift down                       │
│  • Player mines with circle                             │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                    RUN ENDS                             │
│  • Timer expires                                        │
│  • Results screen: credits earned this run              │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                   UPGRADE SCREEN                        │
│  • Tech tree displayed                                  │
│  • Player purchases upgrades                            │
│  • Level advance available if threshold met             │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
                    [Start New Run]
```

---

## Idle Mechanics

- **Active Play Only** — Game pauses when player is away
- No offline progression in current design
- Future consideration: Auto-miner upgrades for idle income

---

## Future Considerations

These features are not in scope for v1.0 but the design should accommodate them:

- **Prestige System** — Reset progress for permanent multipliers
- **Additional Special Asteroids** — Radioactive (risk/reward), Golden (rare bonus), etc.
- **Ship Upgrades** — Visual customization, collection radius
- **Achievements** — Milestones for bonus rewards
- **Auto-Miner** — Idle income when away

---

## Technical Notes

**Level Configuration:**
Each level should be defined in a config file (JSON/ScriptableObject) containing:
- Level number
- Drop table (resource types and spawn weights)
- Asteroid HP multipliers
- Spawn rate
- Credit threshold to unlock
- Advance cost

**Recommended Architecture:**
- Scriptable Objects for asteroid types and upgrade definitions
- Event-driven system for asteroid destruction → particle spawn → collection
- Modular upgrade system to support tech tree expansion

---

## Summary

Astrominer offers a satisfying core loop of mining, collecting, and upgrading. The timed run structure creates clear session boundaries while the tech tree provides long-term goals. Passive mining with the damage circle is complemented by active ship skills for burst damage and strategic play. Starting with simple resources and gradually introducing rarer materials gives players a sense of progression, while special asteroids add variety and excitement to each run.
