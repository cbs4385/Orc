using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Code-generated stats dashboard panel accessible from the main menu.
/// Three tabs: LIFETIME, RECORDS, COMPUTED.
/// Follows BugReportPanel.BuildUI() pattern.
/// </summary>
public class StatsDashboardPanel : MonoBehaviour
{
    private Canvas canvas;
    private GameObject dialogPanel;
    private RectTransform contentContainer;
    private ScrollRect scrollRect;

    // Tab buttons
    private Button lifetimeTabBtn;
    private Button recordsTabBtn;
    private Button computedTabBtn;
    private Image lifetimeUnderline;
    private Image recordsUnderline;
    private Image computedUnderline;

    private int activeTab; // 0=lifetime, 1=records, 2=computed

    // Colors
    private static readonly Color bgColor = new Color(0.15f, 0.12f, 0.08f, 0.95f);
    private static readonly Color goldColor = new Color(0.9f, 0.75f, 0.3f);
    private static readonly Color headerColor = new Color(0.8f, 0.65f, 0.25f);
    private static readonly Color textColor = new Color(0.85f, 0.82f, 0.75f);
    private static readonly Color dimTextColor = new Color(0.55f, 0.52f, 0.45f);
    private static readonly Color tabActiveColor = new Color(0.9f, 0.75f, 0.3f);
    private static readonly Color tabInactiveColor = new Color(0.5f, 0.45f, 0.35f);

    public void Show()
    {
        Debug.Log("[StatsDashboardPanel] Showing stats dashboard.");
        if (canvas == null)
            BuildUI();
        canvas.gameObject.SetActive(true);
        SetActiveTab(0);
    }

    public void Hide()
    {
        Debug.Log("[StatsDashboardPanel] Hiding stats dashboard.");
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void SetActiveTab(int tab)
    {
        activeTab = tab;

        lifetimeUnderline.enabled = (tab == 0);
        recordsUnderline.enabled = (tab == 1);
        computedUnderline.enabled = (tab == 2);

        lifetimeTabBtn.GetComponentInChildren<TextMeshProUGUI>().color = tab == 0 ? tabActiveColor : tabInactiveColor;
        recordsTabBtn.GetComponentInChildren<TextMeshProUGUI>().color = tab == 1 ? tabActiveColor : tabInactiveColor;
        computedTabBtn.GetComponentInChildren<TextMeshProUGUI>().color = tab == 2 ? tabActiveColor : tabInactiveColor;

        // Clear content
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
            Destroy(contentContainer.GetChild(i).gameObject);

        switch (tab)
        {
            case 0: PopulateLifetime(); break;
            case 1: PopulateRecords(); break;
            case 2: PopulateComputed(); break;
        }

        // Reset scroll position to top
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    // ===================== LIFETIME TAB =====================

    private void PopulateLifetime()
    {
        var data = LifetimeStatsManager.GetData();

        AddSectionHeader("OVERVIEW");
        AddStatRow("Total Runs", data.totalRuns.ToString("N0"));
        AddStatRow("Total Days Survived", data.totalDays.ToString("N0"));
        AddStatRow("Total Gold Earned", data.totalGoldEarned.ToString("N0"));
        AddStatRow("Total Gold Spent", data.totalGoldSpent.ToString("N0"));

        AddSectionHeader("ENEMY KILLS");
        AddStatRow("Total Kills", data.totalKills.ToString("N0"));
        AddStatRow("Orc Grunts", data.killsOrcGrunt.ToString("N0"));
        AddStatRow("Bow Orcs", data.killsBowOrc.ToString("N0"));
        AddStatRow("Trolls", data.killsTroll.ToString("N0"));
        AddStatRow("Suicide Goblins", data.killsSuicideGoblin.ToString("N0"));
        AddStatRow("Goblin Cannoneers", data.killsGoblinCannoneer.ToString("N0"));
        AddStatRow("Orc War Bosses", data.killsOrcWarBoss.ToString("N0"));

        AddSectionHeader("DEFENDERS");
        AddStatRow("Total Hires", data.totalHires.ToString("N0"));
        AddStatRow("Engineers Hired", data.hiresEngineer.ToString("N0"));
        AddStatRow("Pikemen Hired", data.hiresPikeman.ToString("N0"));
        AddStatRow("Crossbowmen Hired", data.hiresCrossbowman.ToString("N0"));
        AddStatRow("Wizards Hired", data.hiresWizard.ToString("N0"));
        AddStatRow("Menials Lost", data.totalMenialsLost.ToString("N0"));

        AddSectionHeader("FORTRESS & OTHER");
        AddStatRow("Walls Built", data.totalWallsBuilt.ToString("N0"));
        AddStatRow("Wall HP Repaired", data.totalWallHPRepaired.ToString("N0"));
        AddStatRow("Vegetation Cleared", data.totalVegetationCleared.ToString("N0"));
        AddStatRow("Refugees Saved", data.totalRefugeesSaved.ToString("N0"));
        AddStatRow("Ballista Shots Fired", data.totalBallistaShotsFired.ToString("N0"));

        AddSpacer();
    }

    // ===================== RECORDS TAB =====================

    private void PopulateRecords()
    {
        var data = LifetimeStatsManager.GetData();

        AddSectionHeader("OVERALL BESTS");
        AddStatRow("Longest Run", data.recordLongestRun > 0 ? $"{data.recordLongestRun} days" : "N/A");
        AddStatRow("Highest Score", data.recordHighestScore > 0 ? data.recordHighestScore.ToString("N0") : "N/A");
        AddStatRow("Most Kills", data.recordMostKills > 0 ? data.recordMostKills.ToString("N0") : "N/A");
        AddStatRow("Most Gold Earned", data.recordMostGold > 0 ? data.recordMostGold.ToString("N0") : "N/A");
        AddStatRow("Most Defenders Alive", data.recordMostDefendersAlive > 0 ? data.recordMostDefendersAlive.ToString() : "N/A");

        if (data.recordFastestBossKill > 0)
        {
            int mins = Mathf.FloorToInt(data.recordFastestBossKill / 60);
            int secs = Mathf.FloorToInt(data.recordFastestBossKill % 60);
            AddStatRow("Fastest Boss Kill", $"{mins}:{secs:D2}");
        }
        else
        {
            AddStatRow("Fastest Boss Kill", "N/A");
        }

        AddSectionHeader("PER-DIFFICULTY BESTS");
        AddStatRow("Longest Run (Easy)", data.recordLongestRunEasy > 0 ? $"{data.recordLongestRunEasy} days" : "N/A");
        AddStatRow("Longest Run (Normal)", data.recordLongestRunNormal > 0 ? $"{data.recordLongestRunNormal} days" : "N/A");
        AddStatRow("Longest Run (Hard)", data.recordLongestRunHard > 0 ? $"{data.recordLongestRunHard} days" : "N/A");
        AddStatRow("Longest Run (Nightmare)", data.recordLongestRunNightmare > 0 ? $"{data.recordLongestRunNightmare} days" : "N/A");

        AddSpacer();
    }

    // ===================== COMPUTED TAB =====================

    private void PopulateComputed()
    {
        var data = LifetimeStatsManager.GetData();

        AddSectionHeader("AVERAGES");
        float avgDays = LifetimeStatsManager.GetAverageDays();
        float avgKills = LifetimeStatsManager.GetAverageKills();
        float avgGold = LifetimeStatsManager.GetAverageGold();
        AddStatRow("Avg Days per Run", data.totalRuns > 0 ? avgDays.ToString("F1") : "N/A");
        AddStatRow("Avg Kills per Run", data.totalRuns > 0 ? avgKills.ToString("F1") : "N/A");
        AddStatRow("Avg Gold per Run", data.totalRuns > 0 ? avgGold.ToString("F0") : "N/A");

        AddSectionHeader("RATIOS");
        float kd = LifetimeStatsManager.GetKDRatio();
        AddStatRow("Kill/Death Ratio", data.totalMenialsLost > 0 ? kd.ToString("F1") : "N/A");

        if (data.totalGoldEarned > 0)
        {
            float spendRate = (float)data.totalGoldSpent / data.totalGoldEarned * 100f;
            AddStatRow("Gold Spend Rate", $"{spendRate:F0}%");
        }
        else
        {
            AddStatRow("Gold Spend Rate", "N/A");
        }

        AddSectionHeader("FAVORITES & TRENDS");
        AddStatRow("Favorite Defender", LifetimeStatsManager.GetFavoriteDefender());
        AddStatRow("Most Dangerous Enemy", LifetimeStatsManager.GetMostDangerousEnemy());

        float trend = LifetimeStatsManager.GetScoreTrend();
        if (data.recentScores.Count >= 6)
        {
            string trendStr = trend >= 0 ? $"+{trend:F0}%" : $"{trend:F0}%";
            AddStatRow("Score Trend (last 10)", trendStr);
        }
        else
        {
            AddStatRow("Score Trend", $"Need {6 - data.recentScores.Count} more runs");
        }

        AddSpacer();
    }

    // ===================== UI HELPERS =====================

    private void AddSectionHeader(string text)
    {
        var obj = new GameObject("Header_" + text);
        obj.transform.SetParent(contentContainer, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = headerColor;
        tmp.alignment = TextAlignmentOptions.Left;
        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 36;
        le.flexibleWidth = 1;

        // Underline
        var lineObj = new GameObject("Line");
        lineObj.transform.SetParent(contentContainer, false);
        var lineImg = lineObj.AddComponent<Image>();
        lineImg.color = new Color(headerColor.r, headerColor.g, headerColor.b, 0.3f);
        var lineLe = lineObj.AddComponent<LayoutElement>();
        lineLe.preferredHeight = 1;
        lineLe.flexibleWidth = 1;
    }

    private void AddStatRow(string label, string value)
    {
        var rowObj = new GameObject("Row_" + label);
        rowObj.transform.SetParent(contentContainer, false);
        var rowRect = rowObj.AddComponent<RectTransform>();

        var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(10, 10, 0, 0);

        var rowLe = rowObj.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 28;
        rowLe.flexibleWidth = 1;

        // Label
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(rowObj.transform, false);
        var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 18;
        labelTmp.color = dimTextColor;
        labelTmp.alignment = TextAlignmentOptions.Left;
        var labelLe = labelObj.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1;

        // Value
        var valObj = new GameObject("Value");
        valObj.transform.SetParent(rowObj.transform, false);
        var valTmp = valObj.AddComponent<TextMeshProUGUI>();
        valTmp.text = value;
        valTmp.fontSize = 18;
        valTmp.fontStyle = FontStyles.Bold;
        valTmp.color = textColor;
        valTmp.alignment = TextAlignmentOptions.Right;
        var valLe = valObj.AddComponent<LayoutElement>();
        valLe.preferredWidth = 200;
    }

    private void AddSpacer()
    {
        var obj = new GameObject("Spacer");
        obj.transform.SetParent(contentContainer, false);
        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 20;
    }

    // ===================== BUILD UI =====================

    private void BuildUI()
    {
        // Overlay canvas
        var canvasObj = new GameObject("StatsDashboardCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen dim background
        var dimObj = new GameObject("DimBackground");
        dimObj.transform.SetParent(canvasObj.transform, false);
        var dimImg = dimObj.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.7f);
        dimImg.raycastTarget = true;
        var dimRect = dimObj.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        // Dialog panel (900x700)
        var panelObj = new GameObject("DialogPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        dialogPanel = panelObj;
        var panelImg = panelObj.AddComponent<Image>();
        panelImg.color = bgColor;
        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(900, 700);
        panelRect.anchoredPosition = Vector2.zero;

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "STATISTICS";
        titleTmp.fontSize = 36;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = goldColor;
        titleTmp.alignment = TextAlignmentOptions.Center;
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 50);
        titleRect.anchoredPosition = new Vector2(0, -10);

        // Tab bar
        var tabBar = new GameObject("TabBar");
        tabBar.transform.SetParent(panelObj.transform, false);
        var tabBarRect = tabBar.AddComponent<RectTransform>();
        tabBarRect.anchorMin = new Vector2(0.05f, 0.88f);
        tabBarRect.anchorMax = new Vector2(0.95f, 0.93f);
        tabBarRect.offsetMin = Vector2.zero;
        tabBarRect.offsetMax = Vector2.zero;
        var tabHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabHlg.spacing = 20;
        tabHlg.childAlignment = TextAnchor.MiddleCenter;
        tabHlg.childControlWidth = true;
        tabHlg.childControlHeight = true;
        tabHlg.childForceExpandWidth = true;
        tabHlg.childForceExpandHeight = true;

        lifetimeTabBtn = CreateTabButton("LIFETIME", tabBar.transform, out lifetimeUnderline);
        recordsTabBtn = CreateTabButton("RECORDS", tabBar.transform, out recordsUnderline);
        computedTabBtn = CreateTabButton("COMPUTED", tabBar.transform, out computedUnderline);

        lifetimeTabBtn.onClick.AddListener(() => SetActiveTab(0));
        recordsTabBtn.onClick.AddListener(() => SetActiveTab(1));
        computedTabBtn.onClick.AddListener(() => SetActiveTab(2));

        // ScrollRect area
        var scrollObj = new GameObject("ScrollArea");
        scrollObj.transform.SetParent(panelObj.transform, false);
        var scrollRectTransform = scrollObj.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.03f, 0.1f);
        scrollRectTransform.anchorMax = new Vector2(0.97f, 0.86f);
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;
        scrollObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.2f);
        var mask = scrollObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // Content container inside scroll
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);
        contentContainer = contentObj.AddComponent<RectTransform>();
        contentContainer.anchorMin = new Vector2(0, 1);
        contentContainer.anchorMax = new Vector2(1, 1);
        contentContainer.pivot = new Vector2(0.5f, 1);
        contentContainer.sizeDelta = new Vector2(0, 0);
        contentContainer.anchoredPosition = Vector2.zero;

        var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        var csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentContainer;

        // Close button
        var closeBtn = CreateDialogButton("CloseButton", "CLOSE", panelObj.transform);
        var closeBtnRect = closeBtn.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(0.35f, 0.02f);
        closeBtnRect.anchorMax = new Vector2(0.65f, 0.08f);
        closeBtnRect.offsetMin = Vector2.zero;
        closeBtnRect.offsetMax = Vector2.zero;
        closeBtn.onClick.AddListener(Hide);

        canvas.gameObject.SetActive(false);
        Debug.Log("[StatsDashboardPanel] UI built.");
    }

    private Button CreateTabButton(string label, Transform parent, out Image underline)
    {
        var tabObj = new GameObject("Tab_" + label);
        tabObj.transform.SetParent(parent, false);

        // The tab button itself (transparent bg)
        var tabImg = tabObj.AddComponent<Image>();
        tabImg.color = Color.clear;
        var btn = tabObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = new Color(1, 1, 1, 0.05f);
        colors.pressedColor = new Color(1, 1, 1, 0.1f);
        colors.selectedColor = Color.clear;
        btn.colors = colors;

        // Text
        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(tabObj.transform, false);
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = tabInactiveColor;
        tmp.alignment = TextAlignmentOptions.Center;
        var txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        // Underline indicator
        var lineObj = new GameObject("Underline");
        lineObj.transform.SetParent(tabObj.transform, false);
        underline = lineObj.AddComponent<Image>();
        underline.color = goldColor;
        var lineRect = lineObj.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.1f, 0);
        lineRect.anchorMax = new Vector2(0.9f, 0);
        lineRect.pivot = new Vector2(0.5f, 0);
        lineRect.sizeDelta = new Vector2(0, 3);
        lineRect.anchoredPosition = Vector2.zero;
        underline.enabled = false;

        return btn;
    }

    private Button CreateDialogButton(string name, string label, Transform parent)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.2f, 0.1f, 0.9f);
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
        tmp.fontSize = 26;
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
}
