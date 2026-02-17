---
status: complete
phase: 03-collection-economy-and-session
source: [03-01-SUMMARY.md, 03-02-SUMMARY.md, 03-03-SUMMARY.md]
started: 2026-02-17T22:00:00Z
updated: 2026-02-17T22:15:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Mineral Spawn on Asteroid Death
expected: When an asteroid's HP reaches 0 from mining, small gold/amber spheres spawn at its position (3-8 per asteroid). They should be visible immediately.
result: pass

### 2. Minerals Fly to Ship and Collect
expected: The gold spheres accelerate toward the ship at the bottom of the screen. When they reach the ship, they disappear (collected).
result: pass

### 3. Credits Increment on Collection
expected: The HUD in the top area shows a running credit total. As minerals are collected, the number goes up. Large numbers should show K/M/B/T suffixes.
result: pass

### 4. HUD Timer Countdown
expected: A countdown timer is visible during gameplay showing MM:SS format, counting down from the run duration.
result: pass

### 5. Collecting Phase Transition
expected: When the timer hits 0:00, you can no longer mine asteroids (they stop taking damage). Remaining minerals still fly to the ship and get collected.
result: pass

### 6. Results Screen
expected: After all minerals and asteroids are cleared, a dark overlay results screen appears showing "Run Complete!" and the credits earned this run. A "Continue" button is visible.
result: pass

### 7. Upgrade Screen and New Run
expected: Clicking Continue leads to an upgrade screen with "Upgrades" title, total credits displayed, placeholder text about Phase 6, and a "Start Run" button. Clicking Start Run begins a fresh run with timer reset and no leftover asteroids/minerals.
result: pass

### 8. Credits Persist Across Runs
expected: After completing a run and starting a new one, the credits from the previous run are still there (the total keeps growing). The results screen shows only the delta for the current run.
result: pass

### 9. Cross-Session Save Persistence
expected: After completing a run, stop Play mode in Unity. Press Play again. Your credit total from the previous session should be restored (not reset to 0).
result: pass

## Summary

total: 9
passed: 9
issues: 0
pending: 0
skipped: 0

## Gaps

[none]
