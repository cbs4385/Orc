using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    [Header("Top Bar")]
    [SerializeField] private TextMeshProUGUI treasureText;
    [SerializeField] private TextMeshProUGUI menialText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI defenderCountText;

    [Header("Pause")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Image pauseButtonIcon;

    private bool subscribed;
    private Sprite pauseSprite;
    private Sprite playSprite;

    // Notification banner
    private GameObject bannerRoot;
    private TextMeshProUGUI bannerText;
    private float bannerTimer;
    private bool bannerAutoHide;

    // Build mode panel (top-right modal)
    private GameObject buildPanelRoot;
    private TextMeshProUGUI buildPanelCostText;
    private TextMeshProUGUI buildPanelGoldText;

    private void Awake()
    {
        Instance = this;
        CreateBannerUI();
        CreateBuildPanel();
    }

    private void Start()
    {
        GenerateIcons();
        TrySubscribe();
    }

    private void OnEnable()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);
    }

    private void OnDisable()
    {
        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(OnPauseClicked);
    }

    private void TrySubscribe()
    {
        if (!subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnTreasureChanged += UpdateTreasure;
            GameManager.Instance.OnMenialsChanged += UpdateMenials;
            GameManager.Instance.OnKillsChanged += UpdateKills;
            GameManager.Instance.OnPauseChanged += UpdatePauseButton;
            subscribed = true;

            // Force initial display update
            UpdateTreasure(GameManager.Instance.Treasure);
            UpdateMenials(GameManager.Instance.MenialCount);
            UpdateKills(GameManager.Instance.EnemyKills);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTreasureChanged -= UpdateTreasure;
            GameManager.Instance.OnMenialsChanged -= UpdateMenials;
            GameManager.Instance.OnKillsChanged -= UpdateKills;
            GameManager.Instance.OnPauseChanged -= UpdatePauseButton;
        }
    }

    private void Update()
    {
        if (!subscribed) TrySubscribe();
        if (GameManager.Instance == null) return;

        // Pause toggle
        if (InputBindingManager.Instance != null && InputBindingManager.Instance.WasPressedThisFrame(GameAction.Pause))
        {
            GameManager.Instance.TogglePause();
        }

        // Update timer
        if (timerText != null)
        {
            float time = GameManager.Instance.GameTime;
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            bool inBuild = BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildMode;
            bool idleSpeedup = BuildModeManager.Instance != null && BuildModeManager.Instance.IsIdleSpeedup;

            if (inBuild)
                timerText.text = string.Format("{0}:{1:00} [x0.1]", minutes, seconds);
            else if (idleSpeedup)
                timerText.text = string.Format("{0}:{1:00} [x3]", minutes, seconds);
            else
                timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
        }

        // Update enemy count (always show enemies, no build mode override)
        if (enemyCountText != null && EnemySpawnManager.Instance != null)
        {
            int remaining = EnemySpawnManager.Instance.DayEnemiesRemaining;
            int total = EnemySpawnManager.Instance.DayTotalEnemies;
            enemyCountText.text = string.Format("Enemies: {0}/{1}", remaining, total);
        }

        // Update build mode panel
        UpdateBuildPanel();

        // Update defender counts by type
        if (defenderCountText != null)
        {
            UpdateDefenderCounts();
        }

        // Update banner auto-hide
        if (bannerRoot != null && bannerRoot.activeSelf && bannerAutoHide)
        {
            bannerTimer -= Time.unscaledDeltaTime;
            if (bannerTimer <= 0f)
            {
                bannerRoot.SetActive(false);
                Debug.Log("[GameHUD] Banner auto-hidden.");
            }
        }
    }

    // --- Notification Banner ---

    private void CreateBannerUI()
    {
        bannerRoot = new GameObject("NotificationBanner");
        bannerRoot.transform.SetParent(transform, false);

        var bannerRect = bannerRoot.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.2f, 0.4f);
        bannerRect.anchorMax = new Vector2(0.8f, 0.6f);
        bannerRect.offsetMin = Vector2.zero;
        bannerRect.offsetMax = Vector2.zero;

        // Semi-transparent background
        var bg = bannerRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);
        bg.raycastTarget = false;

        // Text child
        var textObj = new GameObject("BannerText");
        textObj.transform.SetParent(bannerRoot.transform, false);

        bannerText = textObj.AddComponent<TextMeshProUGUI>();
        var textRect = bannerText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);

        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.fontSize = 42;
        bannerText.enableAutoSizing = true;
        bannerText.fontSizeMin = 20;
        bannerText.fontSizeMax = 42;
        bannerText.color = new Color(1f, 0.85f, 0.2f);
        bannerText.fontStyle = FontStyles.Bold;
        bannerText.raycastTarget = false;

        bannerRoot.SetActive(false);
        Debug.Log("[GameHUD] Notification banner UI created.");
    }

    /// <summary>Show a centered notification banner. If duration > 0, auto-hides after that many seconds.</summary>
    public static void ShowBanner(string text, float duration = 0f)
    {
        if (Instance == null || Instance.bannerRoot == null) return;
        Instance.bannerText.text = text;
        Instance.bannerRoot.SetActive(true);
        Instance.bannerAutoHide = duration > 0f;
        Instance.bannerTimer = duration;
        Debug.Log($"[GameHUD] Banner shown: \"{text}\" (autoHide={duration > 0f}, duration={duration:F1}s)");
    }

    /// <summary>Immediately hide the notification banner.</summary>
    public static void HideBanner()
    {
        if (Instance == null || Instance.bannerRoot == null) return;
        if (!Instance.bannerRoot.activeSelf) return;
        Instance.bannerRoot.SetActive(false);
        Debug.Log("[GameHUD] Banner hidden.");
    }

    // --- Build Mode Panel (top-right modal) ---

    private void CreateBuildPanel()
    {
        buildPanelRoot = new GameObject("BuildModePanel");
        buildPanelRoot.transform.SetParent(transform, false);

        var panelRect = buildPanelRoot.AddComponent<RectTransform>();
        // Top-right corner: anchor top-right, fixed size
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-10f, -10f);
        panelRect.sizeDelta = new Vector2(220f, 140f);

        // Semi-transparent background
        var bg = buildPanelRoot.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.05f, 0f, 0.85f);
        bg.raycastTarget = false;

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(buildPanelRoot.transform, false);
        var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        var titleRect = titleTMP.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.72f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-10, -5);
        titleTMP.text = "BUILD MODE";
        titleTMP.fontSize = 22;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color = new Color(1f, 0.85f, 0.2f);
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.raycastTarget = false;

        // Wall cost line
        var costObj = new GameObject("WallCost");
        costObj.transform.SetParent(buildPanelRoot.transform, false);
        buildPanelCostText = costObj.AddComponent<TextMeshProUGUI>();
        var costRect = buildPanelCostText.GetComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0f, 0.44f);
        costRect.anchorMax = new Vector2(1f, 0.72f);
        costRect.offsetMin = new Vector2(10, 0);
        costRect.offsetMax = new Vector2(-10, 0);
        buildPanelCostText.fontSize = 18;
        buildPanelCostText.color = Color.white;
        buildPanelCostText.alignment = TextAlignmentOptions.Left;
        buildPanelCostText.raycastTarget = false;

        // Gold line
        var goldObj = new GameObject("GoldInfo");
        goldObj.transform.SetParent(buildPanelRoot.transform, false);
        buildPanelGoldText = goldObj.AddComponent<TextMeshProUGUI>();
        var goldRect = buildPanelGoldText.GetComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0f, 0.22f);
        goldRect.anchorMax = new Vector2(1f, 0.44f);
        goldRect.offsetMin = new Vector2(10, 0);
        goldRect.offsetMax = new Vector2(-10, 0);
        buildPanelGoldText.fontSize = 18;
        buildPanelGoldText.color = Color.white;
        buildPanelGoldText.alignment = TextAlignmentOptions.Left;
        buildPanelGoldText.raycastTarget = false;

        // Controls hint
        var hintObj = new GameObject("Hint");
        hintObj.transform.SetParent(buildPanelRoot.transform, false);
        var hintTMP = hintObj.AddComponent<TextMeshProUGUI>();
        var hintRect = hintTMP.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0.22f);
        hintRect.offsetMin = new Vector2(10, 3);
        hintRect.offsetMax = new Vector2(-10, 0);
        hintTMP.text = GetBuildHintText();
        hintTMP.fontSize = 14;
        hintTMP.color = new Color(0.7f, 0.7f, 0.7f);
        hintTMP.alignment = TextAlignmentOptions.Center;
        hintTMP.raycastTarget = false;

        buildPanelRoot.SetActive(false);
        Debug.Log("[GameHUD] Build mode panel created (top-right).");
    }

    private void UpdateBuildPanel()
    {
        bool inBuild = BuildModeManager.Instance != null && BuildModeManager.Instance.IsBuildMode;

        if (buildPanelRoot != null && buildPanelRoot.activeSelf != inBuild)
            buildPanelRoot.SetActive(inBuild);

        if (!inBuild || buildPanelRoot == null) return;

        int wallCost = BuildModeManager.Instance.WallCost;
        int gold = GameManager.Instance != null ? GameManager.Instance.Treasure : 0;
        bool canAfford = gold >= wallCost;

        buildPanelCostText.text = string.Format("Wall cost: {0}g", wallCost);
        buildPanelGoldText.text = string.Format("Gold: {0}g", gold);
        buildPanelGoldText.color = canAfford ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
    }

    // --- Existing HUD ---

    private int engCount, pikeCount, xbowCount, wizCount;

    private void UpdateDefenderCounts()
    {
        engCount = 0; pikeCount = 0; xbowCount = 0; wizCount = 0;
        var defenders = FindObjectsByType<Defender>(FindObjectsSortMode.None);
        for (int i = 0; i < defenders.Length; i++)
        {
            if (defenders[i].IsDead || defenders[i].Data == null) continue;
            switch (defenders[i].Data.defenderType)
            {
                case DefenderType.Engineer: engCount++; break;
                case DefenderType.Pikeman: pikeCount++; break;
                case DefenderType.Crossbowman: xbowCount++; break;
                case DefenderType.Wizard: wizCount++; break;
            }
        }
        defenderCountText.text = string.Format("Eng:{0} Pike:{1} Xbow:{2} Wiz:{3}", engCount, pikeCount, xbowCount, wizCount);
    }

    private void UpdateTreasure(int amount)
    {
        if (treasureText != null)
            treasureText.text = "Gold: " + amount;
    }

    private void UpdateMenials(int amount)
    {
        if (menialText != null)
        {
            int idle = GameManager.Instance != null ? GameManager.Instance.IdleMenialCount : 0;
            menialText.text = string.Format("Menials: {0}/{1}", idle, amount);
        }
    }

    private void UpdateKills(int amount)
    {
        if (killsText != null)
            killsText.text = "Kills: " + amount;
    }

    private void OnPauseClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TogglePause();
    }

    private static string GetBuildHintText()
    {
        if (InputBindingManager.Instance == null)
            return "A/D rotate  B to exit";
        string left = InputBindingManager.Instance.GetKeyboardDisplayName(GameAction.RotateWallLeft);
        string right = InputBindingManager.Instance.GetKeyboardDisplayName(GameAction.RotateWallRight);
        string exit = InputBindingManager.Instance.GetKeyboardDisplayName(GameAction.ToggleBuildMode);
        return $"{left}/{right} rotate  {exit} to exit";
    }

    private void UpdatePauseButton(bool isPaused)
    {
        if (pauseButtonIcon != null && playSprite != null && pauseSprite != null)
            pauseButtonIcon.sprite = isPaused ? playSprite : pauseSprite;
    }

    private void GenerateIcons()
    {
        pauseSprite = CreateIcon(DrawPause);
        playSprite = CreateIcon(DrawPlay);
        // Set initial state
        if (pauseButtonIcon != null)
            pauseButtonIcon.sprite = pauseSprite;
        Debug.Log("[GameHUD] Play/pause icons generated.");
    }

    private static Sprite CreateIcon(System.Action<Color32[], int> drawFunc)
    {
        int size = 32;
        var pixels = new Color32[size * size];
        // Fill transparent
        var clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        drawFunc(pixels, size);

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void DrawPause(Color32[] pixels, int size)
    {
        var white = new Color32(255, 255, 255, 255);
        int pad = 8;
        int barW = 4;
        int gap = 4;
        int cx = size / 2;
        int leftX = cx - gap / 2 - barW;
        int rightX = cx + gap / 2;

        for (int y = pad; y < size - pad; y++)
        {
            for (int x = leftX; x < leftX + barW; x++)
                pixels[y * size + x] = white;
            for (int x = rightX; x < rightX + barW; x++)
                pixels[y * size + x] = white;
        }
    }

    private static void DrawPlay(Color32[] pixels, int size)
    {
        var white = new Color32(255, 255, 255, 255);
        int pad = 7;
        int left = 11;
        int right = 23;
        float cy = size / 2f;
        float halfH = (size - 2 * pad) / 2f;

        for (int y = pad; y < size - pad; y++)
        {
            float t = Mathf.Abs(y - cy) / halfH;
            int rowRight = Mathf.RoundToInt(Mathf.Lerp(right, left, t));
            for (int x = left; x <= rowRight; x++)
                pixels[y * size + x] = white;
        }
    }
}
