
using UnityEngine;
using UnityEngine.Audio;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private SceneLoader sceneLoader;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "CentralDistrict";

    [Header("Settings")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Main Menu Audio")]
    [SerializeField] private AudioClip mainMenuBGM1;
    [SerializeField] private AudioClip mainMenuBGM2;
    [SerializeField] private AudioClip buttonHoverClip;
    [SerializeField] private AudioClip buttonPressClip;

    private SettingsPanelController settingsPanelController;

    private void Awake()
    {
        GameSettingsService settingsService = GameSettingsService.Instance;
        if (settingsService == null)
        {
            GameObject settingsObject = new GameObject("GameSettingsService");
            settingsService = settingsObject.AddComponent<GameSettingsService>();
        }

        if (audioMixer == null)
        {
            Debug.LogError("MainMenuManager: Audio Mixer reference is missing.", this);
        }
        else
        {
            settingsService.ConfigureMixer(audioMixer);
        }

        MainMenuAudioController menuAudio = GetComponent<MainMenuAudioController>();
        if (menuAudio == null)
            menuAudio = gameObject.AddComponent<MainMenuAudioController>();

        menuAudio.Initialize(
            audioMixer,
            mainMenuBGM1,
            mainMenuBGM2,
            buttonHoverClip,
            buttonPressClip);

        settingsPanelController = GetComponent<SettingsPanelController>();
        if (settingsPanelController == null)
            settingsPanelController = gameObject.AddComponent<SettingsPanelController>();

        settingsPanelController.Initialize(settingsService);
    }

    public void StartGame()
    {
        if (sceneLoader == null)
        {
            Debug.LogError(
                "MainMenuManager: SceneLoader reference is missing."
            );
            return;
        }

        sceneLoader.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        if (settingsPanelController == null)
        {
            Debug.LogError("MainMenuManager: Settings panel controller is missing.", this);
            return;
        }

        settingsPanelController.OpenSettings();
    }

    public void ExitGame()
    {
        Debug.Log("Exiting Shadow of Justice...");

#if UNITY_EDITOR
        // Stop Play mode when testing inside Unity.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Close the standalone game.
        Application.Quit();
#endif
    }
}
