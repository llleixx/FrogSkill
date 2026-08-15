# Changelog

All notable changes to FrogSkill are documented here.

## Unreleased

## 1.3.0 - 2026-08-15

- Added configurable crosshair forgiveness for near-miss tongue shots, shared by Custom and Vanilla modes, with a 3-degree default and a range up to 10 degrees.
- Kept direct crosshair hits as the highest priority and required assisted targets to have an unobstructed line of sight.

## 1.2.0 - 2026-08-14

- Changed the action key to re-grab toward the current aim direction after releasing an active target.
- Kept the current pull intact when re-grab is pressed during cooldown.
- Changed missed shots to play their tongue animation without triggering cooldown.

## 1.1.1 - 2026-08-14

- Updated the required BepInExPack_PEAK dependency to 5.4.75301.

## 1.1.0 - 2026-08-14

- Added a Vanilla tongue mode that uses PEAK's original Frog network object so unmodded clients can see successful hits.
- Added a `C` mode switch while keeping `G` as the shared fire and release key.
- Added shared cooldown and active-tongue coordination between both modes.
- Kept Custom mode as the default and preserved the original FrogSkill configuration.
- Marked the standalone FrogSkillVanilla plugin as incompatible to prevent duplicate controls.

## 1.0.0 - 2026-08-13

- Added a configurable frog-tongue skill for living Scouts.
- Added support for pulling living Scouts, transformed Zombie Scouts, and NPC Mushroom Zombies.
- Matched the current Frog prefab's distance curve, maximum lift distance, force formula, and release ordering.
- Added a reduced default lift force to account for the Scout mouth anchor and a 0.5-second cooldown.
- Added strict Rescue Claw-style crosshair raycast targeting with physical obstruction handling.
- Added a short synchronized tongue animation when no valid target is hit.
- Added multiplayer synchronization for firing, pulling, releasing, and tongue visuals.
- Added ModConfig-compatible in-game key rebinding and configurable pull behavior.
