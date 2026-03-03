using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static LocalizationManager;

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
    private InputBindingsUI bindingsUI;
    private Toggle onScreenControlsToggle;

    // Language selector UI
    private TextMeshProUGUI languageNameLabel;
    private TextMeshProUGUI languageHeaderLabel;

    // References to localized labels for refresh
    private TextMeshProUGUI audioHeaderLabel;
    private TextMeshProUGUI sfxLabel;
    private TextMeshProUGUI musicLabel;
    private TextMeshProUGUI videoHeaderLabel;
    private TextMeshProUGUI fullscreenLabel;
    private TextMeshProUGUI onScreenControlsLabel;

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

        // Create on-screen controls toggle programmatically
        CreateOnScreenControlsToggle();

        // Build input bindings UI dynamically
        BuildInputBindingsSection();

        // Refresh baked scene labels (title, back button) to current language
        RefreshAllLabels();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshAllLabels;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshAllLabels;
    }

    private void OnDestroy()
    {
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        if (onScreenControlsToggle != null)
            onScreenControlsToggle.onValueChanged.RemoveListener(OnOnScreenControlsChanged);
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);
    }

    private void BuildInputBindingsSection()
    {
        // Find canvas (parent of all existing UI)
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Create a ScrollView that holds all content: existing settings + input bindings
        var scrollObj = new GameObject("OptionsScrollView");
        scrollObj.transform.SetParent(canvas.transform, false);
        var scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.1f, 0.18f);
        scrollRect.anchorMax = new Vector2(0.9f, 0.82f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        var scrollView = scrollObj.AddComponent<ScrollRect>();
        scrollView.horizontal = false;
        scrollView.vertical = true;
        scrollView.movementType = ScrollRect.MovementType.Clamped;
        scrollView.scrollSensitivity = 30f;

        // Viewport (masks content)
        var viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        var viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f); // near-transparent for masking
        viewportObj.AddComponent<Mask>().showMaskGraphic = false;
        scrollView.viewport = viewportRect;

        // Content container (vertical layout)
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(20, 20, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollView.content = contentRect;

        // -- Add Language Section --
        languageHeaderLabel = AddSectionHeader(contentObj.transform, L("options.language"));
        BuildLanguageSelector(contentObj.transform);

        // -- Add Audio Section --
        audioHeaderLabel = AddSectionHeader(contentObj.transform, L("options.audio"));
        sfxLabel = AddAudioRow(contentObj.transform, L("options.sfx_volume"), sfxVolumeSlider, sfxValueText);
        musicLabel = AddAudioRow(contentObj.transform, L("options.music_volume"), musicVolumeSlider, musicValueText);

        // -- Add Video Section --
        videoHeaderLabel = AddSectionHeader(contentObj.transform, L("options.video"));
        fullscreenLabel = AddToggleRow(contentObj.transform, L("options.fullscreen"), fullscreenToggle);
        if (onScreenControlsToggle != null)
            onScreenControlsLabel = AddToggleRow(contentObj.transform, L("options.onscreen_controls"), onScreenControlsToggle);

        // -- Add spacer --
        AddSpacer(contentObj.transform, 20);

        // -- Build Input Bindings UI --
        bindingsUI = gameObject.AddComponent<InputBindingsUI>();
        bindingsUI.Build(contentObj.transform);

        // Reparent existing static UI elements out of the way (they were scene-placed and are now redundant)
        // The audio/video controls are serialized refs that still work — we just moved their visual context.
        // Hide the original scene-placed rows since we've replicated them in the scroll content
        HideOriginalSceneRows(canvas.transform);

        Debug.Log("[OptionsManager] Input bindings section built.");
    }

    private void HideOriginalSceneRows(Transform canvasRoot)
    {
        // Deactivate the old scene-placed elements (AudioHeader, SfxRow, MusicRow, VideoHeader, FullscreenRow)
        string[] names = { "AudioHeader", "SfxRow", "MusicRow", "VideoHeader", "FullscreenRow" };
        foreach (string name in names)
        {
            var t = canvasRoot.Find(name);
            if (t != null) t.gameObject.SetActive(false);
        }
    }

    private TextMeshProUGUI AddSectionHeader(Transform parent, string text)
    {
        var obj = new GameObject($"Header_{text}");
        obj.transform.SetParent(parent, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 36;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.8f, 0.7f, 0.5f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 50;
        return tmp;
    }

    private TextMeshProUGUI AddAudioRow(Transform parent, string label, Slider slider, TextMeshProUGUI valueText)
    {
        if (slider == null) return null;

        var rowObj = new GameObject($"Row_{label.Replace(" ", "")}");
        rowObj.transform.SetParent(parent, false);
        var le = rowObj.AddComponent<LayoutElement>();
        le.preferredHeight = 45;
        var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = true;

        // Label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(rowObj.transform, false);
        var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 28;
        labelTmp.color = Color.white;
        labelTmp.alignment = TextAlignmentOptions.MidlineRight;
        labelTmp.raycastTarget = false;
        var labelLE = labelObj.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 200;

        // Reparent the existing slider into this row
        slider.transform.SetParent(rowObj.transform, false);
        var sliderLE = slider.gameObject.GetComponent<LayoutElement>();
        if (sliderLE == null) sliderLE = slider.gameObject.AddComponent<LayoutElement>();
        sliderLE.preferredWidth = 350;
        sliderLE.minHeight = 30;

        // Reparent the value text
        if (valueText != null)
        {
            valueText.transform.SetParent(rowObj.transform, false);
            var vtLE = valueText.gameObject.GetComponent<LayoutElement>();
            if (vtLE == null) vtLE = valueText.gameObject.AddComponent<LayoutElement>();
            vtLE.preferredWidth = 80;
        }

        return labelTmp;
    }

    private TextMeshProUGUI AddToggleRow(Transform parent, string label, Toggle toggle)
    {
        if (toggle == null) return null;

        var rowObj = new GameObject($"Row_{label}");
        rowObj.transform.SetParent(parent, false);
        var le = rowObj.AddComponent<LayoutElement>();
        le.preferredHeight = 45;
        var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = true;

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(rowObj.transform, false);
        var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 28;
        labelTmp.color = Color.white;
        labelTmp.alignment = TextAlignmentOptions.MidlineRight;
        labelTmp.raycastTarget = false;
        var labelLE = labelObj.AddComponent<LayoutElement>();
        labelLE.preferredWidth = 200;

        toggle.transform.SetParent(rowObj.transform, false);
        return labelTmp;
    }

    private void AddSpacer(Transform parent, float height)
    {
        var obj = new GameObject("Spacer");
        obj.transform.SetParent(parent, false);
        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    private void BuildLanguageSelector(Transform parent)
    {
        var rowObj = new GameObject("LanguageRow");
        rowObj.transform.SetParent(parent, false);
        var le = rowObj.AddComponent<LayoutElement>();
        le.preferredHeight = 50;
        var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = true;

        // Left arrow button
        var leftBtnObj = new GameObject("LanguageLeftBtn");
        leftBtnObj.transform.SetParent(rowObj.transform, false);
        var leftBtnImg = leftBtnObj.AddComponent<Image>();
        leftBtnImg.color = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        var leftBtn = leftBtnObj.AddComponent<Button>();
        var leftColors = leftBtn.colors;
        leftColors.normalColor = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        leftColors.highlightedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        leftColors.pressedColor = new Color(0.6f, 0.4f, 0.1f, 1f);
        leftBtn.colors = leftColors;
        var leftLE = leftBtnObj.AddComponent<LayoutElement>();
        leftLE.preferredWidth = 50;

        var leftTextObj = new GameObject("Text");
        leftTextObj.transform.SetParent(leftBtnObj.transform, false);
        var leftTmp = leftTextObj.AddComponent<TextMeshProUGUI>();
        leftTmp.text = "<";
        leftTmp.fontSize = 32;
        leftTmp.fontStyle = FontStyles.Bold;
        leftTmp.color = new Color(0.9f, 0.8f, 0.5f);
        leftTmp.alignment = TextAlignmentOptions.Center;
        var leftTextRect = leftTextObj.GetComponent<RectTransform>();
        leftTextRect.anchorMin = Vector2.zero;
        leftTextRect.anchorMax = Vector2.one;
        leftTextRect.offsetMin = Vector2.zero;
        leftTextRect.offsetMax = Vector2.zero;

        // Language name label
        var nameLabelObj = new GameObject("LanguageNameLabel");
        nameLabelObj.transform.SetParent(rowObj.transform, false);
        languageNameLabel = nameLabelObj.AddComponent<TextMeshProUGUI>();
        languageNameLabel.text = LocalizationManager.GetLanguageNativeName(LocalizationManager.CurrentLanguage);
        languageNameLabel.fontSize = 28;
        languageNameLabel.fontStyle = FontStyles.Bold;
        languageNameLabel.color = new Color(0.9f, 0.75f, 0.3f);
        languageNameLabel.alignment = TextAlignmentOptions.Center;
        var nameLE = nameLabelObj.AddComponent<LayoutElement>();
        nameLE.preferredWidth = 200;

        // Right arrow button
        var rightBtnObj = new GameObject("LanguageRightBtn");
        rightBtnObj.transform.SetParent(rowObj.transform, false);
        var rightBtnImg = rightBtnObj.AddComponent<Image>();
        rightBtnImg.color = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        var rightBtn = rightBtnObj.AddComponent<Button>();
        var rightColors = rightBtn.colors;
        rightColors.normalColor = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        rightColors.highlightedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        rightColors.pressedColor = new Color(0.6f, 0.4f, 0.1f, 1f);
        rightBtn.colors = rightColors;
        var rightLE = rightBtnObj.AddComponent<LayoutElement>();
        rightLE.preferredWidth = 50;

        var rightTextObj = new GameObject("Text");
        rightTextObj.transform.SetParent(rightBtnObj.transform, false);
        var rightTmp = rightTextObj.AddComponent<TextMeshProUGUI>();
        rightTmp.text = ">";
        rightTmp.fontSize = 32;
        rightTmp.fontStyle = FontStyles.Bold;
        rightTmp.color = new Color(0.9f, 0.8f, 0.5f);
        rightTmp.alignment = TextAlignmentOptions.Center;
        var rightTextRect = rightTextObj.GetComponent<RectTransform>();
        rightTextRect.anchorMin = Vector2.zero;
        rightTextRect.anchorMax = Vector2.one;
        rightTextRect.offsetMin = Vector2.zero;
        rightTextRect.offsetMax = Vector2.zero;

        // Wire button listeners
        leftBtn.onClick.AddListener(OnLanguagePrevious);
        rightBtn.onClick.AddListener(OnLanguageNext);

        Debug.Log($"[OptionsManager] Language selector built. Current: {LocalizationManager.GetLanguageNativeName(LocalizationManager.CurrentLanguage)}");
    }

    private void OnLanguagePrevious()
    {
        int current = (int)LocalizationManager.CurrentLanguage;
        int count = LocalizationManager.LanguageCount;
        int prev = (current - 1 + count) % count;
        ApplyLanguage((LocalizationManager.Language)prev);
    }

    private void OnLanguageNext()
    {
        int current = (int)LocalizationManager.CurrentLanguage;
        int count = LocalizationManager.LanguageCount;
        int next = (current + 1) % count;
        ApplyLanguage((LocalizationManager.Language)next);
    }

    private void ApplyLanguage(LocalizationManager.Language newLang)
    {
        // SetLanguage handles saving to GameSettings and firing OnLanguageChanged
        LocalizationManager.SetLanguage(newLang);
        Debug.Log($"[OptionsManager] Language changed to {LocalizationManager.GetLanguageNativeName(newLang)}");
    }

    private void RefreshAllLabels()
    {
        // Update language selector display
        if (languageNameLabel != null)
            languageNameLabel.text = LocalizationManager.GetLanguageNativeName(LocalizationManager.CurrentLanguage);
        if (languageHeaderLabel != null)
            languageHeaderLabel.text = L("options.language");

        // Update section headers
        if (audioHeaderLabel != null)
            audioHeaderLabel.text = L("options.audio");
        if (videoHeaderLabel != null)
            videoHeaderLabel.text = L("options.video");

        // Update row labels
        if (sfxLabel != null)
            sfxLabel.text = L("options.sfx_volume");
        if (musicLabel != null)
            musicLabel.text = L("options.music_volume");
        if (fullscreenLabel != null)
            fullscreenLabel.text = L("options.fullscreen");
        if (onScreenControlsLabel != null)
            onScreenControlsLabel.text = L("options.onscreen_controls");

        // Update title
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            var titleTransform = canvas.transform.Find("TitleText");
            if (titleTransform != null)
            {
                var tmp = titleTransform.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = L("menu.options");
            }
        }

        // Update back button label
        if (backButton != null)
        {
            var tmp = backButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = L("menu.back");
        }

        Debug.Log("[OptionsManager] All labels refreshed for language change.");
    }

    private void OnSfxVolumeChanged(float value)
    {
        GameSettings.SfxVolume = value;
        UpdateSfxText(value);
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

    private void CreateOnScreenControlsToggle()
    {
        // Create a Toggle for On-Screen Controls setting
        var toggleObj = new GameObject("OnScreenControlsToggle");
        toggleObj.transform.SetParent(transform, false);
        onScreenControlsToggle = toggleObj.AddComponent<Toggle>();

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(toggleObj.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(30, 30);
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f);

        // Checkmark
        var checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(bgObj.transform, false);
        var checkRect = checkObj.AddComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.offsetMin = new Vector2(4, 4);
        checkRect.offsetMax = new Vector2(-4, -4);
        var checkImage = checkObj.AddComponent<Image>();
        checkImage.color = new Color(0.8f, 0.7f, 0.5f);

        onScreenControlsToggle.targetGraphic = bgImage;
        onScreenControlsToggle.graphic = checkImage;
        onScreenControlsToggle.isOn = PlatformDetector.ShowOnScreenControls;
        onScreenControlsToggle.onValueChanged.AddListener(OnOnScreenControlsChanged);

        // On mobile, force ON and make non-interactable
        if (PlatformDetector.IsMobile)
        {
            onScreenControlsToggle.isOn = true;
            onScreenControlsToggle.interactable = false;
        }

        Debug.Log($"[OptionsManager] On-Screen Controls toggle created. isOn={onScreenControlsToggle.isOn}, interactable={onScreenControlsToggle.interactable}");
    }

    private void OnOnScreenControlsChanged(bool isOn)
    {
        PlatformDetector.ShowOnScreenControls = isOn;
        Debug.Log($"[OptionsManager] On-Screen Controls changed to {isOn}");
    }

    private void OnBackClicked()
    {
        // Cancel any in-progress rebind
        if (InputBindingManager.Instance != null && InputBindingManager.Instance.IsListeningForRebind)
            InputBindingManager.Instance.CancelRebind();

        Debug.Log("[OptionsManager] Back clicked.");
        sceneLoader.LoadMainMenu();
    }
}
