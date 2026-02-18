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

        var topLayout = topBar.AddComponent<HorizontalLayoutGroup>();
        topLayout.padding = new RectOffset(20, 20, 5, 5);
        topLayout.spacing = 40;
        topLayout.childAlignment = TextAnchor.MiddleLeft;
        topLayout.childControlWidth = false;
        topLayout.childControlHeight = true;

        var treasureText = CreateText(topBar.transform, "TreasureText", "Gold: 50", 24, Color.yellow, 200);
        var menialText = CreateText(topBar.transform, "MenialText", "Menials: 3/3", 24, new Color(0.4f, 0.7f, 1f), 250);
        var timerText = CreateText(topBar.transform, "TimerText", "0:00", 24, Color.white, 120);
        var enemyText = CreateText(topBar.transform, "EnemyCountText", "Enemies: 0", 24, new Color(1f, 0.5f, 0.5f), 200);

        // Pause button
        var pauseBtn = CreateUIButton(topBar.transform, "PauseButton", "PAUSE [Space]", 160, 35);

        // Add GameHUD component
        var hud = canvasObj.AddComponent<GameHUD>();
        var hudSO = new SerializedObject(hud);
        hudSO.FindProperty("treasureText").objectReferenceValue = treasureText.GetComponent<TextMeshProUGUI>();
        hudSO.FindProperty("menialText").objectReferenceValue = menialText.GetComponent<TextMeshProUGUI>();
        hudSO.FindProperty("timerText").objectReferenceValue = timerText.GetComponent<TextMeshProUGUI>();
        hudSO.FindProperty("enemyCountText").objectReferenceValue = enemyText.GetComponent<TextMeshProUGUI>();
        hudSO.FindProperty("pauseButton").objectReferenceValue = pauseBtn.GetComponent<Button>();
        hudSO.FindProperty("pauseButtonText").objectReferenceValue = pauseBtn.GetComponentInChildren<TextMeshProUGUI>();
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
#endif
}
