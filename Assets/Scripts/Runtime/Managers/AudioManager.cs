using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Centralizes background music, sound effects, and player audio preferences.
/// Add clips to the library in the Inspector, then play them by their unique id.
/// </summary>
public class AudioManager : SingletonMonoBehaviour<AudioManager>
{
    private const string MasterVolumeKey = "Audio.MasterVolume";
    private const string MusicVolumeKey = "Audio.MusicVolume";
    private const string SfxVolumeKey = "Audio.SfxVolume";
    private const string MutedKey = "Audio.Muted";

    [Serializable]
    public class SoundDefinition
    {
        [Tooltip("Unique id used in code, for example: ButtonClick or LevelWin.")]
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop;
    }

    [Header("Library")]
    [SerializeField] private SoundDefinition[] music = Array.Empty<SoundDefinition>();
    [SerializeField] private SoundDefinition[] soundEffects = Array.Empty<SoundDefinition>();

    [Header("Optional Audio Mixer Groups")]
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Playback")]
    [SerializeField, Min(1)] private int soundEffectPoolSize = 8;
    [SerializeField, Min(0f)] private float defaultMusicFadeDuration = 0.5f;

    private readonly Dictionary<string, SoundDefinition> _musicById = new();
    private readonly Dictionary<string, SoundDefinition> _soundEffectsById = new();
    private readonly List<AudioSource> _soundEffectSources = new();
    private readonly Dictionary<AudioSource, float> _soundEffectBaseVolumes = new();

    private AudioSource _musicSource;
    private Coroutine _musicFadeRoutine;
    private float _masterVolume = 1f;
    private float _musicVolume = 1f;
    private float _soundEffectVolume = 1f;
    private float _currentMusicBaseVolume = 1f;
    private bool _isMuted;

    public float MasterVolume => _masterVolume;
    public float MusicVolume => _musicVolume;
    public float SoundEffectVolume => _soundEffectVolume;
    public bool IsMuted => _isMuted;
    public string CurrentMusicId { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        BuildLibrary();
        CreateAudioSources();
        LoadSettings();
    }

    public void PlayMusic(string id, float fadeDuration = -1f)
    {
        if (!TryGetDefinition(_musicById, id, out SoundDefinition definition))
            return;

        if (CurrentMusicId == id && _musicSource.isPlaying)
            return;

        if (_musicFadeRoutine != null)
            StopCoroutine(_musicFadeRoutine);

        float duration = fadeDuration < 0f ? defaultMusicFadeDuration : fadeDuration;
        if (duration <= 0f || !_musicSource.isPlaying)
        {
            StartMusic(definition);
            return;
        }

        _musicFadeRoutine = StartCoroutine(CrossFadeMusic(definition, duration));
    }

    public void StopMusic(float fadeDuration = -1f)
    {
        if (_musicFadeRoutine != null)
            StopCoroutine(_musicFadeRoutine);

        float duration = fadeDuration < 0f ? defaultMusicFadeDuration : fadeDuration;
        if (duration <= 0f || !_musicSource.isPlaying)
        {
            _musicSource.Stop();
            CurrentMusicId = null;
            return;
        }

        _musicFadeRoutine = StartCoroutine(FadeOutMusic(duration));
    }

    public void PlaySoundEffect(string id)
    {
        if (!TryGetDefinition(_soundEffectsById, id, out SoundDefinition definition))
            return;

        AudioSource source = GetAvailableSoundEffectSource();
        source.clip = definition.clip;
        source.loop = definition.loop;
        source.pitch = definition.pitch;
        source.volume = GetEffectiveSoundEffectVolume(definition.volume);
        _soundEffectBaseVolumes[source] = definition.volume;
        source.Play();
    }

    public void StopAllSoundEffects()
    {
        foreach (AudioSource source in _soundEffectSources)
            source.Stop();
    }

    public void SetMasterVolume(float value) => SetVolume(ref _masterVolume, value, MasterVolumeKey);
    public void SetMusicVolume(float value) => SetVolume(ref _musicVolume, value, MusicVolumeKey);
    public void SetSoundEffectVolume(float value) => SetVolume(ref _soundEffectVolume, value, SfxVolumeKey);

    public void SetMuted(bool value)
    {
        _isMuted = value;
        AudioListener.volume = _isMuted ? 0f : _masterVolume;
        PlayerPrefs.SetInt(MutedKey, _isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleMuted() => SetMuted(!_isMuted);

    private void BuildLibrary()
    {
        AddDefinitions(music, _musicById, "music");
        AddDefinitions(soundEffects, _soundEffectsById, "sound effect");
    }

    private void CreateAudioSources()
    {
        _musicSource = CreateAudioSource("Music", musicMixerGroup);
        _musicSource.loop = true;

        for (int i = 0; i < soundEffectPoolSize; i++)
            _soundEffectSources.Add(CreateAudioSource($"SFX {i + 1}", sfxMixerGroup));
    }

    private AudioSource CreateAudioSource(string sourceName, AudioMixerGroup outputGroup)
    {
        GameObject sourceObject = new(sourceName);
        sourceObject.transform.SetParent(transform);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.outputAudioMixerGroup = outputGroup;
        return source;
    }

    private void LoadSettings()
    {
        _masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        _musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        _soundEffectVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        _isMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        ApplyVolumes();
    }

    private void SetVolume(ref float target, float value, string preferenceKey)
    {
        target = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(preferenceKey, target);
        PlayerPrefs.Save();
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        AudioListener.volume = _isMuted ? 0f : _masterVolume;
        if (_musicSource != null && _musicSource.clip != null)
            _musicSource.volume = GetEffectiveMusicVolume(_currentMusicBaseVolume);

        foreach (AudioSource source in _soundEffectSources)
        {
            if (source.isPlaying && _soundEffectBaseVolumes.TryGetValue(source, out float baseVolume))
                source.volume = GetEffectiveSoundEffectVolume(baseVolume);
        }
    }

    private void StartMusic(SoundDefinition definition)
    {
        _currentMusicBaseVolume = definition.volume;
        _musicSource.clip = definition.clip;
        _musicSource.loop = definition.loop;
        _musicSource.pitch = definition.pitch;
        _musicSource.volume = GetEffectiveMusicVolume(definition.volume);
        _musicSource.Play();
        CurrentMusicId = definition.id;
    }

    private IEnumerator CrossFadeMusic(SoundDefinition nextMusic, float duration)
    {
        float initialVolume = _musicSource.volume;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            _musicSource.volume = Mathf.Lerp(initialVolume, 0f, elapsed / duration);
            yield return null;
        }

        StartMusic(nextMusic);
        float targetVolume = _musicSource.volume;
        _musicSource.volume = 0f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            _musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / duration);
            yield return null;
        }

        _musicSource.volume = targetVolume;
        _musicFadeRoutine = null;
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        float initialVolume = _musicSource.volume;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            _musicSource.volume = Mathf.Lerp(initialVolume, 0f, elapsed / duration);
            yield return null;
        }

        _musicSource.Stop();
        CurrentMusicId = null;
        _musicFadeRoutine = null;
    }

    private AudioSource GetAvailableSoundEffectSource()
    {
        foreach (AudioSource source in _soundEffectSources)
        {
            if (!source.isPlaying)
                return source;
        }

        return _soundEffectSources[0];
    }

    private static void AddDefinitions(
        IEnumerable<SoundDefinition> definitions,
        IDictionary<string, SoundDefinition> destination,
        string category)
    {
        foreach (SoundDefinition definition in definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.id) || definition.clip == null)
                continue;

            if (destination.ContainsKey(definition.id))
            {
                Debug.LogWarning($"[AudioManager] Duplicate {category} id '{definition.id}' ignored.");
                continue;
            }

            destination.Add(definition.id, definition);
        }
    }

    private bool TryGetDefinition(
        IReadOnlyDictionary<string, SoundDefinition> definitions,
        string id,
        out SoundDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(id) && definitions.TryGetValue(id, out definition))
            return true;

        definition = null;
        Debug.LogWarning($"[AudioManager] No audio clip configured with id '{id}'.");
        return false;
    }

    private float GetEffectiveMusicVolume(float baseVolume) => baseVolume * _musicVolume;
    private float GetEffectiveSoundEffectVolume(float baseVolume) => baseVolume * _soundEffectVolume;
}
