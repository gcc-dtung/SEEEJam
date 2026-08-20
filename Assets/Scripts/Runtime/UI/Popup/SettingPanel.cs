using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingPanel : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private GameObject root;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button musicButton;
    [SerializeField] private Image musicButtonImage;
    [SerializeField] private Button soundButton;
    [SerializeField] private Image soundButtonImage;

    [Header("Toggle Colors")]
    [Tooltip("Color when the feature is ON (default: White).")]
    [SerializeField] private Color onColor = Color.white;
    [Tooltip("Color when the feature is OFF (default: Black).")]
    [SerializeField] private Color offColor = Color.black;

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        EnsureReferences();

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (musicButton != null)
            musicButton.onClick.AddListener(ToggleMusic);

        if (soundButton != null)
            soundButton.onClick.AddListener(ToggleSound);
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);

        if (musicButton != null)
            musicButton.onClick.RemoveListener(ToggleMusic);

        if (soundButton != null)
            soundButton.onClick.RemoveListener(ToggleSound);
    }

    private void Start()
    {
        RefreshUI();
        if (root != null)
            root.SetActive(false);
    }

    /// <summary>
    /// Shows the setting panel. Bind this to the Setting button in HUD/Menu.
    /// </summary>
    public void Show()
    {
        EnsureReferences();
        RefreshUI();

        if (root != null)
            root.SetActive(true);

        if (AudioManager.TryGetInstance(out AudioManager audioManager))
            audioManager.PlaySoundEffect(AudioManager.SfxPressAnyButton);
    }

    /// <summary>
    /// Hides the setting panel. Bind this to the Quit / Close button.
    /// </summary>
    public void Hide()
    {
        if (root != null)
            root.SetActive(false);

        if (AudioManager.TryGetInstance(out AudioManager audioManager))
            audioManager.PlaySoundEffect(AudioManager.SfxPressAnyButton);
    }

    /// <summary>
    /// Toggles visibility of the setting panel.
    /// </summary>
    public void Toggle()
    {
        if (root != null && root.activeSelf)
            Hide();
        else
            Show();
    }

    /// <summary>
    /// Toggles background music and updates button visual.
    /// </summary>
    public void ToggleMusic()
    {
        if (AudioManager.TryGetInstance(out AudioManager audioManager))
        {
            audioManager.PlaySoundEffect(AudioManager.SfxPressAnyButton);
            audioManager.ToggleMusic();
            UpdateMusicVisual(audioManager.IsMusicOn);
        }
    }

    /// <summary>
    /// Toggles sound effects and updates button visual.
    /// </summary>
    public void ToggleSound()
    {
        if (AudioManager.TryGetInstance(out AudioManager audioManager))
        {
            audioManager.ToggleSoundEffect();
            audioManager.PlaySoundEffect(AudioManager.SfxPressAnyButton);
            UpdateSoundVisual(audioManager.IsSoundEffectOn);
        }
    }

    /// <summary>
    /// Refreshes button colors based on current audio settings.
    /// </summary>
    public void RefreshUI()
    {
        if (AudioManager.TryGetInstance(out AudioManager audioManager))
        {
            UpdateMusicVisual(audioManager.IsMusicOn);
            UpdateSoundVisual(audioManager.IsSoundEffectOn);
        }
        else
        {
            UpdateMusicVisual(true);
            UpdateSoundVisual(true);
        }
    }

    private void UpdateMusicVisual(bool isOn)
    {
        if (musicButtonImage != null)
            musicButtonImage.color = isOn ? onColor : offColor;
    }

    private void UpdateSoundVisual(bool isOn)
    {
        if (soundButtonImage != null)
            soundButtonImage.color = isOn ? onColor : offColor;
    }

    private void EnsureReferences()
    {
        if (root == null)
        {
            Transform rootTransform = transform.Find("Root");
            root = rootTransform != null ? rootTransform.gameObject : gameObject;
        }

        if (closeButton == null && root != null)
        {
            Transform quitTransform = root.transform.Find("Quit");
            if (quitTransform != null)
                closeButton = quitTransform.GetComponent<Button>();
        }

        if (musicButton == null && root != null)
        {
            Transform mbTransform = root.transform.Find("Music Button");
            if (mbTransform != null)
                musicButton = mbTransform.GetComponent<Button>();
        }

        if (musicButtonImage == null && musicButton != null)
        {
            musicButtonImage = musicButton.GetComponent<Image>();
        }

        if (soundButton == null && root != null)
        {
            Transform sbTransform = root.transform.Find("Sound Button");
            if (sbTransform != null)
                soundButton = sbTransform.GetComponent<Button>();
        }

        if (soundButtonImage == null && soundButton != null)
        {
            soundButtonImage = soundButton.GetComponent<Image>();
        }
    }
}
