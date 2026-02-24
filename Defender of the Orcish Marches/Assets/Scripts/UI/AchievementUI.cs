using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Code-generated achievement panel for the main menu.
/// Displays all 20 achievements grouped by category with progress bars and tier badges.
/// Follows MutatorUI/StatsDashboardPanel BuildUI() pattern.
/// </summary>
public class AchievementUI : MonoBehaviour
{
    private Canvas canvas;
    private GameObject dialogPanel;
    private RectTransform contentContainer;
    private ScrollRect scrollRect;
    private TextMeshProUGUI summaryText;

    // Colors
    private static readonly Color bgColor = new Color(0.15f, 0.12f, 0.08f, 0.95f);
    private static readonly Color goldColor = new Color(0.9f, 0.75f, 0.3f);
    private static readonly Color textColor = new Color(0.85f, 0.82f, 0.75f);
    private static readonly Color dimTextColor = new Color(0.55f, 0.52f, 0.45f);
    private static readonly Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
    private static readonly Color silverColor = new Color(0.75f, 0.75f, 0.8f);
    private static readonly Color headerColor = new Color(0.8f, 0.65f, 0.25f);
    private static readonly Color progressBgColor = new Color(0.2f, 0.18f, 0.14f);
    private static readonly Color progressFillColor = new Color(0.5f, 0.4f, 0.15f);

    public void Show()
    {
        Debug.Log("[AchievementUI] Showing achievement panel.");
        if (canvas == null)
            BuildUI();
        canvas.gameObject.SetActive(true);
        RefreshAll();
    }

    public void Hide()
    {
        Debug.Log("[AchievementUI] Hiding achievement panel.");
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void RefreshAll()
    {
        Debug.Log("[AchievementUI] Refreshing achievement display.");
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
            Destroy(contentContainer.GetChild(i).gameObject);

        // Summary
        int totalEarned = AchievementManager.GetTotalTiersEarned();
        int goldCount = AchievementManager.GetGoldCount();
        if (summaryText != null)
            summaryText.text = $"Achievements: {totalEarned}/{AchievementDefs.All.Length}  |  Gold: {goldCount}";

        // Group by category
        AchievementCategory? lastCategory = null;
        foreach (var def in AchievementDefs.All)
        {
            if (lastCategory == null || lastCategory != def.category)
            {
                AddCategoryHeader(def.category.ToString().ToUpper());
                lastCategory = def.category;
            }
            AddAchievementRow(def);
        }
    }

    private void AddCategoryHeader(string text)
    {
        var headerObj = new GameObject("Header_" + text);
        headerObj.transform.SetParent(contentContainer, false);
        var headerTmp = headerObj.AddComponent<TextMeshProUGUI>();
        headerTmp.text = text;
        headerTmp.fontSize = 22;
        headerTmp.fontStyle = FontStyles.Bold;
        headerTmp.color = headerColor;
        headerTmp.alignment = TextAlignmentOptions.Left;
        var le = headerObj.AddComponent<LayoutElement>();
        le.preferredHeight = 35;
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

    private void AddAchievementRow(AchievementDef def)
    {
        var tier = AchievementManager.GetTier(def.id);
        int progress = AchievementManager.GetProgress(def.id);
        int nextThreshold = AchievementManager.GetNextThreshold(def, tier);

        var rowObj = new GameObject("Ach_" + def.id);
        rowObj.transform.SetParent(contentContainer, false);
        var rowLe = rowObj.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 60;
        rowLe.flexibleWidth = 1;

        // Row layout
        var rowHlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        rowHlg.spacing = 10;
        rowHlg.padding = new RectOffset(5, 5, 2, 2);
        rowHlg.childAlignment = TextAnchor.MiddleLeft;
        rowHlg.childControlWidth = true;
        rowHlg.childControlHeight = true;
        rowHlg.childForceExpandHeight = true;

        // Tier badge
        var badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(rowObj.transform, false);
        var badgeImg = badgeObj.AddComponent<Image>();
        badgeImg.color = GetTierColor(tier);
        var badgeLe = badgeObj.AddComponent<LayoutElement>();
        badgeLe.preferredWidth = 8;
        badgeLe.flexibleHeight = 1;

        // Info column
        var infoObj = new GameObject("Info");
        infoObj.transform.SetParent(rowObj.transform, false);
        var infoVlg = infoObj.AddComponent<VerticalLayoutGroup>();
        infoVlg.spacing = 2;
        infoVlg.childAlignment = TextAnchor.MiddleLeft;
        infoVlg.childControlWidth = true;
        infoVlg.childControlHeight = true;
        infoVlg.childForceExpandWidth = true;
        var infoLe = infoObj.AddComponent<LayoutElement>();
        infoLe.flexibleWidth = 1;

        // Name + tier text
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(infoObj.transform, false);
        var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        string tierStr = tier != AchievementTier.None ? $" <color=#{ColorUtility.ToHtmlStringRGB(GetTierColor(tier))}>[{tier}]</color>" : "";
        nameTmp.text = $"{def.name}{tierStr}";
        nameTmp.fontSize = 18;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = tier == AchievementTier.Gold ? goldColor : textColor;
        var nameLe = nameObj.AddComponent<LayoutElement>();
        nameLe.preferredHeight = 22;

        // Description
        var descObj = new GameObject("Desc");
        descObj.transform.SetParent(infoObj.transform, false);
        var descTmp = descObj.AddComponent<TextMeshProUGUI>();
        descTmp.text = def.description;
        descTmp.fontSize = 14;
        descTmp.color = dimTextColor;
        var descLe = descObj.AddComponent<LayoutElement>();
        descLe.preferredHeight = 16;

        // Progress bar row
        var progRowObj = new GameObject("ProgRow");
        progRowObj.transform.SetParent(infoObj.transform, false);
        var progRowHlg = progRowObj.AddComponent<HorizontalLayoutGroup>();
        progRowHlg.spacing = 8;
        progRowHlg.childAlignment = TextAnchor.MiddleLeft;
        progRowHlg.childControlWidth = true;
        progRowHlg.childControlHeight = true;
        progRowHlg.childForceExpandHeight = true;
        var progRowLe = progRowObj.AddComponent<LayoutElement>();
        progRowLe.preferredHeight = 14;

        // Progress bar background
        var barBgObj = new GameObject("BarBg");
        barBgObj.transform.SetParent(progRowObj.transform, false);
        var barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.color = progressBgColor;
        var barBgLe = barBgObj.AddComponent<LayoutElement>();
        barBgLe.flexibleWidth = 1;
        barBgLe.preferredHeight = 10;

        // Progress bar fill
        var barFillObj = new GameObject("BarFill");
        barFillObj.transform.SetParent(barBgObj.transform, false);
        var barFillImg = barFillObj.AddComponent<Image>();
        barFillImg.color = tier == AchievementTier.Gold ? goldColor : progressFillColor;
        var barFillRect = barFillObj.GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = new Vector2(GetProgressFraction(def, tier, progress, nextThreshold), 1f);
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;

        // Progress text
        var progTextObj = new GameObject("ProgText");
        progTextObj.transform.SetParent(progRowObj.transform, false);
        var progTmp = progTextObj.AddComponent<TextMeshProUGUI>();
        if (tier == AchievementTier.Gold)
            progTmp.text = "COMPLETE";
        else
            progTmp.text = $"{progress}/{nextThreshold}";
        progTmp.fontSize = 12;
        progTmp.color = tier == AchievementTier.Gold ? goldColor : dimTextColor;
        progTmp.alignment = TextAlignmentOptions.MidlineRight;
        var progTextLe = progTextObj.AddComponent<LayoutElement>();
        progTextLe.preferredWidth = 80;
    }

    private float GetProgressFraction(AchievementDef def, AchievementTier tier, int progress, int nextThreshold)
    {
        if (tier == AchievementTier.Gold) return 1f;
        if (nextThreshold <= 0) return 0f;

        bool inverted = def.id == "menial_guardian" || def.id == "speed_runner";
        if (inverted)
        {
            // For inverted, lower is better. Show how close to threshold.
            if (progress <= nextThreshold) return 1f;
            int prevThreshold = tier == AchievementTier.None ? def.bronzeThreshold * 2 :
                               tier == AchievementTier.Bronze ? def.bronzeThreshold :
                               def.silverThreshold;
            int range = prevThreshold - nextThreshold;
            if (range <= 0) return 0f;
            return Mathf.Clamp01(1f - (float)(progress - nextThreshold) / range);
        }
        else
        {
            return Mathf.Clamp01((float)progress / nextThreshold);
        }
    }

    private Color GetTierColor(AchievementTier tier)
    {
        switch (tier)
        {
            case AchievementTier.Bronze: return bronzeColor;
            case AchievementTier.Silver: return silverColor;
            case AchievementTier.Gold: return goldColor;
            default: return new Color(0.3f, 0.28f, 0.25f);
        }
    }

    private void BuildUI()
    {
        Debug.Log("[AchievementUI] Building achievement panel UI.");

        // Canvas (overlay, above everything)
        var canvasObj = new GameObject("AchievementCanvas");
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
        panelRect.anchorMin = new Vector2(0.1f, 0.05f);
        panelRect.anchorMax = new Vector2(0.9f, 0.95f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelVlg = dialogPanel.AddComponent<VerticalLayoutGroup>();
        panelVlg.spacing = 8;
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
        titleTmp.text = "ACHIEVEMENTS";
        titleTmp.fontSize = 36;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = goldColor;
        titleTmp.alignment = TextAlignmentOptions.Center;
        var titleLe = titleObj.AddComponent<LayoutElement>();
        titleLe.preferredHeight = 50;

        // Summary text
        var summObj = new GameObject("Summary");
        summObj.transform.SetParent(dialogPanel.transform, false);
        summaryText = summObj.AddComponent<TextMeshProUGUI>();
        summaryText.fontSize = 18;
        summaryText.color = textColor;
        summaryText.alignment = TextAlignmentOptions.Center;
        var summLe = summObj.AddComponent<LayoutElement>();
        summLe.preferredHeight = 25;

        // Scroll area
        var scrollObj = new GameObject("ScrollArea");
        scrollObj.transform.SetParent(dialogPanel.transform, false);
        var scrollLe = scrollObj.AddComponent<LayoutElement>();
        scrollLe.flexibleHeight = 1;
        scrollLe.flexibleWidth = 1;

        scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        var scrollImg = scrollObj.AddComponent<Image>();
        scrollImg.color = Color.clear;
        scrollObj.AddComponent<Mask>();

        // Content container
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
        contentVlg.spacing = 4;
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
