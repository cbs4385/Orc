using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Code-generated bestiary panel for the main menu.
/// Displays enemy entries with kill counts, lore unlock progress, and tactical tips.
/// Follows MutatorUI/AchievementUI BuildUI() pattern.
/// </summary>
public class BestiaryUI : MonoBehaviour
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
    private static readonly Color rowColor = new Color(0, 0, 0, 0.15f);
    private static readonly Color unlockedColor = new Color(0.25f, 0.35f, 0.2f, 0.4f);
    private static readonly Color tipColor = new Color(1f, 0.84f, 0f);
    private static readonly Color progressBgColor = new Color(0.2f, 0.18f, 0.14f);
    private static readonly Color progressFillColor = new Color(0.5f, 0.4f, 0.15f);
    private static readonly Color typeColor = new Color(0.65f, 0.55f, 0.4f);

    public void Show()
    {
        Debug.Log("[BestiaryUI] Showing bestiary panel.");
        if (canvas == null)
            BuildUI();
        canvas.gameObject.SetActive(true);
        RefreshDisplay();
    }

    public void Hide()
    {
        Debug.Log("[BestiaryUI] Hidden.");
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void RefreshDisplay()
    {
        Debug.Log("[BestiaryUI] Refreshing bestiary display.");
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
            Destroy(contentContainer.GetChild(i).gameObject);

        int unlocked = BestiaryManager.UnlockedLoreCount();
        if (summaryText != null)
            summaryText.text = $"Lore Unlocked: {unlocked}/6  |  Kill enemies to reveal tactical tips";

        var entries = BestiaryManager.GetAllEntries();
        foreach (var entry in entries)
        {
            CreateEnemyRow(entry);
        }

        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    private void CreateEnemyRow(BestiaryEntry entry)
    {
        // Row container
        var rowObj = new GameObject("Enemy_" + entry.enemyName.Replace(" ", ""));
        rowObj.transform.SetParent(contentContainer, false);
        var rowImg = rowObj.AddComponent<Image>();
        rowImg.color = entry.loreUnlocked ? unlockedColor : rowColor;

        var rowVlg = rowObj.AddComponent<VerticalLayoutGroup>();
        rowVlg.spacing = 4;
        rowVlg.padding = new RectOffset(12, 12, 8, 8);
        rowVlg.childAlignment = TextAnchor.UpperLeft;
        rowVlg.childControlWidth = true;
        rowVlg.childControlHeight = true;
        rowVlg.childForceExpandWidth = true;
        rowVlg.childForceExpandHeight = false;

        var rowLe = rowObj.AddComponent<LayoutElement>();
        rowLe.flexibleWidth = 1;

        // Header row (name + type + kills)
        var headerObj = new GameObject("Header");
        headerObj.transform.SetParent(rowObj.transform, false);
        var headerHlg = headerObj.AddComponent<HorizontalLayoutGroup>();
        headerHlg.spacing = 10;
        headerHlg.childAlignment = TextAnchor.MiddleLeft;
        headerHlg.childControlWidth = true;
        headerHlg.childControlHeight = true;
        headerHlg.childForceExpandWidth = false;
        headerHlg.childForceExpandHeight = true;
        var headerLe = headerObj.AddComponent<LayoutElement>();
        headerLe.preferredHeight = 28;

        // Enemy name
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(headerObj.transform, false);
        var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = entry.enemyName;
        nameTmp.fontSize = 22;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = entry.loreUnlocked ? goldColor : textColor;
        nameTmp.alignment = TextAlignmentOptions.Left;
        var nameLe = nameObj.AddComponent<LayoutElement>();
        nameLe.flexibleWidth = 1;

        // Enemy type badge
        var typeObj = new GameObject("Type");
        typeObj.transform.SetParent(headerObj.transform, false);
        var typeTmp = typeObj.AddComponent<TextMeshProUGUI>();
        typeTmp.text = $"[{entry.enemyType}]";
        typeTmp.fontSize = 16;
        typeTmp.color = typeColor;
        typeTmp.alignment = TextAlignmentOptions.Right;
        var typeLe = typeObj.AddComponent<LayoutElement>();
        typeLe.preferredWidth = 120;

        // Kill count
        var killObj = new GameObject("Kills");
        killObj.transform.SetParent(headerObj.transform, false);
        var killTmp = killObj.AddComponent<TextMeshProUGUI>();
        killTmp.text = $"Kills: {entry.killCount}";
        killTmp.fontSize = 16;
        killTmp.color = textColor;
        killTmp.alignment = TextAlignmentOptions.Right;
        var killLe = killObj.AddComponent<LayoutElement>();
        killLe.preferredWidth = 100;

        // Lore progress bar
        float progress = BestiaryManager.GetUnlockProgress(entry.enemyName);

        var progRowObj = new GameObject("ProgressRow");
        progRowObj.transform.SetParent(rowObj.transform, false);
        var progRowHlg = progRowObj.AddComponent<HorizontalLayoutGroup>();
        progRowHlg.spacing = 8;
        progRowHlg.childAlignment = TextAnchor.MiddleLeft;
        progRowHlg.childControlWidth = true;
        progRowHlg.childControlHeight = true;
        progRowHlg.childForceExpandHeight = true;
        var progRowLe = progRowObj.AddComponent<LayoutElement>();
        progRowLe.preferredHeight = 14;

        // Progress label
        var progLabelObj = new GameObject("Label");
        progLabelObj.transform.SetParent(progRowObj.transform, false);
        var progLabelTmp = progLabelObj.AddComponent<TextMeshProUGUI>();
        progLabelTmp.text = "Lore:";
        progLabelTmp.fontSize = 13;
        progLabelTmp.color = dimTextColor;
        progLabelTmp.alignment = TextAlignmentOptions.Left;
        var progLabelLe = progLabelObj.AddComponent<LayoutElement>();
        progLabelLe.preferredWidth = 40;

        // Progress bar bg
        var barBgObj = new GameObject("BarBg");
        barBgObj.transform.SetParent(progRowObj.transform, false);
        var barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.color = progressBgColor;
        var barBgLe = barBgObj.AddComponent<LayoutElement>();
        barBgLe.flexibleWidth = 1;
        barBgLe.preferredHeight = 10;

        // Progress bar fill
        var barFillObj = new GameObject("Fill");
        barFillObj.transform.SetParent(barBgObj.transform, false);
        var barFillImg = barFillObj.AddComponent<Image>();
        barFillImg.color = entry.loreUnlocked ? goldColor : progressFillColor;
        var barFillRect = barFillObj.GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = new Vector2(progress, 1f);
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;

        // Progress percentage
        var progPctObj = new GameObject("Pct");
        progPctObj.transform.SetParent(progRowObj.transform, false);
        var progPctTmp = progPctObj.AddComponent<TextMeshProUGUI>();
        progPctTmp.text = entry.loreUnlocked ? "UNLOCKED" : $"{progress:P0}";
        progPctTmp.fontSize = 13;
        progPctTmp.color = entry.loreUnlocked ? goldColor : dimTextColor;
        progPctTmp.alignment = TextAlignmentOptions.Right;
        var progPctLe = progPctObj.AddComponent<LayoutElement>();
        progPctLe.preferredWidth = 80;

        // Lore tip (shown if unlocked, or locked message)
        var tipObj = new GameObject("Tip");
        tipObj.transform.SetParent(rowObj.transform, false);
        var tipTmp = tipObj.AddComponent<TextMeshProUGUI>();
        if (entry.loreUnlocked)
        {
            tipTmp.text = $"<color=#{ColorUtility.ToHtmlStringRGB(tipColor)}>TIP:</color> {entry.loreTip}";
            tipTmp.richText = true;
        }
        else
        {
            tipTmp.text = entry.loreTip; // "Kill X more to unlock lore"
        }
        tipTmp.fontSize = 14;
        tipTmp.color = entry.loreUnlocked ? textColor : dimTextColor;
        tipTmp.alignment = TextAlignmentOptions.Left;
        var tipLe = tipObj.AddComponent<LayoutElement>();
        tipLe.preferredHeight = entry.loreUnlocked ? 36 : 20;
        tipLe.flexibleWidth = 1;
    }

    private void BuildUI()
    {
        Debug.Log("[BestiaryUI] Building bestiary panel UI.");

        // Canvas
        var canvasObj = new GameObject("BestiaryCanvas");
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
        dimImg.raycastTarget = true;
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

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(dialogPanel.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "BESTIARY";
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

        // Summary text
        var summObj = new GameObject("Summary");
        summObj.transform.SetParent(dialogPanel.transform, false);
        summaryText = summObj.AddComponent<TextMeshProUGUI>();
        summaryText.fontSize = 18;
        summaryText.color = textColor;
        summaryText.alignment = TextAlignmentOptions.Center;
        var summRect = summObj.GetComponent<RectTransform>();
        summRect.anchorMin = new Vector2(0.05f, 0.88f);
        summRect.anchorMax = new Vector2(0.95f, 0.93f);
        summRect.offsetMin = Vector2.zero;
        summRect.offsetMax = Vector2.zero;

        // ScrollRect area
        var scrollObj = new GameObject("ScrollArea");
        scrollObj.transform.SetParent(dialogPanel.transform, false);
        var scrollRectTr = scrollObj.AddComponent<RectTransform>();
        scrollRectTr.anchorMin = new Vector2(0.03f, 0.1f);
        scrollRectTr.anchorMax = new Vector2(0.97f, 0.87f);
        scrollRectTr.offsetMin = Vector2.zero;
        scrollRectTr.offsetMax = Vector2.zero;
        scrollObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.2f);
        var mask = scrollObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // Content container
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);
        contentContainer = contentObj.AddComponent<RectTransform>();
        contentContainer.anchorMin = new Vector2(0, 1);
        contentContainer.anchorMax = new Vector2(1, 1);
        contentContainer.pivot = new Vector2(0.5f, 1);
        contentContainer.sizeDelta = Vector2.zero;
        contentContainer.anchoredPosition = Vector2.zero;

        var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentContainer;

        // Close button
        var closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(dialogPanel.transform, false);
        var closeImg = closeObj.AddComponent<Image>();
        closeImg.color = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        var closeBtn = closeObj.AddComponent<Button>();
        var closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.35f, 0.01f);
        closeRect.anchorMax = new Vector2(0.65f, 0.07f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;

        var closeBtnColors = closeBtn.colors;
        closeBtnColors.normalColor = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        closeBtnColors.highlightedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        closeBtnColors.pressedColor = new Color(0.6f, 0.4f, 0.1f, 1f);
        closeBtnColors.selectedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        closeBtn.colors = closeBtnColors;
        closeBtn.onClick.AddListener(Hide);

        var closeTxtObj = new GameObject("Text");
        closeTxtObj.transform.SetParent(closeObj.transform, false);
        var closeTmp = closeTxtObj.AddComponent<TextMeshProUGUI>();
        closeTmp.text = "CLOSE";
        closeTmp.fontSize = 26;
        closeTmp.fontStyle = FontStyles.Bold;
        closeTmp.color = goldColor;
        closeTmp.alignment = TextAlignmentOptions.Center;
        var closeTxtRect = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRect.anchorMin = Vector2.zero;
        closeTxtRect.anchorMax = Vector2.one;
        closeTxtRect.offsetMin = Vector2.zero;
        closeTxtRect.offsetMax = Vector2.zero;

        canvas.gameObject.SetActive(false);
        Debug.Log("[BestiaryUI] UI built.");
    }
}
