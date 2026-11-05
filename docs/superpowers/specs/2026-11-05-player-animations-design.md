# Player animation integration design

Date: 2026-11-05

## Intent

Play the existing Y Bot animations in step with the player's movement, sprint, and attack actions so the character visibly responds to the controls.

## Goals

- Play Idle when the player is not moving.
- Play Walking while moving normally and Running while sprinting is actually active, including the existing energy requirement.
- Play Standing Melee Attack during the existing one-second attack action, then return to the locomotion state matching the current input.
- Keep movement, facing direction, attack hit detection, and animation on the model while leaving the camera behavior unchanged.

## Non-goals

- Add new animations, jump behavior, or animation-driven root motion.
- Change movement speeds, sprint energy values, attack cost/duration, or dialogue and cutscene flow.

## Constraints and current state

- `Y Bot Animator.controller` already contains Idle, Walking, Running, and Standing Melee Attack, but has no parameters or transitions.
- The Animator is on the Y Bot model under the Player object. `PlayerMovement` is on the parent and already owns movement, sprint, attack, and dialogue lock state.
- Root motion is disabled on the model, so the `CharacterController` remains responsible for player movement.

## Approaches

- **Animator parameters and transitions (selected):** add `IsMoving`, `IsSprinting`, and `IsAttacking` parameters and configure the four existing states to transition from those values. `PlayerMovement` updates the parameters from its existing state. This keeps the flow visible and adjustable in Unity's Animator window.
- **Code-driven state changes:** have `PlayerMovement` call Animator state changes directly. This requires less controller configuration but makes transition timing and blending harder to inspect and tune in the Animator window.

The user selected the Animator parameters and transitions approach.

## Implementation calls

- Find the child Animator from `PlayerMovement` if it is not explicitly assigned.
- Use the current movement input and effective sprint state (after checking energy) to set locomotion parameters.
- Set `IsAttacking` for the existing attack routine; transition back to idle, walking, or running based on the current locomotion parameters when the attack ends.
- Clear attack and locomotion parameters when dialogue locks player control so a stale attack or movement state does not continue through a cutscene.
- Use short transition blends, disable root motion, and keep the authored state names and animation clips.
- The authored attack clip is 2.266667 seconds while the existing attack lock is 1 second; set the attack state's playback speed to 2.266667 so its full motion fits the existing gameplay timing without changing the hitbox duration.

## Review boundary

This document records the selected approach. The user should approve it before the Animator controller or player scripts are changed.
