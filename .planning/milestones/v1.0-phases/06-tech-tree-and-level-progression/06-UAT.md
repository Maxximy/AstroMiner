---
status: diagnosed
phase: 06-tech-tree-and-level-progression
source: 06-01-SUMMARY.md, 06-02-SUMMARY.md, 06-03-SUMMARY.md
started: 2026-02-18T16:00:00Z
updated: 2026-02-18T16:15:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Tech Tree Display
expected: Open the upgrade screen after a run. A tech tree with nodes radiating from center should be visible, with 5 branches (Mining, Economy, Ship, Run, Progression) connected by lines.
result: pass

### 2. Node Color States
expected: Nodes you can afford appear green. Nodes too expensive appear red. The center start node should be blue (already purchased). Hidden nodes with unmet prerequisites should not be visible yet.
result: pass

### 3. Pan and Zoom Navigation
expected: Right-click drag or middle-mouse drag pans the tech tree view. Scroll wheel zooms in and out, pivoting around the mouse cursor.
result: issue
reported: "drag works, scroll is too slow, you have to scroll a lot to zoom in a tiny bit"
severity: minor

### 4. Purchase an Upgrade
expected: Click a green (affordable) node. Credits deduct from your total, the node turns blue, a purchase sound effect plays, and any newly-reachable nodes appear.
result: pass

### 5. Tooltip on Hover
expected: Hover over any visible node. A tooltip appears showing the node name, cost, description, and a stat preview showing current value vs. value after purchase.
result: issue
reported: "tooltip works, but too far away from the mouse pointer: hovering in the center shows it barly visible in the lower left corner"
severity: major

### 6. Stat Effect Applied After Purchase
expected: Purchase a mining upgrade (e.g., mining radius or damage). Start a new run. The purchased stat improvement should be noticeable compared to before (larger circle, more damage, etc.).
result: pass

### 7. Skills Locked By Default
expected: Before purchasing any Ship branch upgrades, skills 1-4 should be unavailable (locked in the skill bar). After purchasing a Ship branch skill unlock node, that skill becomes usable.
result: pass

### 8. Resource Tier Mineral Colors
expected: At Level 1, minerals are grey (Iron). If you advance to a higher level, minerals should appear in multiple colors based on resource tier (grey, orange, white, blue, yellow, purple depending on level).
result: pass

### 9. Asteroid HP and Size Scaling
expected: At higher levels, asteroids have more HP (take longer to destroy) and appear visually larger than Level 1 asteroids.
result: pass

### 10. Level Advancement
expected: In the Progression branch of the tech tree, purchasing the "Advance Level" node (when you meet the credit threshold) advances you to the next level. The next run should use the new level's config.
result: pass

### 11. Save Persistence
expected: Purchase some upgrades and advance a level. Close and reopen the game. Your purchased upgrades, credits, and current level should all be preserved.
result: pass

## Summary

total: 11
passed: 9
issues: 2
pending: 0
skipped: 0

## Gaps

- truth: "Scroll wheel zooms in and out smoothly with reasonable sensitivity"
  status: failed
  reason: "User reported: drag works, scroll is too slow, you have to scroll a lot to zoom in a tiny bit"
  severity: minor
  test: 3
  root_cause: "ZoomSpeed=0.1f combined with *0.01f multiplier gives only 0.001 zoom per scroll unit (120*0.001=0.12 per notch). Need ~14 scroll events to traverse full range."
  artifacts:
    - path: "Assets/Scripts/MonoBehaviours/UI/TechTreeController.cs"
      issue: "ZoomSpeed constant too low at 0.1f with additional 0.01f multiplier on line 643"
  missing:
    - "Increase ZoomSpeed from 0.1f to 1.0f for responsive scroll zoom"

- truth: "Tooltip appears near the hovered node, following the mouse pointer"
  status: failed
  reason: "User reported: tooltip works, but too far away from the mouse pointer: hovering in the center shows it barly visible in the lower left corner"
  severity: major
  test: 5
  root_cause: "Anchor/pivot mismatch: anchors at (0,0) but pivot at (0,1). Clamping logic uses halfW/halfH assuming center-origin but anchors are bottom-left. Bottom clamp triggers when centered (localPos.y≈0, 0-tooltipHeight < -halfH) pushing tooltip to lower left."
  artifacts:
    - path: "Assets/Scripts/MonoBehaviours/UI/TechTreeTooltip.cs"
      issue: "anchorMin/Max set to (0,0) with pivot (0,1); clamping logic assumes center-origin coordinates"
  missing:
    - "Change anchors to (0.5, 0.5) center-anchored and fix clamping logic to match"
