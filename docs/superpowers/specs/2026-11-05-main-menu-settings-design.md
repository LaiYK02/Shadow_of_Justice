# Main Menu Settings Design

## Intent

Make the authored main-menu settings panel functional and make its values apply immediately throughout the game.

## Goals

- Settings opens the existing panel and hides the main-menu buttons; Back reverses that state.
- Gameplay and Audio tabs switch the existing subpanels.
- Mouse X/Y sensitivity and Master/BGM/SFX volume sliders update live and persist across scene loads and game launches.
- A mixer asset in `Assets/Audio` exposes Master, BGM, and SFX volume controls.

## Non-goals

- Adding new settings controls or redesigning the authored panel.
- Adding or changing music and sound playback. Existing audio sources must be routed to the new mixer groups for their sound categories to be affected.

## Constraints and Existing State

- The project uses uGUI and already contains the menu, settings panel, tabs, sliders, and Back button.
- `MainMenuManager.OpenSettings()` is a stub.
- `ThirdPersonCameraLook` currently has one serialized sensitivity value (0.12).
- Audio clips exist in `Assets/Audio`, but the inspected scenes contain no `AudioSource` components or mixer routing.
- Settings are required across scenes and should apply immediately.

## Approaches

1. Add settings behavior directly to `MainMenuManager` and save values with `PlayerPrefs`. This is the smallest change but couples view navigation, preference storage, and mixer application.
2. Add a persistent `GameSettingsService` for saved values and mixer control, and a focused `SettingsPanelController` for menu navigation and slider bindings. This adds two small components but keeps menu UI separate from game-wide settings and supports later settings screens.

## Chosen Approach

Use approach 2. The user approved the persistent service approach. `PlayerPrefs` stores the two sensitivities and three normalized volume values. The persistent service applies volume values to exposed mixer parameters; the camera reads the saved X/Y values. The settings panel controller discovers the existing controls by their current unique hierarchy names and switches the existing panels.

## Implementation Calls

- Keep the current two gameplay sensitivity sliders as the complete Gameplay tab for this change.
- Use `Assets/Audio/GameAudioMixer.mixer` with Master, BGM, and SFX groups and exposed volume parameters named `MasterVolume`, `BGMVolume`, and `SFXVolume`.
- Convert normalized volume values to decibels for mixer attenuation, mapping zero to the mixer floor.
- Route no existing AudioSources because none are present in the inspected scenes; future or subsequently added sources should use the appropriate mixer group.
- Preserve existing scene and UI layout; bind the authored controls by name at startup and attach runtime listeners.

## Open Choices for the Builder

- Use the current authored slider ranges and default values where compatible; otherwise use 0.12 for each sensitivity and 1.0 (full volume) for each audio control.
- Ensure initial visibility is one settings subpanel at a time, with the settings panel closed when the main menu starts.
