using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Code-generated Legacy Rank panel for the main menu.
/// Shows current rank, progress bar to next rank, and all active bonuses.
/// Follows MutatorUI/StatsDashboardPanel BuildUI() pattern.
/// </summary>
public class LegacyUI : MonoBehaviour
{
    private Canvas canvas;
    private GameObject dialogPanel;
    private RectTransform contentContainer;

    // Colors
    private static readonly Color bgColor = new Color(0.15f, 0.12f, 0.08f, 0.95f);
    private static readonly Color goldColor = new Color(0.9f, 0.75f, 0.3f);
    private static readonly Color textColor = new Color(0.85f, 0.82f, 0.75f);
    private static readonly Color dimTextColor = new Color(0.55f, 0.52f, 0.45f);
    private static readonly Color headerColor = new Color(0.8f, 0.65f, 0.25f);
    private static readonly Color progressBgColor = new Color(0.2f, 0.18f, 0.14f);
    private static readonly Color progressFillColor = new Color(0.6f, 0.5f, 0.15f);

    public void Show()
    {
        Debug.Log("[LegacyUI] Showing legacy rank panel.");
        if (canvas == null)
            BuildUI();
        canvas.gameObject.SetActive(true);
        RefreshContent();
    }

    public void Hide()
    {
        Debug.Log("[LegacyUI] Hiding legacy rank panel.");
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void RefreshContent()
    {
        Debug.Log("[LegacyUI] Refreshing legacy rank display.");
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
            Destroy(contentContainer.GetChild(i).gameObject);

        int rank = LegacyProgressionManager.GetCurrentRank();
        int points = LegacyProgressionManager.GetLegacyPoints();
        string title = LegacyProgressionManager.GetCurrentRankTitle();
        int maxRank = LegacyProgressionManager.GetMaxRank();

        // Rank title
        AddCenteredText($"<size=40>{title}</size>", goldColor, 55);
        AddCenteredText($"Rank {rank} / {maxRank}", textColor, 28);
        AddSpacer(10);

        // Points
        AddCenteredText($"Legacy Points: {points}", textColor, 22);

        // Progress bar to next rank
        if (rank < maxRank)
        {
            int toNext = LegacyProgressionManager.GetPointsToNextRank();
            float progress = LegacyProgressionManager.GetProgressToNextRank();
            AddSpacer(8);
            AddProgressBar(progress);
            AddCenteredText($"{toNext} points to next rank", dimTextColor, 16);
        }
        else
        {
            AddSpacer(8);
            AddProgressBar(1f);
            AddCenteredText("MAX RANK ACHIEVED", goldColor, 18);
        }

        AddSpacer(15);

        // Bonuses section
        AddSectionHeader("ACTIVE BONUSES");

        if (rank == 0)
        {
            AddCenteredText("Complete runs to earn Legacy Points and unlock bonuses.", dimTextColor, 16);
            AddCenteredText("Points earned = Score / 1000", dimTextColor, 14);
        }
        else
        {
            string bonuses = LegacyProgressionManager.GetBonusSummary();
            string[] lines = bonuses.Split('\n');
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    AddBonusRow(line.Trim());
            }
        }

        AddSpacer(15);

        // Rank tiers reference
        AddSectionHeader("RANK TIERS");
        string[] rankNames = { "Recruit", "Militia", "Sergeant", "Captain", "Commander",
                               "Warden", "Champion", "Marshal", "Grand Marshal", "Legendary", "Mythic" };
        int[] thresholds = { 0, 10, 30, 60, 100, 160, 240, 350, 500, 700 };

        for (int i = 0; i <= maxRank; i++)
        {
            bool isCurrent = (i == rank);
            string pts = i == 0 ? "0" : thresholds[i - 1].ToString();
            Color rowColor = isCurrent ? goldColor : (i < rank ? dimTextColor : textColor);
            string marker = isCurrent ? " <<" : "";
            AddStatRow($"Rank {i}: {rankNames[i]}", $"{pts} pts{marker}", rowColor);
        }
    }

    private void AddCenteredText(string text, Color color, float fontSize)
    {
        var obj = new GameObject("Text");
        obj.transform.SetParent(contentContainer, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.richText = true;
        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 10;
        le.flexibleWidth = 1;
    }

    private void AddSpacer(float height)
    {
        var obj = new GameObject("Spacer");
        obj.transform.SetParent(contentContainer, false);
        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
    }

    private void AddSectionHeader(string text)
    {
        var obj = new GameObject("Header");
        obj.transform.SetParent(contentContainer, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = headerColor;
        tmp.alignment = TextAlignmentOptions.Left;
        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 32;
        le.flexibleWidth = 1;

        // Underline
        var lineObj = new GameObject("Underline");
        lineObj.transform.SetParent(contentContainer, false);
        var lineImg = lineObj.AddComponent<Image>();
        lineImg.color = new Color(headerColor.r, headerColor.g, headerColor.b, 0.3f);
        var lineLe = lineObj.AddComponent<LayoutElement>();
        lineLe.preferredHeight = 2;
        lineLe.flexibleWidth = 1;
    }

    private void AddBonusRow(string text)
    {
        var obj = new GameObject("Bonus");
        obj.transform.SetParent(contentContainer, false);
        var hlg = obj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.padding = new RectOffset(15, 10, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = true;
        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 24;
        le.flexibleWidth = 1;

        // Bullet
        var bulletObj = new GameObject("Bullet");
        bulletObj.transform.SetParent(obj.transform, false);
        var bulletImg = bulletObj.AddComponent<Image>();
        bulletImg.color = goldColor;
        var bulletLe = bulletObj.AddComponent<LayoutElement>();
        bulletLe.preferredWidth = 6;
        bulletLe.preferredHeight = 6;

        // Text
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.color = textColor;
        var textLe = textObj.AddComponent<LayoutElement>();
        textLe.flexibleWidth = 1;
    }

    private void AddStatRow(string label, string value, Color color)
    {
        var obj = new GameObject("StatRow");
        obj.transform.SetParent(contentContainer, false);
        var hlg = obj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.padding = new RectOffset(15, 15, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = true;
        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 22;
        le.flexibleWidth = 1;

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(obj.transform, false);
        var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 16;
        labelTmp.color = color;
        var labelLe = labelObj.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1;

        var valObj = new GameObject("Value");
        valObj.transform.SetParent(obj.transform, false);
        var valTmp = valObj.AddComponent<TextMeshProUGUI>();
        valTmp.text = value;
        valTmp.fontSize = 16;
        valTmp.color = color;
        valTmp.alignment = TextAlignmentOptions.MidlineRight;
        var valLe = valObj.AddComponent<LayoutElement>();
        valLe.preferredWidth = 120;
    }

    private void AddProgressBar(float fraction)
    {
        var barObj = new GameObject("ProgressBar");
        barObj.transform.SetParent(contentContainer, false);
        var barImg = barObj.AddComponent<Image>();
        barImg.color = progressBgColor;
        var barLe = barObj.AddComponent<LayoutElement>();
        barLe.preferredHeight = 16;
        barLe.flexibleWidth = 1;

        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barObj.transform, false);
        var fillImg = fillObj.AddComponent<Image>();
        fillImg.color = fraction >= 1f ? goldColor : progressFillColor;
        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    private void BuildUI()
    {
        Debug.Log("[LegacyUI] Building legacy rank panel UI.");

        // Canvas
        var canvasObj = new GameObject("LegacyCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Dim background
        var dimObj = new GameObject("DimBG");
        dimObj.transform.SetParent(canvasObj.transform, false);
        var dimImg = dimObj.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.7f);
        var dimRect = dimObj.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        var dimBtn = dimObj.AddComponent<Button>();
        dimBtn.onClick.AddListener(Hide);

        // Dialog panel
        dialogPanel = new GameObject("DialogPanel");
        dialogPanel.transform.SetParent(canvasObj.transform, false);
        var panelImg = dialogPanel.AddComponent<Image>();
        panelImg.color = bgColor;
        var panelRect = dialogPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.05f);
        panelRect.anchorMax = new Vector2(0.85f, 0.95f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelVlg = dialogPanel.AddComponent<VerticalLayoutGroup>();
        panelVlg.spacing = 5;
        panelVlg.padding = new RectOffset(20, 20, 15, 15);
        panelVlg.childAlignment = TextAnchor.UpperCenter;
        panelVlg.childControlWidth = true;
        panelVlg.childControlHeight = true;
        panelVlg.childForceExpandWidth = true;
        panelVlg.childForceExpandHeight = false;

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(dialogPanel.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "LEGACY RANK";
        titleTmp.fontSize = 36;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = goldColor;
        titleTmp.alignment = TextAlignmentOptions.Center;
        var titleLe = titleObj.AddComponent<LayoutElement>();
        titleLe.preferredHeight = 50;

        // Scroll area
        var scrollObj = new GameObject("ScrollArea");
        scrollObj.transform.SetParent(dialogPanel.transform, false);
        var scrollLe = scrollObj.AddComponent<LayoutElement>();
        scrollLe.flexibleHeight = 1;
        scrollLe.flexibleWidth = 1;

        var scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        var scrollImg = scrollObj.AddComponent<Image>();
        scrollImg.color = Color.clear;
        scrollObj.AddComponent<Mask>();

        // Content
        var contentObj = new GameObject("Content");
        contentObj.AddComponent<RectTransform>();
        contentObj.transform.SetParent(scrollObj.transform, false);
        contentContainer = contentObj.GetComponent<RectTransform>();
        contentContainer.anchorMin = new Vector2(0, 1);
        contentContainer.anchorMax = new Vector2(1, 1);
        contentContainer.pivot = new Vector2(0.5f, 1);
        contentContainer.offsetMin = Vector2.zero;
        contentContainer.offsetMax = Vector2.zero;

        var contentVlg = contentObj.AddComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 3;
        contentVlg.padding = new RectOffset(10, 10, 5, 5);
        contentVlg.childAlignment = TextAnchor.UpperCenter;
        contentVlg.childControlWidth = true;
        contentVlg.childControlHeight = true;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;

        var csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentContainer;

        // Close button
        var closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(dialogPanel.transform, false);
        var closeImg = closeObj.AddComponent<Image>();
        closeImg.color = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        var closeBtn = closeObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(Hide);
        var closeLe = closeObj.AddComponent<LayoutElement>();
        closeLe.preferredHeight = 45;
        closeLe.preferredWidth = 200;
        closeLe.flexibleWidth = 0;

        var closeTxtObj = new GameObject("Text");
        closeTxtObj.transform.SetParent(closeObj.transform, false);
        var closeTmp = closeTxtObj.AddComponent<TextMeshProUGUI>();
        closeTmp.text = "CLOSE";
        closeTmp.fontSize = 24;
        closeTmp.fontStyle = FontStyles.Bold;
        closeTmp.color = goldColor;
        closeTmp.alignment = TextAlignmentOptions.Center;
        var closeTxtRect = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRect.anchorMin = Vector2.zero;
        closeTxtRect.anchorMax = Vector2.one;
        closeTxtRect.offsetMin = Vector2.zero;
        closeTxtRect.offsetMax = Vector2.zero;

        canvas.gameObject.SetActive(false);
    }
}
