using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-100)]
public sealed class GameSettingsService : MonoBehaviour
{
    private const string MouseXSensitivityKey = "Settings.MouseXSensitivity";
    private const string MouseYSensitivityKey = "Settings.MouseYSensitivity";
    private const string MasterVolumeKey = "Settings.MasterVolume";
    private const string BGMVolumeKey = "Settings.BGMVolume";
    private const string SFXVolumeKey = "Settings.SFXVolume";

    private const float DefaultSensitivity = 0.12f;
    private const float DefaultVolume = 1f;
    private const float MixerFloorDecibels = -80f;
    private const float PreferenceSaveDelay = 0.5f;

    private const string MasterVolumeParameter = "MasterVolume";
    private const string BGMVolumeParameter = "BGMVolume";
    private const string SFXVolumeParameter = "SFXVolume";

    private AudioMixer audioMixer;

    public static GameSettingsService Instance { get; private set; }

    public float MouseSensitivityX { get; private set; }
    public float MouseSensitivityY { get; private set; }
    public float MasterVolume { get; private set; }
    public float BGMVolume { get; private set; }
    public float SFXVolume { get; private set; }

    private bool hasStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadPreferences();
    }

    private void Start()
    {
        hasStarted = true;
        ApplyAudioSettings();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            PlayerPrefs.Save();
    }

    public void ConfigureMixer(AudioMixer mixer)
    {
        audioMixer = mixer;

        if (hasStarted)
            ApplyAudioSettings();
    }

    public void SetMouseSensitivityX(float value)
    {
        MouseSensitivityX = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MouseXSensitivityKey, MouseSensitivityX);
        SchedulePreferenceSave();
    }

    public void SetMouseSensitivityY(float value)
    {
        MouseSensitivityY = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MouseYSensitivityKey, MouseSensitivityY);
        SchedulePreferenceSave();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
        ApplyVolume(MasterVolumeParameter, MasterVolume);
        SchedulePreferenceSave();
    }

    public void SetBGMVolume(float value)
    {
        BGMVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(BGMVolumeKey, BGMVolume);
        ApplyVolume(BGMVolumeParameter, BGMVolume);
        SchedulePreferenceSave();
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SFXVolumeKey, SFXVolume);
        ApplyVolume(SFXVolumeParameter, SFXVolume);
        SchedulePreferenceSave();
    }

    private void LoadPreferences()
    {
        MouseSensitivityX = Mathf.Clamp01(
            PlayerPrefs.GetFloat(MouseXSensitivityKey, DefaultSensitivity));
        MouseSensitivityY = Mathf.Clamp01(
            PlayerPrefs.GetFloat(MouseYSensitivityKey, DefaultSensitivity));
        MasterVolume = Mathf.Clamp01(
            PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume));
        BGMVolume = Mathf.Clamp01(
            PlayerPrefs.GetFloat(BGMVolumeKey, DefaultVolume));
        SFXVolume = Mathf.Clamp01(
            PlayerPrefs.GetFloat(SFXVolumeKey, DefaultVolume));
    }

    private void ApplyAudioSettings()
    {
        if (audioMixer == null)
            return;

        ApplyVolume(MasterVolumeParameter, MasterVolume);
        ApplyVolume(BGMVolumeParameter, BGMVolume);
        ApplyVolume(SFXVolumeParameter, SFXVolume);
    }

    private void ApplyVolume(string parameterName, float normalizedVolume)
    {
        if (audioMixer == null)
            return;

        float decibels = normalizedVolume <= 0f
            ? MixerFloorDecibels
            : Mathf.Max(MixerFloorDecibels, 20f * Mathf.Log10(normalizedVolume));

        if (!audioMixer.SetFloat(parameterName, decibels))
        {
            Debug.LogError(
                $"GameSettingsService: AudioMixer parameter '{parameterName}' is missing or not exposed.",
                this);
        }
    }

    private void SchedulePreferenceSave()
    {
        CancelInvoke(nameof(SavePreferences));
        Invoke(nameof(SavePreferences), PreferenceSaveDelay);
    }

    private void SavePreferences()
    {
        PlayerPrefs.Save();
    }
}
