using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Pause Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Options Controls")]
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxValueText;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicValueText;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button optionsBackButton;

    private bool isOpen;
    private bool optionsOpen;

    private void OnEnable()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
        if (optionsButton != null) optionsButton.onClick.AddListener(OnOptions);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
        if (optionsBackButton != null) optionsBackButton.onClick.AddListener(OnOptionsBack);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    private void OnDisable()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(OnResume);
        if (optionsButton != null) optionsButton.onClick.RemoveListener(OnOptions);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnMainMenu);
        if (optionsBackButton != null) optionsBackButton.onClick.RemoveListener(OnOptionsBack);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;

        // If menu is open but game got unpaused externally (Space key), close menus
        if (isOpen && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            isOpen = false;
            optionsOpen = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (optionsPanel != null) optionsPanel.SetActive(false);
        }

        if (Keyboard.current == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (GameManager.Instance.CurrentState == GameManager.GameState.GameOver) return;

        // If wall placement is active, let it handle ESC
        var wp = FindAnyObjectByType<WallPlacement>();
        if (wp != null && wp.IsPlacing) return;

        if (optionsOpen)
        {
            OnOptionsBack();
        }
        else if (isOpen)
        {
            OnResume();
        }
        else
        {
            Open();
        }
    }

    private void Open()
    {
        isOpen = true;
        optionsOpen = false;
        if (pausePanel != null) pausePanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            GameManager.Instance.TogglePause();
        }

        Debug.Log("[PauseMenu] Opened.");
    }

    private void OnResume()
    {
        isOpen = false;
        optionsOpen = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Paused)
        {
            GameManager.Instance.TogglePause();
        }

        Debug.Log("[PauseMenu] Resumed.");
    }

    private void OnOptions()
    {
        optionsOpen = true;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);

        // Load current values
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = GameSettings.SfxVolume;
            UpdateSfxText(sfxVolumeSlider.value);
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = GameSettings.MusicVolume;
            UpdateMusicText(musicVolumeSlider.value);
        }
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = GameSettings.Fullscreen;

        Debug.Log("[PauseMenu] Opened options.");
    }

    private void OnOptionsBack()
    {
        optionsOpen = false;
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
        Debug.Log("[PauseMenu] Returned to pause menu.");
    }

    private void OnMainMenu()
    {
        isOpen = false;
        Time.timeScale = 1f;
        Debug.Log("[PauseMenu] Quitting to main menu.");
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private void OnSfxVolumeChanged(float value)
    {
        GameSettings.SfxVolume = value;
        UpdateSfxText(value);
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetVolume(value);
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
}
