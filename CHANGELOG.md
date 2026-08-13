# Changelog

All notable changes to FrogSkill are documented here.

## Unreleased

## 1.0.0 - 2026-08-13

- Added a configurable frog-tongue skill for living Scouts.
- Added support for pulling living Scouts, transformed Zombie Scouts, and NPC Mushroom Zombies.
- Matched the current Frog prefab's distance curve, maximum lift distance, force formula, and release ordering.
- Added a reduced default lift force to account for the Scout mouth anchor and a 0.5-second cooldown.
- Added strict Rescue Claw-style crosshair raycast targeting with physical obstruction handling.
- Added a short synchronized tongue animation when no valid target is hit.
- Added multiplayer synchronization for firing, pulling, releasing, and tongue visuals.
- Added ModConfig-compatible in-game key rebinding and configurable pull behavior.
