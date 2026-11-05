# Player Animations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Drive the existing Y Bot Idle, Walking, Running, and Standing Melee Attack animations from the player's existing movement, sprint, attack, and dialogue-control state.

**Architecture:** Add three bool parameters and transitions to the existing Animator Controller. `PlayerMovement` finds the model Animator and updates those parameters using its existing input and attack state; the CharacterController continues to own movement and root motion remains disabled.

**Tech Stack:** Unity 6 Animator Controller, C#, CharacterController.

**Spec:** `docs/superpowers/specs/2026-11-05-player-animations-design.md`

## Global Constraints

- Use only the four existing Y Bot animation clips and states.
- Keep root motion disabled; preserve the existing movement, sprint energy, attack hitbox, attack cost, and attack duration behavior.
- Keep the camera separate from player rotation and do not change camera behavior.
- Use the Unity Pipeline package only for live Editor authoring; it does not add runtime animation dependencies.

## Review Focus

- Player Animator missing or inactive: movement must remain usable and the missing Animator should be reported without a null-reference failure.
- Sprint key held with depleted energy: show Walking, not Running, because effective sprint is false.
- Attack denied for insufficient energy or missing hitbox: do not enter the attack animation.
- Attack completes while movement or sprint input remains held: return to the matching locomotion state.
- Dialogue starts during an attack: clear the attack state and leave the model in Idle while controls are locked.

---

### Task 1: Configure the existing Y Bot Animator Controller

**Files:**
- Modify: `Assets/Objects/Player/Y Bot Animator.controller`

**Interfaces:**
- Produces bool parameters `IsMoving`, `IsSprinting`, and `IsAttacking`, consumed by `PlayerMovement`.

- [x] Add the three bool parameters and transitions among the existing Idle, Walking, Running, and Standing Melee Attack states.
- [x] Configure locomotion transitions: Idle to Walking on `IsMoving && !IsSprinting`; Idle to Running on `IsSprinting`; Walking to Idle on `!IsMoving`; Walking to Running on `IsSprinting`; Running to Idle on `!IsMoving`; Running to Walking on `IsMoving && !IsSprinting`.
- [x] Add an `IsAttacking` transition from each locomotion state to Standing Melee Attack. Return from attack to Idle on `!IsAttacking && !IsMoving`, Walking on `!IsAttacking && IsMoving && !IsSprinting`, or Running on `!IsAttacking && IsSprinting`. Use short 0.1-second blends with exit time disabled so the existing one-second attack timing controls the return.
- [x] Set Standing Melee Attack state speed to `2.266667` so its `2.266667`-second clip completes within the existing one-second attack duration.
- [x] Keep Idle as the default state and preserve the existing animation clips and root-motion setting.

### Task 2: Drive Animator parameters from PlayerMovement

**Files:**
- Modify: `Assets/Scripts/Player/PlayerMovement.cs`

**Interfaces:**
- Consumes the three Animator bool parameters from Task 1.
- Produces movement, sprint, attack, and dialogue updates for the child Y Bot Animator.

- [x] Find and cache the child Animator, including inactive children, and handle a missing Animator without preventing player controls.
- [x] Set `IsMoving` from movement input and `IsSprinting` from the effective sprint state after the current energy check.
- [x] Set `IsAttacking` only after `TryStartAttack` accepts the attack; clear all three parameters when dialogue control begins and keep the model in Idle while controls are locked.
- [x] Keep the existing CharacterController motion and attack hitbox timing unchanged.

### Task 3: Review the resulting changes

**Files:**
- Review: `Assets/Objects/Player/Y Bot Animator.controller`
- Review: `Assets/Scripts/Player/PlayerMovement.cs`
- Review: `Packages/manifest.json` and `Packages/packages-lock.json` (live Editor tooling dependency)

- [x] Confirm the controller parameter names match the script and all four states have the intended transitions.
- [x] Confirm gameplay changes are limited to the Animator controller and PlayerMovement; Pipeline is editor tooling only. Do not run tests unless requested.
