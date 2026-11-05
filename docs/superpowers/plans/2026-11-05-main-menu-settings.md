# Main Menu Settings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the existing main-menu settings screen navigate correctly and apply saved gameplay sensitivity and mixer volume settings throughout the game.

**Architecture:** A persistent `GameSettingsService` stores sensitivity and normalized volume values in `PlayerPrefs`, applies volume to exposed AudioMixer parameters, and survives scene changes. A focused `SettingsPanelController` binds the authored uGUI controls, while `ThirdPersonCameraLook` reads the service’s X/Y sensitivity values.

**Tech Stack:** Unity 6.3, C#, uGUI, TextMeshPro where already authored, AudioMixer, PlayerPrefs.

**Spec:** `docs/superpowers/specs/2026-11-05-main-menu-settings-design.md`

## Global Constraints

- Preserve the existing authored MainMenu UI layout and use uGUI.
- Settings changes apply immediately and persist across launches.
- Mixer asset path: `Assets/Audio/GameAudioMixer.mixer`.
- Keep Gameplay to its existing Mouse X and Mouse Y sliders.
- Do not create tests or modify unrelated project files.

## Review Focus

- Missing service or mixer references should log a useful error and leave the menu functional.
- Existing PlayerPrefs values outside slider bounds should be clamped before display/application.
- A zero volume slider must map to a finite low mixer level instead of negative infinity.
- Settings should persist across the MainMenu-to-CentralDistrict scene transition and subsequent launches.
- The settings screen should initialize closed with exactly one tab content panel active after opening.

---

### Task 1: Create the AudioMixer asset

**Files:**
- Create: `Assets/Audio/GameAudioMixer.mixer`
- Create: `Assets/Audio/GameAudioMixer.mixer.meta` (Unity-generated)
- Temporary editor helper: create and remove after asset generation under `Assets/Editor/`

**Interfaces:**
- Produces exposed mixer parameter names `MasterVolume`, `BGMVolume`, and `SFXVolume`.
- Produces Master, BGM, and SFX groups, with BGM and SFX routed through Master.

- [x] Create the mixer asset through Unity Editor APIs, expose the attenuation value for each group with the exact names above, and save it under `Assets/Audio`.
- [x] Inspect the generated asset to confirm all three exposed parameters and the Master/BGM/SFX group hierarchy exist.

### Task 2: Add persistent game settings service

**Files:**
- Create: `Assets/Scripts/Managers/GameSettingsService.cs`
- Modify: `Assets/Scripts/Managers/MainMenuManager.cs` to create/configure the service on menu startup.
- Modify: `Assets/Scenes/MainMenu.unity` to assign the mixer asset to `MainMenuManager`.

**Interfaces:**
- `public static GameSettingsService Instance { get; }`
- `public float MouseSensitivityX { get; }`
- `public float MouseSensitivityY { get; }`
- `public void SetMouseSensitivityX(float value)`
- `public void SetMouseSensitivityY(float value)`
- `public void SetMasterVolume(float value)`
- `public void SetBGMVolume(float value)`
- `public void SetSFXVolume(float value)`
- Consumes the exposed mixer parameter names from Task 1.

- [x] Implement singleton persistence with `DontDestroyOnLoad`, duplicate protection, and `PlayerPrefs` keys for the two sensitivities and three volume values.
- [x] Initialize absent preferences to sensitivity `0.12` and volume `1.0`; clamp values to their UI-supported ranges.
- [x] Apply saved volume settings at startup and each setter call, mapping normalized `0..1` values to finite mixer decibels with zero at the mixer floor.
- [x] Have `MainMenuManager` create a dedicated service GameObject at runtime, assign the mixer, and let only that service persist across scenes.

### Task 3: Wire settings navigation and slider controls

**Files:**
- Create: `Assets/Scripts/UI/SettingsPanelController.cs`
- Modify: `Assets/Scripts/Managers/MainMenuManager.cs`
- Modify: `Assets/Scenes/MainMenu.unity`

**Interfaces:**
- `SettingsPanelController.OpenSettings()` hides the Start, Settings, and Exit buttons and opens the settings panel.
- `SettingsPanelController.BackToMainMenu()` closes settings and restores those buttons.
- `SettingsPanelController.ShowGameplaySettings()` and `ShowAudioSettings()` show only their respective child panel.
- `MainMenuManager.OpenSettings()` delegates to the panel controller while preserving its existing button binding.

- [x] Discover the existing controls by their current unique hierarchy names and bind runtime button/slider listeners, keeping the authored UI scene hierarchy intact.
- [x] Initialize with the settings panel closed and main-menu buttons visible; opening the panel selects Gameplay as the initial tab.
- [x] Initialize sliders from `GameSettingsService`, update them without firing callbacks, then send changes to the service immediately.
- [x] Confirm the named controls and button/slider components exist and the existing EventSystem remains the only one.

### Task 4: Apply separate sensitivity values in gameplay

**Files:**
- Modify: `Assets/Scripts/Player/ThirdPersonCameraLook.cs`

**Interfaces:**
- Horizontal mouse delta uses `GameSettingsService.MouseSensitivityX`.
- Vertical mouse delta uses `GameSettingsService.MouseSensitivityY`.
- Existing serialized `sensitivity` value `0.12` remains the fallback when no service is available.

- [x] Replace the shared sensitivity multiplication with independent horizontal and vertical values, preserving inversion, pitch limits, cursor handling, and camera/player rotation behavior.
- [x] Inspect CentralDistrict scene references to confirm its camera look component remains enabled and no inspector references were lost.

### Task 5: Verify Unity import and integration

**Files:**
- No new files.

- [x] Generate/import the mixer in a Unity 6.3.17f1 batch project and compile the game assembly using a temporary MSBuild target that includes the two new scripts; the MainMenu project was already open in Unity and had no Pipeline live-control package.
- [x] Inspect the changed scene and mixer asset references, confirming the mixer GUID and all three exposed parameter names resolve.
- [x] Review the settings-specific scripts and references; preserve unrelated existing in-progress scene and asset changes.
