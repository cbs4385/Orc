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
    private InputBindingsUI bindingsUI;

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

        // Build input bindings UI dynamically
        BuildInputBindingsSection();
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

    private void BuildInputBindingsSection()
    {
        // Find canvas (parent of all existing UI)
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Create a ScrollView that holds all content: existing settings + input bindings
        var scrollObj = new GameObject("OptionsScrollView");
        scrollObj.transform.SetParent(canvas.transform, false);
        var scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.1f, 0.12f);
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

        // -- Add Audio Section --
        AddSectionHeader(contentObj.transform, "AUDIO");
        AddAudioRow(contentObj.transform, "SFX Volume", sfxVolumeSlider, sfxValueText);
        AddAudioRow(contentObj.transform, "Music Volume", musicVolumeSlider, musicValueText);

        // -- Add Video Section --
        AddSectionHeader(contentObj.transform, "VIDEO");
        AddToggleRow(contentObj.transform, "Fullscreen", fullscreenToggle);

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

    private void AddSectionHeader(Transform parent, string text)
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
    }

    private void AddAudioRow(Transform parent, string label, Slider slider, TextMeshProUGUI valueText)
    {
        if (slider == null) return;

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
    }

    private void AddToggleRow(Transform parent, string label, Toggle toggle)
    {
        if (toggle == null) return;

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
    }

    private void AddSpacer(Transform parent, float height)
    {
        var obj = new GameObject("Spacer");
        obj.transform.SetParent(parent, false);
        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
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
        // Cancel any in-progress rebind
        if (InputBindingManager.Instance != null && InputBindingManager.Instance.IsListeningForRebind)
            InputBindingManager.Instance.CancelRebind();

        Debug.Log("[OptionsManager] Back clicked.");
        sceneLoader.LoadMainMenu();
    }
}
