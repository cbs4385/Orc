using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxValueText;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicValueText;

    [Header("Video")]
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    private SceneLoader sceneLoader;

    private void Awake()
    {
        sceneLoader = GetComponent<SceneLoader>();
        if (sceneLoader == null)
            sceneLoader = gameObject.AddComponent<SceneLoader>();

        Debug.Log("[OptionsManager] Initialized.");
    }

    private void Start()
    {
        // Load saved values
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = GameSettings.SfxVolume;
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            UpdateSfxText(sfxVolumeSlider.value);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = GameSettings.MusicVolume;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            UpdateMusicText(musicVolumeSlider.value);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = GameSettings.Fullscreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDestroy()
    {
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);
    }

    private void OnSfxVolumeChanged(float value)
    {
        GameSettings.SfxVolume = value;
        UpdateSfxText(value);
    }

    private void UpdateSfxText(float value)
    {
        if (sfxValueText != null)
            sfxValueText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    private void OnMusicVolumeChanged(float value)
    {
        GameSettings.MusicVolume = value;
        UpdateMusicText(value);
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetMusicVolume(value);
    }

    private void UpdateMusicText(float value)
    {
        if (musicValueText != null)
            musicValueText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    private void OnFullscreenChanged(bool isOn)
    {
        GameSettings.Fullscreen = isOn;
    }

    private void OnBackClicked()
    {
        Debug.Log("[OptionsManager] Back clicked.");
        sceneLoader.LoadMainMenu();
    }
}
