using UnityEngine;

/// <summary>
/// Manages background music for the whole game.
/// Attach to a GameObject in your bootstrap/main scene.
/// Drag Assets/Audio/BackgroundMusic.mp3 into the musicClip field in the Inspector.
/// </summary>
public class AudioManager : SingletonMonoBehaviour<AudioManager>
{
    [Header("Background Music")]
    [SerializeField] private AudioClip musicClip;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;

    private AudioSource _musicSource;

    protected override void Awake()
    {
        base.Awake();

        // base.Awake() destroys duplicates; only continue for the kept instance
        if (this != Instance)
            return;

        EnsureMusicSource();
        PlayMusic();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public float MusicVolume
    {
        get => _musicSource != null ? _musicSource.volume : musicVolume;
        set
        {
            musicVolume = Mathf.Clamp01(value);
            if (_musicSource != null)
                _musicSource.volume = musicVolume;
        }
    }

    public bool IsMusicPlaying => _musicSource != null && _musicSource.isPlaying;

    public void SetMusicMuted(bool muted)
    {
        if (_musicSource != null)
            _musicSource.mute = muted;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void EnsureMusicSource()
    {
        _musicSource = GetComponent<AudioSource>();
        if (_musicSource == null)
            _musicSource = gameObject.AddComponent<AudioSource>();

        _musicSource.clip        = musicClip;
        _musicSource.volume      = musicVolume;
        _musicSource.loop        = true;
        _musicSource.playOnAwake = false;
        _musicSource.spatialBlend = 0f;  // 2D audio
    }

    private void PlayMusic()
    {
        if (_musicSource == null || _musicSource.clip == null)
        {
            Debug.LogWarning("[AudioManager] No music clip assigned. Drag BackgroundMusic.mp3 into the musicClip field.");
            return;
        }

        if (!_musicSource.isPlaying)
            _musicSource.Play();
    }
}
