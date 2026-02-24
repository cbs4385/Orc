using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Code-generated commander selection panel for the main menu.
/// Displays commander classes with descriptions and a select button for each.
/// Follows MutatorUI/AchievementUI BuildUI() pattern.
/// </summary>
public class CommanderSelectionUI : MonoBehaviour
{
    private Canvas canvas;
    private GameObject dialogPanel;
    private RectTransform contentContainer;
    private ScrollRect scrollRect;
    private TextMeshProUGUI selectedText;

    // Colors
    private static readonly Color bgColor = new Color(0.15f, 0.12f, 0.08f, 0.95f);
    private static readonly Color goldColor = new Color(0.9f, 0.75f, 0.3f);
    private static readonly Color textColor = new Color(0.85f, 0.82f, 0.75f);
    private static readonly Color dimTextColor = new Color(0.55f, 0.52f, 0.45f);
    private static readonly Color activeColor = new Color(0.3f, 0.5f, 0.25f, 0.6f);
    private static readonly Color rowColor = new Color(0, 0, 0, 0.15f);

    public void Show()
    {
        Debug.Log("[CommanderSelectionUI] Showing commander panel.");
        CommanderManager.LoadSelection();
        if (canvas == null)
            BuildUI();
        canvas.gameObject.SetActive(true);
        RefreshAll();
    }

    public void Hide()
    {
        Debug.Log("[CommanderSelectionUI] Hidden.");
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void RefreshAll()
    {
        Debug.Log("[CommanderSelectionUI] Refreshing commander display.");
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
            Destroy(contentContainer.GetChild(i).gameObject);

        if (selectedText != null)
            selectedText.text = $"Active Commander: {CommanderManager.GetActiveDisplayName()}";

        // "None" option
        CreateCommanderRow(CommanderDefs.NONE_ID, "None", "No commander selected. Play with default settings.");

        // Commander classes
        foreach (var def in CommanderDefs.All)
        {
            CreateCommanderRow(def.id, def.name, def.description);
        }

        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    private void CreateCommanderRow(string commanderId, string displayName, string description)
    {
        bool isActive = CommanderManager.ActiveCommanderId == commanderId;

        // Row container
        var rowObj = new GameObject("Cmd_" + commanderId);
        rowObj.transform.SetParent(contentContainer, false);
        var rowImg = rowObj.AddComponent<Image>();
        rowImg.color = isActive ? activeColor : rowColor;
        var rowLe = rowObj.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 80;
        rowLe.flexibleWidth = 1;

        var rowHlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        rowHlg.spacing = 10;
        rowHlg.padding = new RectOffset(10, 10, 5, 5);
        rowHlg.childAlignment = TextAnchor.MiddleLeft;
        rowHlg.childControlWidth = true;
        rowHlg.childControlHeight = true;
        rowHlg.childForceExpandWidth = false;
        rowHlg.childForceExpandHeight = true;

        // Active indicator
        var indicatorObj = new GameObject("Indicator");
        indicatorObj.transform.SetParent(rowObj.transform, false);
        var indicatorImg = indicatorObj.AddComponent<Image>();
        indicatorImg.color = isActive ? goldColor : new Color(0.3f, 0.28f, 0.25f);
        var indicatorLe = indicatorObj.AddComponent<LayoutElement>();
        indicatorLe.preferredWidth = 6;
        indicatorLe.flexibleHeight = 1;

        // Info column
        var infoObj = new GameObject("Info");
        infoObj.transform.SetParent(rowObj.transform, false);
        var infoVlg = infoObj.AddComponent<VerticalLayoutGroup>();
        infoVlg.spacing = 2;
        infoVlg.childAlignment = TextAnchor.MiddleLeft;
        infoVlg.childControlWidth = true;
        infoVlg.childControlHeight = true;
        infoVlg.childForceExpandWidth = true;
        infoVlg.childForceExpandHeight = false;
        var infoLe = infoObj.AddComponent<LayoutElement>();
        infoLe.flexibleWidth = 1;

        // Name
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(infoObj.transform, false);
        var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = displayName;
        nameTmp.fontSize = 22;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = isActive ? goldColor : textColor;
        nameTmp.alignment = TextAlignmentOptions.Left;
        var nameLe = nameObj.AddComponent<LayoutElement>();
        nameLe.preferredHeight = 26;

        // Description
        var descObj = new GameObject("Desc");
        descObj.transform.SetParent(infoObj.transform, false);
        var descTmp = descObj.AddComponent<TextMeshProUGUI>();
        descTmp.text = description;
        descTmp.fontSize = 15;
        descTmp.color = dimTextColor;
        descTmp.alignment = TextAlignmentOptions.Left;
        var descLe = descObj.AddComponent<LayoutElement>();
        descLe.preferredHeight = 36;

        // Select button
        var btnObj = new GameObject("SelectBtn");
        btnObj.transform.SetParent(rowObj.transform, false);
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = isActive ? new Color(0.4f, 0.55f, 0.3f, 0.9f) : new Color(0.3f, 0.2f, 0.1f, 0.9f);
        var btn = btnObj.AddComponent<Button>();
        var btnLe = btnObj.AddComponent<LayoutElement>();
        btnLe.preferredWidth = 100;
        btnLe.preferredHeight = 40;

        var btnColors = btn.colors;
        btnColors.normalColor = btnImg.color;
        btnColors.highlightedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        btnColors.pressedColor = new Color(0.6f, 0.4f, 0.1f, 1f);
        btnColors.selectedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        btn.colors = btnColors;

        var btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnTmp = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnTmp.text = isActive ? "ACTIVE" : "SELECT";
        btnTmp.fontSize = 18;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.color = isActive ? new Color(0.9f, 1f, 0.9f) : goldColor;
        btnTmp.alignment = TextAlignmentOptions.Center;
        var btnTxtRect = btnTextObj.GetComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero;
        btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.offsetMin = Vector2.zero;
        btnTxtRect.offsetMax = Vector2.zero;

        if (!isActive)
        {
            string id = commanderId;
            btn.onClick.AddListener(() =>
            {
                CommanderManager.SelectCommander(id);
                Debug.Log($"[CommanderSelectionUI] Commander selected: {id}");
                RefreshAll();
            });
        }
        else
        {
            btn.interactable = false;
        }
    }

    private void BuildUI()
    {
        Debug.Log("[CommanderSelectionUI] Building commander panel UI.");

        // Canvas
        var canvasObj = new GameObject("CommanderCanvas");
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
        panelRect.anchorMin = new Vector2(0.15f, 0.05f);
        panelRect.anchorMax = new Vector2(0.85f, 0.95f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(dialogPanel.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "COMMANDER";
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

        // Selected commander display
        var selectedObj = new GameObject("SelectedText");
        selectedObj.transform.SetParent(dialogPanel.transform, false);
        selectedText = selectedObj.AddComponent<TextMeshProUGUI>();
        selectedText.text = $"Active Commander: {CommanderManager.GetActiveDisplayName()}";
        selectedText.fontSize = 20;
        selectedText.color = textColor;
        selectedText.alignment = TextAlignmentOptions.Center;
        var selRect = selectedObj.GetComponent<RectTransform>();
        selRect.anchorMin = new Vector2(0.05f, 0.88f);
        selRect.anchorMax = new Vector2(0.95f, 0.93f);
        selRect.offsetMin = Vector2.zero;
        selRect.offsetMax = Vector2.zero;

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
        Debug.Log("[CommanderSelectionUI] UI built.");
    }
}
