using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// On-screen touch button overlay for mobile and hybrid touch devices.
/// Creates its own Canvas with high sort order. Respects Screen.safeArea.
/// Instantiated when PlatformDetector.ShowOnScreenControls is true.
/// </summary>
public class MobileControlsOverlay : MonoBehaviour
{
    public static MobileControlsOverlay Instance { get; private set; }

    // Send Menial toggle state
    public bool IsSendMenialMode { get; private set; }
    private float menialModeTimer;
    private const float MENIAL_MODE_TIMEOUT = 5f;

    // References
    private Canvas overlayCanvas;
    private RectTransform safeAreaRect;

    // Buttons
    private Button pauseButton;
    private Button buildButton;
    private Button upgradeButton;
    private Button recallButton;
    private Button switchBallistaButton;
    private Button sendMenialButton;
    private Image sendMenialImage;

    // Build mode only buttons
    private GameObject buildModeGroup;
    private Button rotateLeftButton;
    private Button rotateRightButton;
    private Button exitBuildButton;

    private bool subscribedToBuildMode;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Debug.Log("[MobileControlsOverlay] Instance created.");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (BuildModeManager.Instance != null)
        {
            BuildModeManager.Instance.OnBuildModeStarted -= OnBuildModeStarted;
            BuildModeManager.Instance.OnBuildModeEnded -= OnBuildModeEnded;
        }
    }

    private void Start()
    {
        CreateOverlayCanvas();
        CreateButtons();
        SubscribeToBuildMode();
    }

    private void Update()
    {
        if (!subscribedToBuildMode)
            SubscribeToBuildMode();

        // Send Menial timeout
        if (IsSendMenialMode)
        {
            menialModeTimer -= Time.deltaTime;
            if (menialModeTimer <= 0f)
            {
                DisableSendMenialMode();
            }
        }

        // Show/hide switch ballista button based on ballista count
        if (switchBallistaButton != null)
        {
            bool show = BallistaManager.Instance != null && BallistaManager.Instance.BallistaCount > 1;
            switchBallistaButton.gameObject.SetActive(show);
        }
    }

    private void SubscribeToBuildMode()
    {
        if (subscribedToBuildMode) return;
        if (BuildModeManager.Instance == null) return;
        BuildModeManager.Instance.OnBuildModeStarted += OnBuildModeStarted;
        BuildModeManager.Instance.OnBuildModeEnded += OnBuildModeEnded;
        subscribedToBuildMode = true;

        // Sync initial state
        if (buildModeGroup != null)
            buildModeGroup.SetActive(BuildModeManager.Instance.IsBuildMode);
    }

    private void OnBuildModeStarted()
    {
        if (buildModeGroup != null) buildModeGroup.SetActive(true);
        Debug.Log("[MobileControlsOverlay] Build mode buttons shown.");
    }

    private void OnBuildModeEnded()
    {
        if (buildModeGroup != null) buildModeGroup.SetActive(false);
        Debug.Log("[MobileControlsOverlay] Build mode buttons hidden.");
    }

    private void CreateOverlayCanvas()
    {
        var canvasObj = new GameObject("MobileControlsCanvas");
        canvasObj.transform.SetParent(transform, false);

        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 200; // Above GameHUD

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Safe area container
        var safeObj = new GameObject("SafeArea");
        safeObj.transform.SetParent(canvasObj.transform, false);
        safeAreaRect = safeObj.AddComponent<RectTransform>();
        safeAreaRect.anchorMin = Vector2.zero;
        safeAreaRect.anchorMax = Vector2.one;
        ApplySafeArea();

        Debug.Log("[MobileControlsOverlay] Canvas created with safe area.");
    }

    private void ApplySafeArea()
    {
        if (safeAreaRect == null) return;
        Rect safe = Screen.safeArea;
        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        safeAreaRect.anchorMin = anchorMin;
        safeAreaRect.anchorMax = anchorMax;
        safeAreaRect.offsetMin = Vector2.zero;
        safeAreaRect.offsetMax = Vector2.zero;
    }

    private void CreateButtons()
    {
        // Always visible buttons
        pauseButton = CreateButton("Pause", new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(10, -10), new Vector2(80, 80), OnPauseClicked);

        buildButton = CreateButton("Build", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 0f), new Vector2(-10, 170), new Vector2(100, 50), OnBuildClicked);

        upgradeButton = CreateButton("Upgrade", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 0f), new Vector2(-10, 110), new Vector2(100, 50), OnUpgradeClicked);

        recallButton = CreateButton("Recall", new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 0f), new Vector2(-10, 50), new Vector2(100, 50), OnRecallClicked);

        switchBallistaButton = CreateButton("Switch", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-10, -10), new Vector2(100, 50), OnSwitchBallistaClicked);
        switchBallistaButton.gameObject.SetActive(false);

        // Send Menial toggle (bottom-left)
        sendMenialButton = CreateButton("Menial", new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(0f, 0f), new Vector2(10, 50), new Vector2(100, 50), OnSendMenialClicked);
        sendMenialImage = sendMenialButton.GetComponent<Image>();

        // Build mode only buttons (bottom-center)
        var groupObj = new GameObject("BuildModeGroup");
        groupObj.transform.SetParent(safeAreaRect, false);
        var groupRect = groupObj.AddComponent<RectTransform>();
        groupRect.anchorMin = new Vector2(0.5f, 0f);
        groupRect.anchorMax = new Vector2(0.5f, 0f);
        groupRect.pivot = new Vector2(0.5f, 0f);
        groupRect.anchoredPosition = new Vector2(0, 10);
        groupRect.sizeDelta = new Vector2(350, 60);

        var hlg = groupObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        buildModeGroup = groupObj;

        rotateLeftButton = CreateButtonInParent("RotL", groupObj.transform, OnRotateLeftClicked);
        rotateRightButton = CreateButtonInParent("RotR", groupObj.transform, OnRotateRightClicked);
        exitBuildButton = CreateButtonInParent("Exit", groupObj.transform, OnExitBuildClicked);

        buildModeGroup.SetActive(false);

        Debug.Log("[MobileControlsOverlay] All buttons created.");
    }

    private Button CreateButton(string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var obj = new GameObject($"Btn_{label}");
        obj.transform.SetParent(safeAreaRect, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var img = obj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.65f);

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        // Label text
        var textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }

    private Button CreateButtonInParent(string label, Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        var obj = new GameObject($"Btn_{label}");
        obj.transform.SetParent(parent, false);

        var le = obj.AddComponent<LayoutElement>();
        le.minHeight = 50;
        le.minWidth = 80;

        var img = obj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.65f);

        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var textObj = new GameObject("Label");
        textObj.transform.SetParent(obj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }

    // Button handlers

    private void OnPauseClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TogglePause();
        Debug.Log("[MobileControlsOverlay] Pause clicked.");
    }

    private void OnBuildClicked()
    {
        if (BuildModeManager.Instance == null) return;
        if (BuildModeManager.Instance.IsBuildMode)
            BuildModeManager.Instance.ExitBuildMode();
        else
            BuildModeManager.Instance.EnterBuildMode();
        Debug.Log("[MobileControlsOverlay] Build clicked.");
    }

    private void OnUpgradeClicked()
    {
        var panel = FindAnyObjectByType<UpgradePanel>();
        if (panel != null)
            panel.TogglePanel();
        Debug.Log("[MobileControlsOverlay] Upgrade clicked.");
    }

    private void OnRecallClicked()
    {
        if (RecallManager.Instance != null)
            RecallManager.Instance.RecallAll();
        Debug.Log("[MobileControlsOverlay] Recall clicked.");
    }

    private void OnSwitchBallistaClicked()
    {
        if (BallistaManager.Instance != null)
            BallistaManager.Instance.CycleActiveBallista();
        Debug.Log("[MobileControlsOverlay] Switch ballista clicked.");
    }

    private void OnSendMenialClicked()
    {
        if (IsSendMenialMode)
        {
            DisableSendMenialMode();
        }
        else
        {
            IsSendMenialMode = true;
            menialModeTimer = MENIAL_MODE_TIMEOUT;
            if (sendMenialImage != null)
                sendMenialImage.color = new Color(0.7f, 0.6f, 0.1f, 0.8f); // Gold highlight
            Debug.Log("[MobileControlsOverlay] Send Menial mode ENABLED.");
        }
    }

    /// <summary>Disable send menial mode after successful tap or timeout.</summary>
    public void DisableSendMenialMode()
    {
        IsSendMenialMode = false;
        if (sendMenialImage != null)
            sendMenialImage.color = new Color(0.15f, 0.15f, 0.15f, 0.65f);
        Debug.Log("[MobileControlsOverlay] Send Menial mode disabled.");
    }

    private void OnRotateLeftClicked()
    {
        // Simulate rotate left — WallPlacement reads InputBindingManager, so fire the action
        // We set a one-frame flag via a helper
        if (InputBindingManager.Instance != null)
        {
            var wallPlacement = FindAnyObjectByType<WallPlacement>();
            if (wallPlacement != null)
                SendRotateAction(-45f);
        }
        Debug.Log("[MobileControlsOverlay] Rotate left clicked.");
    }

    private void OnRotateRightClicked()
    {
        if (InputBindingManager.Instance != null)
        {
            var wallPlacement = FindAnyObjectByType<WallPlacement>();
            if (wallPlacement != null)
                SendRotateAction(45f);
        }
        Debug.Log("[MobileControlsOverlay] Rotate right clicked.");
    }

    /// <summary>
    /// Directly rotate the ghost wall by the given degrees.
    /// WallPlacement tracks ghostRotationY privately, but the overlay works via
    /// InputBindingManager keybindings which WallPlacement already polls.
    /// Since we can't inject key presses, we expose a public rotate method instead.
    /// </summary>
    private void SendRotateAction(float degrees)
    {
        // WallPlacement polls InputBindingManager.WasPressedThisFrame in its Update,
        // but on-screen buttons fire onClick at a different timing.
        // Instead, call a public method on WallPlacement.
        var wp = FindAnyObjectByType<WallPlacement>();
        if (wp != null)
            wp.RotateGhost(degrees);
    }

    private void OnExitBuildClicked()
    {
        if (BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildMode)
            BuildModeManager.Instance.ExitBuildMode();
        Debug.Log("[MobileControlsOverlay] Exit build clicked.");
    }
}
