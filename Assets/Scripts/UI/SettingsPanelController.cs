using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsPanelController : MonoBehaviour
{
    private static readonly string[] MainMenuButtonNames =
    {
        "Start_Button",
        "Settings_Button",
        "Exit_Button"
    };

    private GameObject[] mainMenuButtons;
    private GameObject settingsPanel;
    private GameObject gameplaySettingsPanel;
    private GameObject audioSettingsPanel;
    private Button gameplayTabButton;
    private Button audioTabButton;
    private Button backButton;
    private Slider mouseXSensitivitySlider;
    private Slider mouseYSensitivitySlider;
    private Slider masterVolumeSlider;
    private Slider bgmVolumeSlider;
    private Slider sfxVolumeSlider;
    private GameSettingsService settingsService;
    private bool initialized;

    public void Initialize(GameSettingsService service)
    {
        if (initialized)
            return;

        settingsService = service;

        Canvas canvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            Debug.LogError("SettingsPanelController: Could not find the MainMenu Canvas.", this);
            return;
        }

        Transform canvasRoot = canvas.transform;
        mainMenuButtons = new GameObject[MainMenuButtonNames.Length];
        for (int i = 0; i < MainMenuButtonNames.Length; i++)
        {
            Transform buttonTransform = FindDescendant(canvasRoot, MainMenuButtonNames[i]);
            if (buttonTransform == null)
            {
                Debug.LogError(
                    $"SettingsPanelController: Could not find '{MainMenuButtonNames[i]}'.",
                    this);
                return;
            }

            mainMenuButtons[i] = buttonTransform.gameObject;
        }

        settingsPanel = FindObject(canvasRoot, "SettingsPanel");
        gameplaySettingsPanel = FindObject(canvasRoot, "GameplaySettingsPanel");
        audioSettingsPanel = FindObject(canvasRoot, "AudioSettingsPanel");
        gameplayTabButton = FindComponent<Button>(canvasRoot, "ButtonGameplaySettings");
        audioTabButton = FindComponent<Button>(canvasRoot, "ButtonAudioSettings");
        backButton = FindComponent<Button>(canvasRoot, "ButtonBack");
        mouseXSensitivitySlider = FindComponent<Slider>(canvasRoot, "SliderMouseXSensitivity");
        mouseYSensitivitySlider = FindComponent<Slider>(canvasRoot, "SliderMouseYSensitivity");
        masterVolumeSlider = FindComponent<Slider>(canvasRoot, "SliderMasterVolume");
        bgmVolumeSlider = FindComponent<Slider>(canvasRoot, "SliderBGMVolume");
        sfxVolumeSlider = FindComponent<Slider>(canvasRoot, "SliderSFXVolume");

        if (settingsPanel == null || gameplaySettingsPanel == null || audioSettingsPanel == null ||
            gameplayTabButton == null || audioTabButton == null || backButton == null ||
            mouseXSensitivitySlider == null || mouseYSensitivitySlider == null ||
            masterVolumeSlider == null || bgmVolumeSlider == null || sfxVolumeSlider == null)
        {
            Debug.LogError("SettingsPanelController: One or more settings UI references are missing.", this);
            return;
        }

        AddAudioFeedback(gameplayTabButton.gameObject);
        AddAudioFeedback(audioTabButton.gameObject);
        AddAudioFeedback(backButton.gameObject);
        AddAudioFeedback(mouseXSensitivitySlider.gameObject);
        AddAudioFeedback(mouseYSensitivitySlider.gameObject);
        AddAudioFeedback(masterVolumeSlider.gameObject);
        AddAudioFeedback(bgmVolumeSlider.gameObject);
        AddAudioFeedback(sfxVolumeSlider.gameObject);

        gameplayTabButton.onClick.AddListener(ShowGameplaySettings);
        audioTabButton.onClick.AddListener(ShowAudioSettings);
        backButton.onClick.AddListener(BackToMainMenu);

        mouseXSensitivitySlider.onValueChanged.AddListener(HandleMouseXSensitivityChanged);
        mouseYSensitivitySlider.onValueChanged.AddListener(HandleMouseYSensitivityChanged);
        masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
        bgmVolumeSlider.onValueChanged.AddListener(HandleBGMVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(HandleSFXVolumeChanged);

        if (settingsService != null)
        {
            mouseXSensitivitySlider.SetValueWithoutNotify(
                Mathf.Clamp(settingsService.MouseSensitivityX,
                    mouseXSensitivitySlider.minValue, mouseXSensitivitySlider.maxValue));
            mouseYSensitivitySlider.SetValueWithoutNotify(
                Mathf.Clamp(settingsService.MouseSensitivityY,
                    mouseYSensitivitySlider.minValue, mouseYSensitivitySlider.maxValue));
            masterVolumeSlider.SetValueWithoutNotify(settingsService.MasterVolume);
            bgmVolumeSlider.SetValueWithoutNotify(settingsService.BGMVolume);
            sfxVolumeSlider.SetValueWithoutNotify(settingsService.SFXVolume);
        }
        else
        {
            Debug.LogError("SettingsPanelController: GameSettingsService is missing.", this);
        }

        settingsPanel.SetActive(false);
        SetMainMenuButtonsActive(true);
        SetSettingsTab(true);
        initialized = true;
    }

    public void OpenSettings()
    {
        if (!initialized)
            return;

        SetMainMenuButtonsActive(false);
        settingsPanel.SetActive(true);
        SetSettingsTab(true);
    }

    public void BackToMainMenu()
    {
        if (!initialized)
            return;

        settingsPanel.SetActive(false);
        SetMainMenuButtonsActive(true);
    }

    public void ShowGameplaySettings()
    {
        if (initialized)
            SetSettingsTab(true);
    }

    public void ShowAudioSettings()
    {
        if (initialized)
            SetSettingsTab(false);
    }

    private void SetSettingsTab(bool showGameplay)
    {
        gameplaySettingsPanel.SetActive(showGameplay);
        audioSettingsPanel.SetActive(!showGameplay);
    }

    private void SetMainMenuButtonsActive(bool active)
    {
        if (mainMenuButtons == null)
            return;

        foreach (GameObject button in mainMenuButtons)
        {
            if (button != null)
                button.SetActive(active);
        }
    }

    private void HandleMouseXSensitivityChanged(float value)
    {
        settingsService?.SetMouseSensitivityX(value);
    }

    private void HandleMouseYSensitivityChanged(float value)
    {
        settingsService?.SetMouseSensitivityY(value);
    }

    private void HandleMasterVolumeChanged(float value)
    {
        settingsService?.SetMasterVolume(value);
    }

    private void HandleBGMVolumeChanged(float value)
    {
        settingsService?.SetBGMVolume(value);
    }

    private void HandleSFXVolumeChanged(float value)
    {
        settingsService?.SetSFXVolume(value);
    }

    private static GameObject FindObject(Transform root, string objectName)
    {
        Transform found = FindDescendant(root, objectName);
        return found != null ? found.gameObject : null;
    }

    private static T FindComponent<T>(Transform root, string objectName) where T : Component
    {
        Transform found = FindDescendant(root, objectName);
        return found != null ? found.GetComponent<T>() : null;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendant(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void AddAudioFeedback(GameObject target)
    {
        if (target.GetComponent<MenuUIAudioFeedback>() == null)
            target.AddComponent<MenuUIAudioFeedback>();
    }
}
