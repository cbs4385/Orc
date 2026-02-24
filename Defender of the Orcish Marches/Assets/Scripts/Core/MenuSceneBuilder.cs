using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class MenuSceneBuilder : MonoBehaviour
{
    [MenuItem("Game/Build Menu Scenes")]
    public static void BuildMenuScenes()
    {
        // Save current scene first
        EditorSceneManager.SaveOpenScenes();

        // Rename SampleScene to GameScene if needed
        string oldPath = "Assets/Scenes/SampleScene.unity";
        string newPath = "Assets/Scenes/GameScene.unity";
        if (AssetDatabase.LoadAssetAtPath<Object>(oldPath) != null &&
            AssetDatabase.LoadAssetAtPath<Object>(newPath) == null)
        {
            string result = AssetDatabase.RenameAsset(oldPath, "GameScene");
            if (string.IsNullOrEmpty(result))
                Debug.Log("[MenuSceneBuilder] Renamed SampleScene → GameScene.");
            else
                Debug.LogWarning($"[MenuSceneBuilder] Could not rename SampleScene: {result}");
        }

        EnsureSplashIsSprite();
        BuildMainMenuScene();
        BuildOptionsScene();
        SetupBuildSettings();

        Debug.Log("[MenuSceneBuilder] All menu scenes built successfully!");
    }

    static void EnsureSplashIsSprite()
    {
        string splashPath = "Assets/UI/TitleScreen/splash.png";
        var importer = AssetImporter.GetAtPath(splashPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            Debug.Log("[MenuSceneBuilder] Set splash.png texture type to Sprite.");
        }
    }

    static void BuildMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camObj = new GameObject("Main Camera");
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        camObj.AddComponent<AudioListener>();
        camObj.tag = "MainCamera";

        // Canvas
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Background image (splash.png)
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bgImage = bgObj.AddComponent<Image>();
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Load splash image — try Sprite first, then Texture2D fallback
        string splashPath = "Assets/UI/TitleScreen/splash.png";
        var splashSprite = AssetDatabase.LoadAssetAtPath<Sprite>(splashPath);
        if (splashSprite == null)
        {
            // Sprite sub-asset might not exist yet; try loading all assets
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(splashPath);
            foreach (var asset in allAssets)
            {
                if (asset is Sprite s)
                {
                    splashSprite = s;
                    break;
                }
            }
            Debug.Log($"[MenuSceneBuilder] LoadAllAssetsAtPath found {allAssets.Length} assets at {splashPath}.");
        }
        if (splashSprite == null)
        {
            // Last resort: load as Texture2D and create sprite at runtime won't persist,
            // so just check if the texture exists at all
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(splashPath);
            Debug.Log($"[MenuSceneBuilder] Texture2D load: {(tex != null ? tex.name : "null")}");
            if (tex != null)
            {
                // Force the importer to Sprite and reimport
                var imp = AssetImporter.GetAtPath(splashPath) as TextureImporter;
                if (imp != null)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.SaveAndReimport();
                    AssetDatabase.Refresh();
                    splashSprite = AssetDatabase.LoadAssetAtPath<Sprite>(splashPath);
                    Debug.Log($"[MenuSceneBuilder] After reimport, sprite: {(splashSprite != null ? "found" : "null")}");
                }
            }
        }
        if (splashSprite != null)
        {
            bgImage.sprite = splashSprite;
            bgImage.preserveAspect = true;
            Debug.Log("[MenuSceneBuilder] Splash image loaded successfully.");
        }
        else
        {
            bgImage.color = new Color(0.15f, 0.1f, 0.05f);
            Debug.LogWarning("[MenuSceneBuilder] splash.png could not be loaded as Sprite — using fallback color.");
        }

        // Semi-transparent overlay for button readability
        var overlayObj = new GameObject("Overlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        var overlayImage = overlayObj.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 0.4f);
        var overlayRect = overlayObj.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // Title text
        var titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(canvasObj.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "DEFENDER OF THE\nORCISH MARCHES";
        titleTmp.fontSize = 72;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.9f, 0.75f, 0.3f); // gold
        titleTmp.alignment = TextAlignmentOptions.Center;
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.9f);
        titleRect.sizeDelta = new Vector2(1200, 200);
        titleRect.anchoredPosition = Vector2.zero;

        // Button panel (vertical column on the left side)
        var panelObj = new GameObject("ButtonPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.08f);
        panelRect.anchorMax = new Vector2(0f, 0.88f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.sizeDelta = new Vector2(220, 0);
        panelRect.anchoredPosition = new Vector2(20, 0);
        var vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;

        // Main buttons on left
        var playBtn = CreateMenuButton("PlayButton", "PLAY", panelObj.transform);
        var tutorialBtn = CreateMenuButton("TutorialButton", "TUTORIAL", panelObj.transform);
        var optionsBtn = CreateMenuButton("OptionsButton", "OPTIONS", panelObj.transform);
        var exitBtn = CreateMenuButton("ExitButton", "EXIT", panelObj.transform);

        // Stats button (below main buttons in the left panel)
        var statsBtn = CreateMenuButton("StatsButton", "STATISTICS", panelObj.transform);

        // Mutators button
        var mutatorsBtn = CreateMenuButton("MutatorsButton", "MUTATORS", panelObj.transform);

        // Bug report button (top-right corner)
        var bugReportBtn = CreateMenuButton("BugReportButton", "REPORT BUG", canvasObj.transform);
        var bugRect = bugReportBtn.GetComponent<RectTransform>();
        bugRect.anchorMin = new Vector2(1f, 1f);
        bugRect.anchorMax = new Vector2(1f, 1f);
        bugRect.pivot = new Vector2(1f, 1f);
        bugRect.sizeDelta = new Vector2(200, 50);
        bugRect.anchoredPosition = new Vector2(-20, -10);

        // --- Difficulty selector (vertical, right edge) ---
        var diffPanel = new GameObject("DifficultyPanel");
        diffPanel.transform.SetParent(canvasObj.transform, false);
        var diffPanelRect = diffPanel.AddComponent<RectTransform>();
        diffPanelRect.anchorMin = new Vector2(1f, 0.08f);
        diffPanelRect.anchorMax = new Vector2(1f, 0.88f);
        diffPanelRect.pivot = new Vector2(1f, 0.5f);
        diffPanelRect.sizeDelta = new Vector2(180, 0);
        diffPanelRect.anchoredPosition = new Vector2(-10, 0);

        // "DIFFICULTY" header at top
        var diffHeaderObj = new GameObject("DifficultyHeader");
        diffHeaderObj.transform.SetParent(diffPanel.transform, false);
        var diffHeaderTmp = diffHeaderObj.AddComponent<TextMeshProUGUI>();
        diffHeaderTmp.text = "DIFFICULTY";
        diffHeaderTmp.fontSize = 26;
        diffHeaderTmp.fontStyle = FontStyles.Bold;
        diffHeaderTmp.color = new Color(0.9f, 0.75f, 0.3f);
        diffHeaderTmp.alignment = TextAlignmentOptions.Center;
        diffHeaderObj.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);
        var diffHeaderRect = diffHeaderObj.GetComponent<RectTransform>();
        diffHeaderRect.anchorMin = new Vector2(0f, 0.92f);
        diffHeaderRect.anchorMax = new Vector2(1f, 1f);
        diffHeaderRect.offsetMin = Vector2.zero;
        diffHeaderRect.offsetMax = Vector2.zero;

        // Build vertical slider manually (CreateSlider makes a horizontal layout)
        var diffSliderObj = new GameObject("DifficultySlider");
        diffSliderObj.transform.SetParent(diffPanel.transform, false);
        var diffSliderRect = diffSliderObj.AddComponent<RectTransform>();
        // Slider occupies center column, full vertical span between labels
        diffSliderRect.anchorMin = new Vector2(0.55f, 0.05f);
        diffSliderRect.anchorMax = new Vector2(0.75f, 0.90f);
        diffSliderRect.offsetMin = Vector2.zero;
        diffSliderRect.offsetMax = Vector2.zero;

        // Slider background (thin vertical bar)
        var slBg = new GameObject("Background");
        slBg.transform.SetParent(diffSliderObj.transform, false);
        var slBgImg = slBg.AddComponent<Image>();
        slBgImg.color = new Color(0.2f, 0.2f, 0.2f);
        var slBgRect = slBg.GetComponent<RectTransform>();
        slBgRect.anchorMin = new Vector2(0.3f, 0f);
        slBgRect.anchorMax = new Vector2(0.7f, 1f);
        slBgRect.offsetMin = Vector2.zero;
        slBgRect.offsetMax = Vector2.zero;

        // Fill area (vertical)
        var slFillArea = new GameObject("Fill Area");
        slFillArea.transform.SetParent(diffSliderObj.transform, false);
        var slFillAreaRect = slFillArea.AddComponent<RectTransform>();
        slFillAreaRect.anchorMin = new Vector2(0.3f, 0f);
        slFillAreaRect.anchorMax = new Vector2(0.7f, 1f);
        slFillAreaRect.offsetMin = Vector2.zero;
        slFillAreaRect.offsetMax = Vector2.zero;

        var slFill = new GameObject("Fill");
        slFill.transform.SetParent(slFillArea.transform, false);
        var slFillImg = slFill.AddComponent<Image>();
        slFillImg.color = new Color(0.7f, 0.55f, 0.2f);
        var slFillRect = slFill.GetComponent<RectTransform>();
        slFillRect.anchorMin = Vector2.zero;
        slFillRect.anchorMax = Vector2.one;
        slFillRect.offsetMin = Vector2.zero;
        slFillRect.offsetMax = Vector2.zero;

        // Handle slide area (full rect)
        var slHandleArea = new GameObject("Handle Slide Area");
        slHandleArea.transform.SetParent(diffSliderObj.transform, false);
        var slHandleAreaRect = slHandleArea.AddComponent<RectTransform>();
        slHandleAreaRect.anchorMin = Vector2.zero;
        slHandleAreaRect.anchorMax = Vector2.one;
        slHandleAreaRect.offsetMin = Vector2.zero;
        slHandleAreaRect.offsetMax = Vector2.zero;

        // Handle (wide horizontal bar for vertical slider)
        var slHandle = new GameObject("Handle");
        slHandle.transform.SetParent(slHandleArea.transform, false);
        var slHandleImg = slHandle.AddComponent<Image>();
        slHandleImg.color = new Color(0.9f, 0.75f, 0.3f);
        var slHandleRect = slHandle.GetComponent<RectTransform>();
        slHandleRect.anchorMin = new Vector2(0f, 0f);
        slHandleRect.anchorMax = new Vector2(1f, 0f);
        slHandleRect.sizeDelta = new Vector2(0, 16);

        // Wire Slider component
        var diffSlider = diffSliderObj.AddComponent<Slider>();
        diffSlider.direction = Slider.Direction.BottomToTop;
        diffSlider.fillRect = slFillRect;
        diffSlider.handleRect = slHandleRect;
        diffSlider.targetGraphic = slHandleImg;
        diffSlider.minValue = 0;
        diffSlider.maxValue = 3;
        diffSlider.wholeNumbers = true;
        diffSlider.value = 1; // Normal

        // Difficulty level labels to the left of the slider with drop shadows
        string[] diffNames = { "Easy", "Normal", "Hard", "Nightmare" };
        float sliderBottom = 0.05f;
        float sliderTop = 0.90f;
        float sliderRange = sliderTop - sliderBottom;
        for (int i = 0; i < diffNames.Length; i++)
        {
            float t = i / 3f; // 0, 0.333, 0.667, 1.0
            float anchorY = sliderBottom + t * sliderRange;

            var tickObj = new GameObject($"DiffLabel_{diffNames[i]}");
            tickObj.transform.SetParent(diffPanel.transform, false);
            var tickTmp = tickObj.AddComponent<TextMeshProUGUI>();
            tickTmp.text = diffNames[i];
            tickTmp.fontSize = 24;
            tickTmp.fontStyle = FontStyles.Bold;
            tickTmp.color = Color.white;
            tickTmp.alignment = TextAlignmentOptions.MidlineRight;
            // Drop shadow for contrast
            var shadow = tickObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.9f);
            shadow.effectDistance = new Vector2(2, -2);
            var tickRect = tickObj.GetComponent<RectTransform>();
            tickRect.anchorMin = new Vector2(0f, anchorY);
            tickRect.anchorMax = new Vector2(0.5f, anchorY);
            tickRect.sizeDelta = new Vector2(0, 30);
            tickRect.anchoredPosition = Vector2.zero;
        }

        // Current difficulty value text below everything
        var diffValObj = new GameObject("DifficultyValueText");
        diffValObj.transform.SetParent(diffPanel.transform, false);
        var diffValTmp = diffValObj.AddComponent<TextMeshProUGUI>();
        diffValTmp.text = "Normal";
        diffValTmp.fontSize = 24;
        diffValTmp.fontStyle = FontStyles.Bold;
        diffValTmp.color = new Color(0.9f, 0.75f, 0.3f);
        diffValTmp.alignment = TextAlignmentOptions.Center;
        diffValObj.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);
        var diffValRect = diffValObj.GetComponent<RectTransform>();
        diffValRect.anchorMin = new Vector2(0f, 0f);
        diffValRect.anchorMax = new Vector2(1f, 0.05f);
        diffValRect.offsetMin = Vector2.zero;
        diffValRect.offsetMax = Vector2.zero;

        // LogCapture (DontDestroyOnLoad singleton — persists across scenes)
        var logCaptureObj = new GameObject("LogCapture");
        logCaptureObj.AddComponent<LogCapture>();

        // Manager object
        var mgrObj = new GameObject("MenuManager");
        var mainMenu = mgrObj.AddComponent<MainMenuManager>();
        mgrObj.AddComponent<SceneLoader>();
        var bugReportPanel = mgrObj.AddComponent<BugReportPanel>();
        var statsDashboardPanel = mgrObj.AddComponent<StatsDashboardPanel>();
        var mutatorUI = mgrObj.AddComponent<MutatorUI>();

        // Load BugReportConfig asset
        var bugReportConfig = AssetDatabase.LoadAssetAtPath<BugReportConfig>("Assets/ScriptableObjects/BugReportConfig.asset");
        if (bugReportConfig == null)
            Debug.LogWarning("[MenuSceneBuilder] BugReportConfig.asset not found at Assets/ScriptableObjects/. Create it via Create > Game > Bug Report Config.");

        // Wire buttons + difficulty + bug report
        var mmSO = new SerializedObject(mainMenu);
        mmSO.FindProperty("playButton").objectReferenceValue = playBtn;
        mmSO.FindProperty("optionsButton").objectReferenceValue = optionsBtn;
        mmSO.FindProperty("tutorialButton").objectReferenceValue = tutorialBtn;
        mmSO.FindProperty("exitButton").objectReferenceValue = exitBtn;
        mmSO.FindProperty("statsButton").objectReferenceValue = statsBtn;
        mmSO.FindProperty("statsDashboardPanel").objectReferenceValue = statsDashboardPanel;
        mmSO.FindProperty("mutatorsButton").objectReferenceValue = mutatorsBtn;
        mmSO.FindProperty("mutatorUI").objectReferenceValue = mutatorUI;
        mmSO.FindProperty("bugReportButton").objectReferenceValue = bugReportBtn;
        mmSO.FindProperty("bugReportPanel").objectReferenceValue = bugReportPanel;
        mmSO.FindProperty("difficultySlider").objectReferenceValue = diffSlider;
        mmSO.FindProperty("difficultyLabel").objectReferenceValue = diffValTmp;
        mmSO.ApplyModifiedProperties();

        // Wire BugReportPanel config
        var brSO = new SerializedObject(bugReportPanel);
        brSO.FindProperty("config").objectReferenceValue = bugReportConfig;
        brSO.ApplyModifiedProperties();

        // EventSystem
        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        Debug.Log("[MenuSceneBuilder] MainMenu scene created.");
    }

    static void BuildOptionsScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camObj = new GameObject("Main Camera");
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.08f, 0.05f);
        cam.orthographic = true;
        camObj.AddComponent<AudioListener>();
        camObj.tag = "MainCamera";

        // Canvas
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Dark background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.1f, 0.08f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Title
        var titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(canvasObj.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "OPTIONS";
        titleTmp.fontSize = 56;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.9f, 0.75f, 0.3f);
        titleTmp.alignment = TextAlignmentOptions.Center;
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.85f);
        titleRect.anchorMax = new Vector2(0.5f, 0.95f);
        titleRect.sizeDelta = new Vector2(600, 80);
        titleRect.anchoredPosition = Vector2.zero;

        // --- Audio section ---
        var audioHeader = CreateLabel("AudioHeader", "AUDIO", canvasObj.transform,
            new Vector2(0.5f, 0.72f), 36, new Color(0.8f, 0.7f, 0.5f));

        // SFX Volume row
        var sfxRow = new GameObject("SfxRow");
        sfxRow.transform.SetParent(canvasObj.transform, false);
        var sfxRowRect = sfxRow.AddComponent<RectTransform>();
        sfxRowRect.anchorMin = new Vector2(0.5f, 0.6f);
        sfxRowRect.anchorMax = new Vector2(0.5f, 0.6f);
        sfxRowRect.sizeDelta = new Vector2(700, 50);
        sfxRowRect.anchoredPosition = Vector2.zero;
        var sfxHlg = sfxRow.AddComponent<HorizontalLayoutGroup>();
        sfxHlg.spacing = 15;
        sfxHlg.childAlignment = TextAnchor.MiddleCenter;
        sfxHlg.childControlWidth = true;
        sfxHlg.childControlHeight = true;
        sfxHlg.childForceExpandHeight = true;

        // Label
        var sfxLabelObj = new GameObject("SfxLabel");
        sfxLabelObj.transform.SetParent(sfxRow.transform, false);
        var sfxLabelTmp = sfxLabelObj.AddComponent<TextMeshProUGUI>();
        sfxLabelTmp.text = "SFX Volume";
        sfxLabelTmp.fontSize = 28;
        sfxLabelTmp.color = Color.white;
        sfxLabelTmp.alignment = TextAlignmentOptions.MidlineRight;
        var sfxLabelLE = sfxLabelObj.AddComponent<LayoutElement>();
        sfxLabelLE.preferredWidth = 200;

        // Slider
        var sfxSliderObj = CreateSlider("SfxVolumeSlider", sfxRow.transform);
        var sfxSliderLE = sfxSliderObj.AddComponent<LayoutElement>();
        sfxSliderLE.preferredWidth = 350;
        sfxSliderLE.minHeight = 30;

        // Value text
        var sfxValObj = new GameObject("SfxValueText");
        sfxValObj.transform.SetParent(sfxRow.transform, false);
        var sfxValTmp = sfxValObj.AddComponent<TextMeshProUGUI>();
        sfxValTmp.text = "50%";
        sfxValTmp.fontSize = 28;
        sfxValTmp.color = Color.white;
        sfxValTmp.alignment = TextAlignmentOptions.MidlineLeft;
        var sfxValLE = sfxValObj.AddComponent<LayoutElement>();
        sfxValLE.preferredWidth = 80;

        // Music Volume row
        var musicRow = new GameObject("MusicRow");
        musicRow.transform.SetParent(canvasObj.transform, false);
        var musicRowRect = musicRow.AddComponent<RectTransform>();
        musicRowRect.anchorMin = new Vector2(0.5f, 0.52f);
        musicRowRect.anchorMax = new Vector2(0.5f, 0.52f);
        musicRowRect.sizeDelta = new Vector2(700, 50);
        musicRowRect.anchoredPosition = Vector2.zero;
        var musicHlg = musicRow.AddComponent<HorizontalLayoutGroup>();
        musicHlg.spacing = 15;
        musicHlg.childAlignment = TextAnchor.MiddleCenter;
        musicHlg.childControlWidth = true;
        musicHlg.childControlHeight = true;
        musicHlg.childForceExpandHeight = true;

        var musicLabelObj = new GameObject("MusicLabel");
        musicLabelObj.transform.SetParent(musicRow.transform, false);
        var musicLabelTmp = musicLabelObj.AddComponent<TextMeshProUGUI>();
        musicLabelTmp.text = "Music Volume";
        musicLabelTmp.fontSize = 28;
        musicLabelTmp.color = Color.white;
        musicLabelTmp.alignment = TextAlignmentOptions.MidlineRight;
        var musicLabelLE = musicLabelObj.AddComponent<LayoutElement>();
        musicLabelLE.preferredWidth = 200;

        var musicSliderObj = CreateSlider("MusicVolumeSlider", musicRow.transform);
        var musicSliderLE = musicSliderObj.AddComponent<LayoutElement>();
        musicSliderLE.preferredWidth = 350;
        musicSliderLE.minHeight = 30;

        var musicValObj = new GameObject("MusicValueText");
        musicValObj.transform.SetParent(musicRow.transform, false);
        var musicValTmp = musicValObj.AddComponent<TextMeshProUGUI>();
        musicValTmp.text = "50%";
        musicValTmp.fontSize = 28;
        musicValTmp.color = Color.white;
        musicValTmp.alignment = TextAlignmentOptions.MidlineLeft;
        var musicValLE = musicValObj.AddComponent<LayoutElement>();
        musicValLE.preferredWidth = 80;

        // --- Video section ---
        var videoHeader = CreateLabel("VideoHeader", "VIDEO", canvasObj.transform,
            new Vector2(0.5f, 0.38f), 36, new Color(0.8f, 0.7f, 0.5f));

        // Fullscreen toggle row
        var fsRow = new GameObject("FullscreenRow");
        fsRow.transform.SetParent(canvasObj.transform, false);
        var fsRowRect = fsRow.AddComponent<RectTransform>();
        fsRowRect.anchorMin = new Vector2(0.5f, 0.28f);
        fsRowRect.anchorMax = new Vector2(0.5f, 0.28f);
        fsRowRect.sizeDelta = new Vector2(400, 50);
        fsRowRect.anchoredPosition = Vector2.zero;
        var fsHlg = fsRow.AddComponent<HorizontalLayoutGroup>();
        fsHlg.spacing = 15;
        fsHlg.childAlignment = TextAnchor.MiddleCenter;
        fsHlg.childControlWidth = true;
        fsHlg.childControlHeight = true;
        fsHlg.childForceExpandHeight = true;

        var fsLabelObj = new GameObject("FullscreenLabel");
        fsLabelObj.transform.SetParent(fsRow.transform, false);
        var fsLabelTmp = fsLabelObj.AddComponent<TextMeshProUGUI>();
        fsLabelTmp.text = "Fullscreen";
        fsLabelTmp.fontSize = 28;
        fsLabelTmp.color = Color.white;
        fsLabelTmp.alignment = TextAlignmentOptions.MidlineRight;
        var fsLabelLE = fsLabelObj.AddComponent<LayoutElement>();
        fsLabelLE.preferredWidth = 200;

        var fsToggleObj = CreateToggle("FullscreenToggle", fsRow.transform);

        // --- Back button ---
        var backBtnObj = CreateMenuButton("BackButton", "BACK", canvasObj.transform);
        var backRect = backBtnObj.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0.1f);
        backRect.anchorMax = new Vector2(0.5f, 0.1f);
        backRect.sizeDelta = new Vector2(300, 60);
        backRect.anchoredPosition = Vector2.zero;

        // Manager object
        var mgrObj = new GameObject("OptionsManager");
        var optMgr = mgrObj.AddComponent<OptionsManager>();
        mgrObj.AddComponent<SceneLoader>();

        // Wire references
        var optSO = new SerializedObject(optMgr);
        optSO.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSliderObj.GetComponent<Slider>();
        optSO.FindProperty("sfxValueText").objectReferenceValue = sfxValTmp;
        optSO.FindProperty("musicVolumeSlider").objectReferenceValue = musicSliderObj.GetComponent<Slider>();
        optSO.FindProperty("musicValueText").objectReferenceValue = musicValTmp;
        optSO.FindProperty("fullscreenToggle").objectReferenceValue = fsToggleObj.GetComponent<Toggle>();
        optSO.FindProperty("backButton").objectReferenceValue = backBtnObj.GetComponent<Button>();
        optSO.ApplyModifiedProperties();

        // EventSystem
        var esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Options.unity");
        Debug.Log("[MenuSceneBuilder] Options scene created.");
    }

    static Button CreateMenuButton(string name, string label, Transform parent)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        var btn = btnObj.AddComponent<Button>();

        var colors = btn.colors;
        colors.normalColor = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        colors.highlightedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        colors.pressedColor = new Color(0.6f, 0.4f, 0.1f, 1f);
        colors.selectedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        btn.colors = colors;

        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 32;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.9f, 0.8f, 0.5f);
        tmp.alignment = TextAlignmentOptions.Center;
        var txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        // Drop shadow
        var shadow = txtObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(2f, -2f);

        return btn;
    }

    static GameObject CreateLabel(string name, string text, Transform parent, Vector2 anchor, float fontSize, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = new Vector2(400, 50);
        rect.anchoredPosition = Vector2.zero;
        return obj;
    }

    static GameObject CreateSlider(string name, Transform parent)
    {
        var sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        var sliderRect = sliderObj.AddComponent<RectTransform>();

        // Background
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        var bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f);
        var bgR = bgObj.GetComponent<RectTransform>();
        bgR.anchorMin = new Vector2(0, 0.35f);
        bgR.anchorMax = new Vector2(1, 0.65f);
        bgR.offsetMin = Vector2.zero;
        bgR.offsetMax = Vector2.zero;

        // Fill Area
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

        // Handle slide area
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

        // Setup Slider component
        var slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        slider.targetGraphic = handleImg;

        return sliderObj;
    }

    static GameObject CreateToggle(string name, Transform parent)
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

    static void SetupBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Options.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/TutorialScene.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[MenuSceneBuilder] Build Settings updated: MainMenu (0), GameScene (1), Options (2), TutorialScene (3).");
    }
}
#endif
