using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public sealed class MainMenuAudioController : MonoBehaviour
{
    private const string BGMGroupPath = "Master/BGM";
    private const string SFXGroupPath = "Master/SFX";

    public static MainMenuAudioController Instance { get; private set; }

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip[] musicPlaylist;
    private AudioClip hoverClip;
    private AudioClip pressClip;
    private bool initialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void Initialize(
        AudioMixer mixer,
        AudioClip firstTrack,
        AudioClip secondTrack,
        AudioClip buttonHover,
        AudioClip buttonPress)
    {
        if (initialized)
            return;

        initialized = true;
        musicPlaylist = new[] { firstTrack, secondTrack };
        hoverClip = buttonHover;
        pressClip = buttonPress;

        AudioMixerGroup bgmGroup = FindGroup(mixer, BGMGroupPath);
        AudioMixerGroup sfxGroup = FindGroup(mixer, SFXGroupPath);

        musicSource = CreateSource(bgmGroup);
        sfxSource = CreateSource(sfxGroup);

        if (firstTrack == null || secondTrack == null)
        {
            Debug.LogError("MainMenuAudioController: Both menu music clips must be assigned.", this);
            return;
        }

        StartCoroutine(PlayMusicPlaylist());
    }

    public void PlayHover()
    {
        PlayOneShot(hoverClip);
    }

    public void PlayPress()
    {
        PlayOneShot(pressClip);
    }

    private IEnumerator PlayMusicPlaylist()
    {
        int trackIndex = 0;
        while (true)
        {
            AudioClip track = musicPlaylist[trackIndex];
            musicSource.clip = track;
            musicSource.Play();

            // Wait for the current song to finish before advancing to the next track.
            while (musicSource.isPlaying)
                yield return null;

            trackIndex = (trackIndex + 1) % musicPlaylist.Length;
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (initialized && clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    private AudioSource CreateSource(AudioMixerGroup outputGroup)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = outputGroup;
        return source;
    }

    private static AudioMixerGroup FindGroup(AudioMixer mixer, string groupPath)
    {
        if (mixer == null)
            return null;

        AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupPath);
        if (groups.Length == 0)
        {
            Debug.LogError($"MainMenuAudioController: Mixer group '{groupPath}' was not found.");
            return null;
        }

        return groups[0];
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
