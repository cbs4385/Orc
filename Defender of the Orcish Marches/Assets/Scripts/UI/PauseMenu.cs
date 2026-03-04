using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using static LocalizationManager;

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
    private SaveSlotPicker saveSlotPicker;
    private Button saveQuitButton;

    private void OnEnable()
    {
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
        if (optionsButton != null) optionsButton.onClick.AddListener(OnOptions);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
        if (optionsBackButton != null) optionsBackButton.onClick.AddListener(OnOptionsBack);

        // Create Save & Quit button programmatically as sibling of mainMenuButton
        if (saveQuitButton == null && mainMenuButton != null)
        {
            CreateSaveQuitButton();
        }
        if (saveQuitButton != null) saveQuitButton.onClick.AddListener(OnSaveQuit);

        // Set button text from localization
        SetButtonText(resumeButton, L("pause.resume"));
        SetButtonText(optionsButton, L("pause.options"));
        SetButtonText(mainMenuButton, L("pause.quit"));

        // Localize the PAUSED title and give it proper spacing
        LocalizePauseTitle();

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
        if (saveQuitButton != null) saveQuitButton.onClick.RemoveListener(OnSaveQuit);

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

        if (InputBindingManager.Instance == null) return;
        if (!InputBindingManager.Instance.WasPressedThisFrame(GameAction.OpenMenu)) return;
        if (GameManager.Instance.CurrentState == GameManager.GameState.GameOver) return;

        // If wall placement is active, let it handle ESC
        var wp = FindAnyObjectByType<WallPlacement>();
        if (wp != null && wp.IsPlacing) return;

        // If save slot picker is visible, close it first
        if (saveSlotPicker != null && saveSlotPicker.IsVisible)
        {
            saveSlotPicker.Hide();
            return;
        }

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

        // Grow all buttons to fit their localized text (TMP is initialized by now)
        GrowAllButtonsToFitText();

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

    private void CreateSaveQuitButton()
    {
        if (mainMenuButton == null) return;
        var parent = mainMenuButton.transform.parent;

        var btnObj = new GameObject("SaveQuitButton");
        btnObj.transform.SetParent(parent, false);
        // Place it just before the main menu button
        btnObj.transform.SetSiblingIndex(mainMenuButton.transform.GetSiblingIndex());

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.3f, 0.2f, 0.1f, 0.9f);

        // Match the mainMenuButton's size via sizeDelta (childControlWidth is off)
        var sourceRect = mainMenuButton.GetComponent<RectTransform>();
        var btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.sizeDelta = sourceRect.sizeDelta;
        Debug.Log($"[PauseMenu] SaveQuit sizeDelta set to {btnRect.sizeDelta} (matching sibling {sourceRect.sizeDelta})");

        saveQuitButton = btnObj.AddComponent<Button>();
        var colors = saveQuitButton.colors;
        colors.normalColor = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        colors.highlightedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        colors.pressedColor = new Color(0.6f, 0.4f, 0.1f, 1f);
        saveQuitButton.colors = colors;

        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = L("pause.save_quit");

        // Copy text styling from the mainMenuButton for uniform appearance
        var sourceTmp = mainMenuButton.GetComponentInChildren<TextMeshProUGUI>();
        if (sourceTmp != null)
        {
            tmp.font = sourceTmp.font;
            tmp.fontSize = sourceTmp.fontSize;
            tmp.fontStyle = sourceTmp.fontStyle;
            tmp.color = sourceTmp.color;
            tmp.alignment = sourceTmp.alignment;
            // Fixed font size — do NOT copy auto-sizing; button will grow to fit instead
            tmp.enableAutoSizing = false;
        }
        else
        {
            tmp.fontSize = 28;
            tmp.enableAutoSizing = false;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.9f, 0.8f, 0.5f);
            tmp.alignment = TextAlignmentOptions.Center;
            Debug.LogWarning("[PauseMenu] Could not find TMP on mainMenuButton to copy styling.");
        }

        var txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(8f, 0f);
        txtRect.offsetMax = new Vector2(-8f, 0f);

        Debug.Log($"[PauseMenu] Save & Quit button created. sizeDelta={btnRect.sizeDelta}. Text width will be checked in Open().");
    }

    private void OnSaveQuit()
    {
        Debug.Log("[PauseMenu] Save & Quit clicked.");

        // Create or find SaveSlotPicker
        if (saveSlotPicker == null)
        {
            var pickerObj = new GameObject("SaveSlotPicker");
            saveSlotPicker = pickerObj.AddComponent<SaveSlotPicker>();
        }

        saveSlotPicker.Show(SaveSlotPicker.Mode.Save, (slot) =>
        {
            // Save to selected slot
            SaveManager.SaveToSlot(slot);

            // Return to main menu
            isOpen = false;
            Time.timeScale = 1f;
            Debug.Log($"[PauseMenu] Saved to slot {slot}, returning to main menu.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        });
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
            sfxValueText.text = L("options.volume_pct", Mathf.RoundToInt(value * 100));
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
            musicValueText.text = L("options.volume_pct", Mathf.RoundToInt(value * 100));
    }

    private void OnFullscreenChanged(bool isOn)
    {
        GameSettings.Fullscreen = isOn;
    }

    /// <summary>Set button label. Width adjustment is deferred to Open() when TMP is fully initialized.</summary>
    private static void SetButtonText(Button button, string text)
    {
        if (button == null) return;
        var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null) return;
        tmp.text = text;

        // Use fixed font size so all buttons match — disable auto-sizing
        if (tmp.enableAutoSizing)
        {
            tmp.fontSize = tmp.fontSizeMax;
            tmp.enableAutoSizing = false;
        }
    }

    /// <summary>Check all pause buttons and grow any that are too narrow for their text (sets sizeDelta directly).</summary>
    private void GrowAllButtonsToFitText()
    {
        Button[] buttons = { resumeButton, optionsButton, saveQuitButton, mainMenuButton };
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp == null) continue;

            tmp.ForceMeshUpdate();
            float textWidth = tmp.GetPreferredValues().x;
            float padding = 24f;
            float neededWidth = textWidth + padding;

            var btnRect = btn.GetComponent<RectTransform>();
            float currentWidth = btnRect.sizeDelta.x;

            Debug.Log($"[PauseMenu] ButtonWidth '{btn.name}': text='{tmp.text}', textW={textWidth:F0}, needed={neededWidth:F0}, sizeDelta.x={currentWidth:F0}");

            if (neededWidth > currentWidth)
            {
                btnRect.sizeDelta = new Vector2(neededWidth, btnRect.sizeDelta.y);
                Debug.Log($"[PauseMenu] Grew '{btn.name}' sizeDelta.x to {neededWidth:F0}");
            }
        }
    }

    /// <summary>Find the PAUSED title text in the pause panel and set it from localization.</summary>
    private void LocalizePauseTitle()
    {
        if (pausePanel == null) return;

        // Log the pause panel layout structure for diagnosis
        var vlg = pausePanel.GetComponent<VerticalLayoutGroup>();
        if (vlg != null)
        {
            Debug.Log($"[PauseMenu] PausePanel VLG: spacing={vlg.spacing}, padding=({vlg.padding.left},{vlg.padding.top},{vlg.padding.right},{vlg.padding.bottom}), childForceW={vlg.childForceExpandWidth}, childCtrlW={vlg.childControlWidth}");
        }

        // Log all children
        for (int i = 0; i < pausePanel.transform.childCount; i++)
        {
            var child = pausePanel.transform.GetChild(i);
            var childRect = child.GetComponent<RectTransform>();
            var childLE = child.GetComponent<LayoutElement>();
            var childTmp = child.GetComponent<TextMeshProUGUI>();
            var childBtn = child.GetComponent<Button>();
            Debug.Log($"[PauseMenu] Child[{i}] '{child.name}': isBtn={childBtn != null}, isTMP={childTmp != null}, rect.size={childRect.rect.size}, LE.prefH={childLE?.preferredHeight ?? -1}, LE.prefW={childLE?.preferredWidth ?? -1}");
        }

        // The title is typically the first TMP text in the pause panel that isn't inside a button
        foreach (var tmp in pausePanel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.GetComponentInParent<Button>() != null) continue;

            tmp.text = L("pause.title");

            // The title has a large font (56pt) but only 40px rect height — expand it to fit
            float neededHeight = tmp.fontSize * 1.2f; // rough line height
            var titleRect = tmp.rectTransform;
            if (titleRect.rect.height < neededHeight)
            {
                titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, neededHeight);
                Debug.Log($"[PauseMenu] Expanded title rect height from {titleRect.rect.height} to {neededHeight} for fontSize={tmp.fontSize}");
            }

            // Ensure the title has a LayoutElement with proper height so it pushes buttons down
            var titleLE = tmp.GetComponent<LayoutElement>();
            if (titleLE == null)
                titleLE = tmp.gameObject.AddComponent<LayoutElement>();
            titleLE.minHeight = neededHeight;
            titleLE.preferredHeight = neededHeight;

            Debug.Log($"[PauseMenu] Localized pause title to '{tmp.text}'. fontSize={tmp.fontSize}, rect.height={titleRect.rect.height}, LE.minH={titleLE.minHeight}");
            break;
        }

        // Also log button text info for sizing diagnosis
        Button[] buttons = { resumeButton, optionsButton, saveQuitButton, mainMenuButton };
        string[] names = { "Resume", "Options", "SaveQuit", "Quit" };
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            var tmp = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            var le = buttons[i].GetComponent<LayoutElement>();
            var btnRect = buttons[i].GetComponent<RectTransform>();
            Debug.Log($"[PauseMenu] Btn '{names[i]}': text='{tmp?.text}', fontSize={tmp?.fontSize}, autoSize={tmp?.enableAutoSizing}, fontSizeMin={tmp?.fontSizeMin}, fontSizeMax={tmp?.fontSizeMax}, LE.prefW={le?.preferredWidth ?? -1}, rect.w={btnRect.rect.width}");
        }
    }
}
