using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIBuilder : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Game/Build UI")]
    public static void BuildUI()
    {
        // Delete existing canvas if any
        var existingCanvas = GameObject.Find("GameCanvas");
        if (existingCanvas != null) Object.DestroyImmediate(existingCanvas);

        // Create Canvas
        var canvasObj = new GameObject("GameCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Event System
        if (GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ===== TOP BAR =====
        var topBar = CreatePanel(canvasObj.transform, "TopBar", new Color(0, 0, 0, 0.7f));
        var topBarRect = topBar.GetComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0, 1);
        topBarRect.anchorMax = new Vector2(1, 1);
        topBarRect.pivot = new Vector2(0.5f, 1);
        topBarRect.sizeDelta = new Vector2(0, 50);
        topBarRect.anchoredPosition = Vector2.zero;

        // Mask clips the wheel to the bar bounds (stencil-based)
        var barMask = topBar.AddComponent<Mask>();
        barMask.showMaskGraphic = true;

        var topLayout = topBar.AddComponent<HorizontalLayoutGroup>();
        topLayout.padding = new RectOffset(20, 20, 5, 5);
        topLayout.spacing = 40;
        topLayout.childAlignment = TextAnchor.MiddleCenter;
        topLayout.childControlWidth = false;
        topLayout.childControlHeight = true;
        topLayout.childForceExpandHeight = true;

        var treasureText = CreateText(topBar.transform, "TreasureText", "Gold: 50", 24, Color.yellow, 200);
        treasureText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        var menialText = CreateText(topBar.transform, "MenialText", "Menials: 3/3", 24, new Color(0.4f, 0.7f, 1f), 250);
        menialText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        var timerText = CreateText(topBar.transform, "TimerText", "0:00", 24, Color.white, 120);
        timerText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        var enemyText = CreateText(topBar.transform, "EnemyCountText", "Enemies: 0", 24, new Color(1f, 0.5f, 0.5f), 200);
        enemyText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        var killsText = CreateText(topBar.transform, "KillsText", "Kills: 0", 24, new Color(1f, 0.7f, 0.3f), 150);
        killsText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

        // Spacer pushes wheel + pause to the right
        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(topBar.transform, false);
        spacer.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
        var spacerLE = spacer.AddComponent<LayoutElement>();
        spacerLE.flexibleWidth = 1;

        // Day number text — to the left of the wheel
        var dayNumObj = CreateText(topBar.transform, "DayNumberText", "Day 1", 22, Color.white, 70);
        var dayNumTmp = dayNumObj.GetComponent<TextMeshProUGUI>();
        dayNumTmp.fontStyle = FontStyles.Bold;
        dayNumTmp.alignment = TextAlignmentOptions.Right;

        // Day/Night wheel slot — participates in layout, 50px wide clip rect
        var wheelSlot = new GameObject("WheelSlot");
        wheelSlot.transform.SetParent(topBar.transform, false);
        wheelSlot.AddComponent<RectTransform>().sizeDelta = new Vector2(50, 40);
        wheelSlot.AddComponent<RectMask2D>(); // clips wheel to 50px slot without stencil

        // Actual wheel image — oversized child of slot, clipped by slot mask
        var wheelContainer = new GameObject("DayNightWheel");
        wheelContainer.transform.SetParent(wheelSlot.transform, false);
        var wheelContainerRect = wheelContainer.AddComponent<RectTransform>();
        wheelContainerRect.anchorMin = new Vector2(0.5f, 0f);
        wheelContainerRect.anchorMax = new Vector2(0.5f, 0f);
        wheelContainerRect.pivot = new Vector2(0.5f, 0.5f);
        wheelContainerRect.sizeDelta = new Vector2(200, 200);
        wheelContainerRect.anchoredPosition = new Vector2(0, -10); // center below bar bottom
        var wheelImg = wheelContainer.AddComponent<Image>();
        wheelImg.preserveAspect = true;
        wheelImg.raycastTarget = false;

        // Pause icon button (square, icon-only)
        var pauseBtn = new GameObject("PauseButton");
        pauseBtn.transform.SetParent(topBar.transform, false);
        var pauseBtnRect = pauseBtn.AddComponent<RectTransform>();
        pauseBtnRect.sizeDelta = new Vector2(35, 35);
        var pauseBtnImg = pauseBtn.AddComponent<Image>();
        pauseBtnImg.color = new Color(0.3f, 0.3f, 0.3f);
        var pauseBtnButton = pauseBtn.AddComponent<Button>();
        var pauseBtnColors = pauseBtnButton.colors;
        pauseBtnColors.highlightedColor = new Color(0.5f, 0.5f, 0.5f);
        pauseBtnButton.colors = pauseBtnColors;

        var pauseIconObj = new GameObject("Icon");
        pauseIconObj.transform.SetParent(pauseBtn.transform, false);
        var pauseIconRect = pauseIconObj.AddComponent<RectTransform>();
        pauseIconRect.anchorMin = new Vector2(0.15f, 0.15f);
        pauseIconRect.anchorMax = new Vector2(0.85f, 0.85f);
        pauseIconRect.offsetMin = Vector2.zero;
        pauseIconRect.offsetMax = Vector2.zero;
        var pauseIconImg = pauseIconObj.AddComponent<Image>();
        pauseIconImg.preserveAspect = true;

        // Add DayNightWheel component to wheel
        var dayNightWheel = wheelContainer.AddComponent<DayNightWheel>();
        var wheelSO = new SerializedObject(dayNightWheel);
        wheelSO.FindProperty("wheelImage").objectReferenceValue = wheelImg;
        wheelSO.FindProperty("dayNumberText").objectReferenceValue = dayNumTmp;
        wheelSO.ApplyModifiedProperties();

        // Add GameHUD component
        var hud = canvasObj.AddComponent<GameHUD>();
        var hudSO = new SerializedObject(hud);
        hudSO.FindProperty("treasureText").objectReferenceValue = treasureText.GetComponent<TextMeshProUGUI>();
        hudSO.FindProperty("menialText").objectReferenceValue = menialText.GetComponent<TextMeshProUGUI>();
        hudSO.FindProperty("timerText").objectReferenceValue = timerText.GetComponent<TextMeshProUGUI>();
        hudSO.FindProperty("enemyCountText").objectReferenceValue = enemyText.GetComponent<TextMeshProUGUI>();
        hudSO.FindProperty("killsText").objectReferenceValue = killsText.GetComponent<TextMeshProUGUI>();
        hudSO.FindProperty("pauseButton").objectReferenceValue = pauseBtnButton;
        hudSO.FindProperty("pauseButtonIcon").objectReferenceValue = pauseIconImg;
        hudSO.ApplyModifiedProperties();

        // ===== UPGRADE PANEL (Bottom) =====
        var upgradeRoot = CreatePanel(canvasObj.transform, "UpgradePanel", new Color(0, 0, 0, 0.8f));
        var upgradeRootRect = upgradeRoot.GetComponent<RectTransform>();
        upgradeRootRect.anchorMin = new Vector2(0, 0);
        upgradeRootRect.anchorMax = new Vector2(1, 0);
        upgradeRootRect.pivot = new Vector2(0.5f, 0);
        upgradeRootRect.sizeDelta = new Vector2(0, 80);
        upgradeRootRect.anchoredPosition = Vector2.zero;

        var upgradeLayout = upgradeRoot.AddComponent<HorizontalLayoutGroup>();
        upgradeLayout.padding = new RectOffset(10, 10, 5, 5);
        upgradeLayout.spacing = 10;
        upgradeLayout.childAlignment = TextAnchor.MiddleLeft;
        upgradeLayout.childControlWidth = false;
        upgradeLayout.childControlHeight = true;

        // Create upgrade button template prefab
        var buttonTemplate = CreateUpgradeButton(upgradeRoot.transform, "UpgradeButtonTemplate");
        buttonTemplate.SetActive(false);

        // Add hint text
        var hintText = CreateText(upgradeRoot.transform, "HintText", "[U] Toggle | Right-click loot to collect", 16, new Color(0.7f, 0.7f, 0.7f), 400);

        // Add UpgradePanel component
        var upgradePanel = canvasObj.AddComponent<UpgradePanel>();
        var upgPanelSO = new SerializedObject(upgradePanel);
        upgPanelSO.FindProperty("buttonPrefab").objectReferenceValue = buttonTemplate;
        upgPanelSO.FindProperty("buttonContainer").objectReferenceValue = upgradeRoot.transform;
        upgPanelSO.FindProperty("panelRoot").objectReferenceValue = upgradeRoot;
        upgPanelSO.ApplyModifiedProperties();

        // ===== GAME OVER SCREEN =====
        var gameOverRoot = CreatePanel(canvasObj.transform, "GameOverPanel", new Color(0, 0, 0, 0.85f));
        var goRect = gameOverRoot.GetComponent<RectTransform>();
        goRect.anchorMin = Vector2.zero;
        goRect.anchorMax = Vector2.one;
        goRect.sizeDelta = Vector2.zero;

        var goTitle = CreateText(gameOverRoot.transform, "GameOverTitle", "WALLS BREACHED!\nGAME OVER", 48, Color.red, 600);
        var goTitleRect = goTitle.GetComponent<RectTransform>();
        goTitleRect.anchorMin = new Vector2(0.5f, 0.6f);
        goTitleRect.anchorMax = new Vector2(0.5f, 0.6f);
        goTitleRect.anchoredPosition = Vector2.zero;
        goTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var goStats = CreateText(gameOverRoot.transform, "GameOverStats", "Survival Time: 0:00", 28, Color.white, 500);
        var goStatsRect = goStats.GetComponent<RectTransform>();
        goStatsRect.anchorMin = new Vector2(0.5f, 0.45f);
        goStatsRect.anchorMax = new Vector2(0.5f, 0.45f);
        goStatsRect.anchoredPosition = Vector2.zero;
        goStats.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Restart button
        var restartBtn = CreateUIButton(gameOverRoot.transform, "RestartButton", "RESTART", 200, 50);
        var restartBtnRect = restartBtn.GetComponent<RectTransform>();
        restartBtnRect.anchorMin = new Vector2(0.5f, 0.3f);
        restartBtnRect.anchorMax = new Vector2(0.5f, 0.3f);
        restartBtnRect.anchoredPosition = Vector2.zero;

        gameOverRoot.SetActive(false);

        // Add GameOverScreen component
        var goScreen = canvasObj.AddComponent<GameOverScreen>();
        var goSO = new SerializedObject(goScreen);
        goSO.FindProperty("panelRoot").objectReferenceValue = gameOverRoot;
        goSO.FindProperty("titleText").objectReferenceValue = goTitle.GetComponent<TextMeshProUGUI>();
        goSO.FindProperty("statsText").objectReferenceValue = goStats.GetComponent<TextMeshProUGUI>();
        goSO.FindProperty("restartButton").objectReferenceValue = restartBtn.GetComponent<Button>();
        goSO.ApplyModifiedProperties();

        // ===== PAUSE MENU OVERLAY =====
        var pauseRoot = CreatePanel(canvasObj.transform, "PauseMenuPanel", new Color(0, 0, 0, 0.75f));
        var pauseRect = pauseRoot.GetComponent<RectTransform>();
        pauseRect.anchorMin = Vector2.zero;
        pauseRect.anchorMax = Vector2.one;
        pauseRect.sizeDelta = Vector2.zero;

        var pauseTitle = CreateText(pauseRoot.transform, "PauseTitle", "PAUSED", 56, new Color(0.9f, 0.75f, 0.3f), 400);
        var pauseTitleRect = pauseTitle.GetComponent<RectTransform>();
        pauseTitleRect.anchorMin = new Vector2(0.5f, 0.72f);
        pauseTitleRect.anchorMax = new Vector2(0.5f, 0.72f);
        pauseTitleRect.anchoredPosition = Vector2.zero;
        pauseTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        pauseTitle.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        var pauseBtnPanel = new GameObject("PauseButtons");
        pauseBtnPanel.transform.SetParent(pauseRoot.transform, false);
        var pausePanelRect = pauseBtnPanel.AddComponent<RectTransform>();
        pausePanelRect.anchorMin = new Vector2(0.5f, 0.3f);
        pausePanelRect.anchorMax = new Vector2(0.5f, 0.6f);
        pausePanelRect.sizeDelta = new Vector2(300, 250);
        pausePanelRect.anchoredPosition = Vector2.zero;
        var pauseVlg = pauseBtnPanel.AddComponent<VerticalLayoutGroup>();
        pauseVlg.spacing = 15;
        pauseVlg.childAlignment = TextAnchor.MiddleCenter;
        pauseVlg.childControlWidth = true;
        pauseVlg.childControlHeight = true;
        pauseVlg.childForceExpandWidth = true;
        pauseVlg.childForceExpandHeight = true;

        var pmResumeBtn = CreatePauseButton(pauseBtnPanel.transform, "ResumeButton", "RESUME");
        var pmOptionsBtn = CreatePauseButton(pauseBtnPanel.transform, "OptionsButton", "OPTIONS");
        var pmMainMenuBtn = CreatePauseButton(pauseBtnPanel.transform, "MainMenuButton", "MAIN MENU");

        pauseRoot.SetActive(false);

        // ===== IN-GAME OPTIONS OVERLAY =====
        var optRoot = CreatePanel(canvasObj.transform, "OptionsPanel", new Color(0, 0, 0, 0.85f));
        var optRect = optRoot.GetComponent<RectTransform>();
        optRect.anchorMin = Vector2.zero;
        optRect.anchorMax = Vector2.one;
        optRect.sizeDelta = Vector2.zero;

        var optTitle = CreateText(optRoot.transform, "OptionsTitle", "OPTIONS", 56, new Color(0.9f, 0.75f, 0.3f), 400);
        var optTitleRect = optTitle.GetComponent<RectTransform>();
        optTitleRect.anchorMin = new Vector2(0.5f, 0.8f);
        optTitleRect.anchorMax = new Vector2(0.5f, 0.8f);
        optTitleRect.anchoredPosition = Vector2.zero;
        optTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        optTitle.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Audio section header
        var audioLabel = CreateText(optRoot.transform, "AudioHeader", "AUDIO", 32, new Color(0.8f, 0.7f, 0.5f), 400);
        var audioLabelRect = audioLabel.GetComponent<RectTransform>();
        audioLabelRect.anchorMin = new Vector2(0.5f, 0.68f);
        audioLabelRect.anchorMax = new Vector2(0.5f, 0.68f);
        audioLabelRect.anchoredPosition = Vector2.zero;
        audioLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        audioLabel.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // SFX volume row
        var sfxRow = new GameObject("SfxRow");
        sfxRow.transform.SetParent(optRoot.transform, false);
        var sfxRowRect = sfxRow.AddComponent<RectTransform>();
        sfxRowRect.anchorMin = new Vector2(0.5f, 0.58f);
        sfxRowRect.anchorMax = new Vector2(0.5f, 0.58f);
        sfxRowRect.sizeDelta = new Vector2(600, 40);
        sfxRowRect.anchoredPosition = Vector2.zero;
        var sfxHlg = sfxRow.AddComponent<HorizontalLayoutGroup>();
        sfxHlg.spacing = 15;
        sfxHlg.childAlignment = TextAnchor.MiddleCenter;
        sfxHlg.childControlWidth = true;
        sfxHlg.childControlHeight = true;
        sfxHlg.childForceExpandHeight = true;

        var sfxLabel = CreateText(sfxRow.transform, "SfxLabel", "SFX Volume", 24, Color.white, 180);
        sfxLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
        var sfxLabelLE = sfxLabel.AddComponent<LayoutElement>();
        sfxLabelLE.preferredWidth = 180;

        var sfxSlider = CreateInGameSlider("SfxSlider", sfxRow.transform);
        var sfxSliderLE = sfxSlider.AddComponent<LayoutElement>();
        sfxSliderLE.preferredWidth = 300;
        sfxSliderLE.minHeight = 30;

        var sfxVal = CreateText(sfxRow.transform, "SfxValueText", "50%", 24, Color.white, 70);
        sfxVal.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
        var sfxValLE = sfxVal.AddComponent<LayoutElement>();
        sfxValLE.preferredWidth = 70;

        // Music volume row
        var musicRow = new GameObject("MusicRow");
        musicRow.transform.SetParent(optRoot.transform, false);
        var musicRowRect = musicRow.AddComponent<RectTransform>();
        musicRowRect.anchorMin = new Vector2(0.5f, 0.48f);
        musicRowRect.anchorMax = new Vector2(0.5f, 0.48f);
        musicRowRect.sizeDelta = new Vector2(600, 40);
        musicRowRect.anchoredPosition = Vector2.zero;
        var musicHlg = musicRow.AddComponent<HorizontalLayoutGroup>();
        musicHlg.spacing = 15;
        musicHlg.childAlignment = TextAnchor.MiddleCenter;
        musicHlg.childControlWidth = true;
        musicHlg.childControlHeight = true;
        musicHlg.childForceExpandHeight = true;

        var musicLabel = CreateText(musicRow.transform, "MusicLabel", "Music Volume", 24, Color.white, 180);
        musicLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
        var musicLabelLE = musicLabel.AddComponent<LayoutElement>();
        musicLabelLE.preferredWidth = 180;

        var musicSlider = CreateInGameSlider("MusicSlider", musicRow.transform);
        var musicSliderLE = musicSlider.AddComponent<LayoutElement>();
        musicSliderLE.preferredWidth = 300;
        musicSliderLE.minHeight = 30;

        var musicVal = CreateText(musicRow.transform, "MusicValueText", "50%", 24, Color.white, 70);
        musicVal.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
        var musicValLE = musicVal.AddComponent<LayoutElement>();
        musicValLE.preferredWidth = 70;

        // Video section header
        var videoLabel = CreateText(optRoot.transform, "VideoHeader", "VIDEO", 32, new Color(0.8f, 0.7f, 0.5f), 400);
        var videoLabelRect = videoLabel.GetComponent<RectTransform>();
        videoLabelRect.anchorMin = new Vector2(0.5f, 0.36f);
        videoLabelRect.anchorMax = new Vector2(0.5f, 0.36f);
        videoLabelRect.anchoredPosition = Vector2.zero;
        videoLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        videoLabel.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Fullscreen row
        var fsRow = new GameObject("FullscreenRow");
        fsRow.transform.SetParent(optRoot.transform, false);
        var fsRowRect = fsRow.AddComponent<RectTransform>();
        fsRowRect.anchorMin = new Vector2(0.5f, 0.26f);
        fsRowRect.anchorMax = new Vector2(0.5f, 0.26f);
        fsRowRect.sizeDelta = new Vector2(350, 40);
        fsRowRect.anchoredPosition = Vector2.zero;
        var fsHlg = fsRow.AddComponent<HorizontalLayoutGroup>();
        fsHlg.spacing = 15;
        fsHlg.childAlignment = TextAnchor.MiddleCenter;
        fsHlg.childControlWidth = true;
        fsHlg.childControlHeight = true;
        fsHlg.childForceExpandHeight = true;

        var fsLabel = CreateText(fsRow.transform, "FullscreenLabel", "Fullscreen", 24, Color.white, 180);
        fsLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
        var fsLabelLE = fsLabel.AddComponent<LayoutElement>();
        fsLabelLE.preferredWidth = 180;

        var fsToggle = CreateInGameToggle("FullscreenToggle", fsRow.transform);

        // Back button
        var optBackBtn = CreatePauseButton(optRoot.transform, "OptionsBackButton", "BACK");
        var optBackRect = optBackBtn.GetComponent<RectTransform>();
        optBackRect.anchorMin = new Vector2(0.5f, 0.15f);
        optBackRect.anchorMax = new Vector2(0.5f, 0.15f);
        optBackRect.sizeDelta = new Vector2(250, 55);
        optBackRect.anchoredPosition = Vector2.zero;

        optRoot.SetActive(false);

        // Wire PauseMenu component
        var pauseMenu = canvasObj.AddComponent<PauseMenu>();
        var pmSO = new SerializedObject(pauseMenu);
        pmSO.FindProperty("pausePanel").objectReferenceValue = pauseRoot;
        pmSO.FindProperty("optionsPanel").objectReferenceValue = optRoot;
        pmSO.FindProperty("resumeButton").objectReferenceValue = pmResumeBtn.GetComponent<Button>();
        pmSO.FindProperty("optionsButton").objectReferenceValue = pmOptionsBtn.GetComponent<Button>();
        pmSO.FindProperty("mainMenuButton").objectReferenceValue = pmMainMenuBtn.GetComponent<Button>();
        pmSO.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider.GetComponent<Slider>();
        pmSO.FindProperty("sfxValueText").objectReferenceValue = sfxVal.GetComponent<TextMeshProUGUI>();
        pmSO.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider.GetComponent<Slider>();
        pmSO.FindProperty("musicValueText").objectReferenceValue = musicVal.GetComponent<TextMeshProUGUI>();
        pmSO.FindProperty("fullscreenToggle").objectReferenceValue = fsToggle.GetComponent<Toggle>();
        pmSO.FindProperty("optionsBackButton").objectReferenceValue = optBackBtn.GetComponent<Button>();
        pmSO.ApplyModifiedProperties();

        // ===== TOOLTIP =====
        var tooltipPanel = CreatePanel(canvasObj.transform, "TooltipPanel", new Color(0, 0, 0, 0.9f));
        var tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        tooltipRect.anchorMin = Vector2.zero;
        tooltipRect.anchorMax = Vector2.zero;
        tooltipRect.pivot = new Vector2(0, 1);
        tooltipRect.sizeDelta = new Vector2(200, 60);

        var csf = tooltipPanel.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var tooltipText = CreateText(tooltipPanel.transform, "TooltipText", "", 16, Color.white, 200);

        var tooltip = canvasObj.AddComponent<TooltipSystem>();
        var tipSO = new SerializedObject(tooltip);
        tipSO.FindProperty("tooltipPanel").objectReferenceValue = tooltipPanel;
        tipSO.FindProperty("tooltipText").objectReferenceValue = tooltipText.GetComponent<TextMeshProUGUI>();
        tipSO.ApplyModifiedProperties();

        tooltipPanel.SetActive(false);

        // ===== WAVE PREVIEW PANEL (Center-top, appears at dawn) =====
        var wavePreviewRoot = CreatePanel(canvasObj.transform, "WavePreviewPanel", new Color(0.05f, 0.02f, 0.0f, 0.85f));
        var wpRect = wavePreviewRoot.GetComponent<RectTransform>();
        wpRect.anchorMin = new Vector2(0.5f, 0.75f);
        wpRect.anchorMax = new Vector2(0.5f, 0.75f);
        wpRect.pivot = new Vector2(0.5f, 0.5f);
        wpRect.sizeDelta = new Vector2(400, 180);
        wpRect.anchoredPosition = Vector2.zero;

        var wpCG = wavePreviewRoot.AddComponent<CanvasGroup>();

        var wpVlg = wavePreviewRoot.AddComponent<VerticalLayoutGroup>();
        wpVlg.padding = new RectOffset(15, 15, 10, 10);
        wpVlg.spacing = 5;
        wpVlg.childAlignment = TextAnchor.UpperCenter;
        wpVlg.childControlWidth = true;
        wpVlg.childControlHeight = false;
        wpVlg.childForceExpandWidth = true;

        var wpHeader = CreateText(wavePreviewRoot.transform, "WavePreviewHeader", "DAY 2", 30, new Color(0.9f, 0.75f, 0.3f), 370);
        var wpHeaderTmp = wpHeader.GetComponent<TextMeshProUGUI>();
        wpHeaderTmp.alignment = TextAlignmentOptions.Center;
        wpHeaderTmp.fontStyle = FontStyles.Bold;
        var wpHeaderLE = wpHeader.AddComponent<LayoutElement>();
        wpHeaderLE.preferredHeight = 38;

        var wpBody = CreateText(wavePreviewRoot.transform, "WavePreviewBody", "", 18, new Color(0.85f, 0.85f, 0.8f), 370);
        var wpBodyTmp = wpBody.GetComponent<TextMeshProUGUI>();
        wpBodyTmp.alignment = TextAlignmentOptions.Center;
        wpBodyTmp.richText = true;
        var wpBodyLE = wpBody.AddComponent<LayoutElement>();
        wpBodyLE.preferredHeight = 120;

        wavePreviewRoot.SetActive(false);

        // Add WavePreviewUI component
        var wavePreview = canvasObj.AddComponent<WavePreviewUI>();
        var wpSO = new SerializedObject(wavePreview);
        wpSO.FindProperty("panelRoot").objectReferenceValue = wavePreviewRoot;
        wpSO.FindProperty("headerText").objectReferenceValue = wpHeaderTmp;
        wpSO.FindProperty("bodyText").objectReferenceValue = wpBodyTmp;
        wpSO.ApplyModifiedProperties();

        Debug.Log("UI built successfully!");
    }

    static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

static GameObject CreateText(Transform parent, string name, string text, int fontSize, Color color, float width)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, 40);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null) tmp.font = defaultFont;

        return go;
    }

static GameObject CreateUIButton(Transform parent, string name, string label, float w, float h)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(w, h);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.3f);
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.5f, 0.5f, 0.5f);
        btn.colors = colors;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(go.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null) tmp.font = defaultFont;

        return go;
    }

static GameObject CreateUpgradeButton(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180, 65);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.25f, 0.15f);
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.2f, 0.4f, 0.2f);
        colors.disabledColor = new Color(0.1f, 0.1f, 0.1f);
        btn.colors = colors;

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(go.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = new Vector2(5, 0);
        labelRect.offsetMax = new Vector2(-5, -3);
        var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = "Upgrade";
        labelTmp.fontSize = 16;
        labelTmp.color = Color.white;
        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null) labelTmp.font = defaultFont;

        var costObj = new GameObject("Cost");
        costObj.transform.SetParent(go.transform, false);
        var costRect = costObj.AddComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0, 0);
        costRect.anchorMax = new Vector2(1, 0.5f);
        costRect.offsetMin = new Vector2(5, 3);
        costRect.offsetMax = new Vector2(-5, 0);
        var costTmp = costObj.AddComponent<TextMeshProUGUI>();
        costTmp.text = "0g 0m";
        costTmp.fontSize = 14;
        costTmp.color = new Color(1, 0.85f, 0);
        if (defaultFont != null) costTmp.font = defaultFont;

        return go;
    }
    static GameObject CreatePauseButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(250, 55);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        colors.highlightedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        colors.pressedColor = new Color(0.6f, 0.4f, 0.1f, 1f);
        colors.selectedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        btn.colors = colors;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(go.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.9f, 0.8f, 0.5f);
        tmp.alignment = TextAlignmentOptions.Center;
        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null) tmp.font = defaultFont;

        return go;
    }

    static GameObject CreateInGameSlider(string name, Transform parent)
    {
        var sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        sliderObj.AddComponent<RectTransform>();

        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f);
        var bgR = bgObj.GetComponent<RectTransform>();
        bgR.anchorMin = new Vector2(0, 0.35f);
        bgR.anchorMax = new Vector2(1, 0.65f);
        bgR.offsetMin = Vector2.zero;
        bgR.offsetMax = Vector2.zero;

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.35f);
        fillAreaRect.anchorMax = new Vector2(1, 0.65f);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        var fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.7f, 0.55f, 0.2f);
        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        var handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = Vector2.zero;
        handleAreaRect.offsetMax = Vector2.zero;

        var handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleArea.transform, false);
        var handleImg = handleObj.AddComponent<Image>();
        handleImg.color = new Color(0.9f, 0.75f, 0.3f);
        var handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);
        handleRect.anchorMin = new Vector2(0, 0);
        handleRect.anchorMax = new Vector2(0, 1);

        var slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        slider.targetGraphic = handleImg;

        return sliderObj;
    }

    static GameObject CreateInGameToggle(string name, Transform parent)
    {
        var toggleObj = new GameObject(name);
        toggleObj.transform.SetParent(parent, false);
        var toggleLE = toggleObj.AddComponent<LayoutElement>();
        toggleLE.preferredWidth = 40;
        toggleLE.preferredHeight = 40;

        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(toggleObj.transform, false);
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.25f, 0.25f, 0.25f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(36, 36);

        var checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(bgObj.transform, false);
        var checkImg = checkObj.AddComponent<Image>();
        checkImg.color = new Color(0.9f, 0.75f, 0.3f);
        var checkRect = checkObj.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.15f, 0.15f);
        checkRect.anchorMax = new Vector2(0.85f, 0.85f);
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;

        var toggle = toggleObj.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        toggle.isOn = true;

        return toggleObj;
    }

    [MenuItem("Game/Build Options UI")]
    public static void BuildOptionsUI()
    {
        // Delete existing canvas
        var existingCanvas = GameObject.Find("Canvas");
        if (existingCanvas != null) Object.DestroyImmediate(existingCanvas);

        // Delete existing OptionsManager GO
        var existingMgr = GameObject.Find("OptionsManager");
        if (existingMgr != null) Object.DestroyImmediate(existingMgr);

        // Event System
        if (GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // Create Canvas
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Background
        var bg = CreatePanel(canvasObj.transform, "Background", new Color(0.08f, 0.05f, 0.02f, 1f));
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Title
        var title = CreateText(canvasObj.transform, "TitleText", "OPTIONS", 56, new Color(0.9f, 0.75f, 0.3f), 400);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.87f);
        titleRect.anchorMax = new Vector2(0.5f, 0.87f);
        titleRect.anchoredPosition = Vector2.zero;
        title.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Audio header
        var audioHeader = CreateText(canvasObj.transform, "AudioHeader", "AUDIO", 32, new Color(0.8f, 0.7f, 0.5f), 400);
        var audioHdrRect = audioHeader.GetComponent<RectTransform>();
        audioHdrRect.anchorMin = new Vector2(0.5f, 0.73f);
        audioHdrRect.anchorMax = new Vector2(0.5f, 0.73f);
        audioHdrRect.anchoredPosition = Vector2.zero;
        audioHeader.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        audioHeader.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // SFX volume row
        var sfxRow = new GameObject("SfxRow");
        sfxRow.transform.SetParent(canvasObj.transform, false);
        var sfxRowRect = sfxRow.AddComponent<RectTransform>();
        sfxRowRect.anchorMin = new Vector2(0.5f, 0.63f);
        sfxRowRect.anchorMax = new Vector2(0.5f, 0.63f);
        sfxRowRect.sizeDelta = new Vector2(700, 50);
        sfxRowRect.anchoredPosition = Vector2.zero;
        var sfxHlg = sfxRow.AddComponent<HorizontalLayoutGroup>();
        sfxHlg.spacing = 15;
        sfxHlg.childAlignment = TextAnchor.MiddleCenter;
        sfxHlg.childControlWidth = true;
        sfxHlg.childControlHeight = true;
        sfxHlg.childForceExpandHeight = true;

        var sfxLabel = CreateText(sfxRow.transform, "SfxLabel", "SFX Volume", 26, Color.white, 200);
        sfxLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
        var sfxLabelLE = sfxLabel.AddComponent<LayoutElement>();
        sfxLabelLE.preferredWidth = 200;

        var sfxSlider = CreateInGameSlider("SfxSlider", sfxRow.transform);
        var sfxSliderLE = sfxSlider.AddComponent<LayoutElement>();
        sfxSliderLE.preferredWidth = 350;
        sfxSliderLE.minHeight = 30;

        var sfxVal = CreateText(sfxRow.transform, "SfxValueText", "50%", 26, Color.white, 80);
        sfxVal.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
        var sfxValLE = sfxVal.AddComponent<LayoutElement>();
        sfxValLE.preferredWidth = 80;

        // Music volume row
        var musicRow = new GameObject("MusicRow");
        musicRow.transform.SetParent(canvasObj.transform, false);
        var musicRowRect = musicRow.AddComponent<RectTransform>();
        musicRowRect.anchorMin = new Vector2(0.5f, 0.53f);
        musicRowRect.anchorMax = new Vector2(0.5f, 0.53f);
        musicRowRect.sizeDelta = new Vector2(700, 50);
        musicRowRect.anchoredPosition = Vector2.zero;
        var musicHlg = musicRow.AddComponent<HorizontalLayoutGroup>();
        musicHlg.spacing = 15;
        musicHlg.childAlignment = TextAnchor.MiddleCenter;
        musicHlg.childControlWidth = true;
        musicHlg.childControlHeight = true;
        musicHlg.childForceExpandHeight = true;

        var musicLabel = CreateText(musicRow.transform, "MusicLabel", "Music Volume", 26, Color.white, 200);
        musicLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
        var musicLabelLE = musicLabel.AddComponent<LayoutElement>();
        musicLabelLE.preferredWidth = 200;

        var musicSlider = CreateInGameSlider("MusicSlider", musicRow.transform);
        var musicSliderLE = musicSlider.AddComponent<LayoutElement>();
        musicSliderLE.preferredWidth = 350;
        musicSliderLE.minHeight = 30;

        var musicVal = CreateText(musicRow.transform, "MusicValueText", "50%", 26, Color.white, 80);
        musicVal.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
        var musicValLE = musicVal.AddComponent<LayoutElement>();
        musicValLE.preferredWidth = 80;

        // Video header
        var videoHeader = CreateText(canvasObj.transform, "VideoHeader", "VIDEO", 32, new Color(0.8f, 0.7f, 0.5f), 400);
        var videoHdrRect = videoHeader.GetComponent<RectTransform>();
        videoHdrRect.anchorMin = new Vector2(0.5f, 0.40f);
        videoHdrRect.anchorMax = new Vector2(0.5f, 0.40f);
        videoHdrRect.anchoredPosition = Vector2.zero;
        videoHeader.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        videoHeader.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Fullscreen row
        var fsRow = new GameObject("FullscreenRow");
        fsRow.transform.SetParent(canvasObj.transform, false);
        var fsRowRect = fsRow.AddComponent<RectTransform>();
        fsRowRect.anchorMin = new Vector2(0.5f, 0.30f);
        fsRowRect.anchorMax = new Vector2(0.5f, 0.30f);
        fsRowRect.sizeDelta = new Vector2(400, 50);
        fsRowRect.anchoredPosition = Vector2.zero;
        var fsHlg = fsRow.AddComponent<HorizontalLayoutGroup>();
        fsHlg.spacing = 15;
        fsHlg.childAlignment = TextAnchor.MiddleCenter;
        fsHlg.childControlWidth = true;
        fsHlg.childControlHeight = true;
        fsHlg.childForceExpandHeight = true;

        var fsLabel = CreateText(fsRow.transform, "FullscreenLabel", "Fullscreen", 26, Color.white, 200);
        fsLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
        var fsLabelLE = fsLabel.AddComponent<LayoutElement>();
        fsLabelLE.preferredWidth = 200;

        var fsToggle = CreateInGameToggle("FullscreenToggle", fsRow.transform);

        // Back button
        var backBtn = CreatePauseButton(canvasObj.transform, "BackButton", "BACK");
        var backRect = backBtn.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0.10f);
        backRect.anchorMax = new Vector2(0.5f, 0.10f);
        backRect.sizeDelta = new Vector2(300, 60);
        backRect.anchoredPosition = Vector2.zero;

        // OptionsManager GO
        var mgrObj = new GameObject("OptionsManager");
        var optMgr = mgrObj.AddComponent<OptionsManager>();
        mgrObj.AddComponent<SceneLoader>();
        var optSO = new SerializedObject(optMgr);
        optSO.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider.GetComponent<Slider>();
        optSO.FindProperty("sfxValueText").objectReferenceValue = sfxVal.GetComponent<TextMeshProUGUI>();
        optSO.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider.GetComponent<Slider>();
        optSO.FindProperty("musicValueText").objectReferenceValue = musicVal.GetComponent<TextMeshProUGUI>();
        optSO.FindProperty("fullscreenToggle").objectReferenceValue = fsToggle.GetComponent<Toggle>();
        optSO.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
        optSO.ApplyModifiedProperties();

        Debug.Log("Options UI built successfully!");
    }
#endif
}
