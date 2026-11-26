using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private PlayerHUDController playerHud;
    [SerializeField] private PlayerInteractionManager interaction;
    [SerializeField] private TutorialPanelController tutorial;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private GameObject overlay;
    [SerializeField] private GameObject menu;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private SettingsPanelController settings;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    private bool isPaused;
    private bool resumeRequested;
    private float previousTimeScale;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisibility;
    private bool interactionWasEnabled;

    private void Awake()
    {
        settings.Initialize(GameSettingsService.GetOrCreate(), settingsPanel, ShowPauseButtons);

        resumeButton.onClick.AddListener(RequestResume);
        settingsButton.onClick.AddListener(OpenSettings);
        mainMenuButton.onClick.AddListener(BackToMainMenu);
        quitButton.onClick.AddListener(Quit);
        overlay.SetActive(false);
    }

    private void Start() => GameSettingsService.Instance.ConfigureMixer(audioMixer);

    private void Update()
    {
        if (!EscapePressed())
            return;

        if (!isPaused)
            Pause();
        else if (settings.IsOpen)
            settings.CloseSettings();
        else
            RequestResume();
    }

    private void LateUpdate()
    {
        // Restoring gameplay after input updates prevents a Resume click from triggering melee.
        if (resumeRequested)
            RestoreGameplay();
    }

    private void OnDisable() => RestoreGameplay();

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && isPaused)
            ShowCursor();
    }

    private void Pause()
    {
        if (Time.timeScale <= 0f || (playerHud != null && !playerHud.IsVisible))
            return;

        isPaused = true;
        previousTimeScale = Time.timeScale;
        previousCursorLock = Cursor.lockState;
        previousCursorVisibility = Cursor.visible;
        Time.timeScale = 0f;

        if (interaction != null)
        {
            interactionWasEnabled = interaction.enabled;
            interaction.enabled = false;
        }
        if (playerHud != null)
            playerHud.SetPauseMenuActive(true);
        if (tutorial != null)
            tutorial.SetPauseMenuActive(true);

        ShowCursor();
        overlay.SetActive(true);
        menu.SetActive(true);
        resumeButton.Select();
    }

    private void RequestResume() => resumeRequested = isPaused;

    private void RestoreGameplay()
    {
        if (!isPaused)
            return;

        isPaused = false;
        resumeRequested = false;
        settings.CloseSettings();
        overlay.SetActive(false);
        Time.timeScale = previousTimeScale;
        if (interaction != null)
            interaction.enabled = interactionWasEnabled;
        if (playerHud != null)
            playerHud.SetPauseMenuActive(false);
        if (tutorial != null)
            tutorial.SetPauseMenuActive(false);
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisibility;
    }

    private void OpenSettings()
    {
        settings.OpenSettings();
        if (settings.IsOpen)
            menu.SetActive(false);
    }

    private void ShowPauseButtons()
    {
        if (!isPaused)
            return;

        menu.SetActive(true);
        settingsButton.Select();
    }

    private void BackToMainMenu()
    {
        const string mainMenuScene = "MainMenu";
        if (!Application.CanStreamedLevelBeLoaded(mainMenuScene))
        {
            Debug.LogError("Pause menu: MainMenu is missing from the active build profile.", this);
            return;
        }

        RestoreGameplay();
        Time.timeScale = 1f;
        MainMenuIntro.SkipNextIntro = true;
        SceneManager.LoadScene(mainMenuScene);
    }

    private void Quit()
    {
        RestoreGameplay();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            return Keyboard.current.escapeKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }
}
