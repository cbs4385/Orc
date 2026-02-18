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
            bgImage.preserveAspect = false;
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

        // Button panel
        var panelObj = new GameObject("ButtonPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.15f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 300);
        panelRect.anchoredPosition = Vector2.zero;
        var vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;

        // Buttons
        var playBtn = CreateMenuButton("PlayButton", "PLAY", panelObj.transform);
        var optionsBtn = CreateMenuButton("OptionsButton", "OPTIONS", panelObj.transform);
        var exitBtn = CreateMenuButton("ExitButton", "EXIT", panelObj.transform);

        // Manager object
        var mgrObj = new GameObject("MenuManager");
        var mainMenu = mgrObj.AddComponent<MainMenuManager>();
        mgrObj.AddComponent<SceneLoader>();

        // Wire buttons
        var mmSO = new SerializedObject(mainMenu);
        mmSO.FindProperty("playButton").objectReferenceValue = playBtn;
        mmSO.FindProperty("optionsButton").objectReferenceValue = optionsBtn;
        mmSO.FindProperty("exitButton").objectReferenceValue = exitBtn;
        mmSO.ApplyModifiedProperties();

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

        // --- Video section ---
        var videoHeader = CreateLabel("VideoHeader", "VIDEO", canvasObj.transform,
            new Vector2(0.5f, 0.48f), 36, new Color(0.8f, 0.7f, 0.5f));

        // Fullscreen toggle row
        var fsRow = new GameObject("FullscreenRow");
        fsRow.transform.SetParent(canvasObj.transform, false);
        var fsRowRect = fsRow.AddComponent<RectTransform>();
        fsRowRect.anchorMin = new Vector2(0.5f, 0.38f);
        fsRowRect.anchorMax = new Vector2(0.5f, 0.38f);
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
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[MenuSceneBuilder] Build Settings updated: MainMenu (0), GameScene (1), Options (2).");
    }
}
#endif
