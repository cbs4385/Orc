using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Code-generated meta-progression panel for the main menu.
/// Displays War Trophy balance and purchasable upgrades.
/// Follows MutatorUI/AchievementUI BuildUI() pattern.
/// </summary>
public class MetaProgressionUI : MonoBehaviour
{
    private Canvas canvas;
    private GameObject dialogPanel;
    private RectTransform contentContainer;
    private ScrollRect scrollRect;
    private TextMeshProUGUI trophyText;

    // Colors
    private static readonly Color bgColor = new Color(0.15f, 0.12f, 0.08f, 0.95f);
    private static readonly Color goldColor = new Color(0.9f, 0.75f, 0.3f);
    private static readonly Color textColor = new Color(0.85f, 0.82f, 0.75f);
    private static readonly Color dimTextColor = new Color(0.55f, 0.52f, 0.45f);
    private static readonly Color rowColor = new Color(0, 0, 0, 0.15f);
    private static readonly Color maxedColor = new Color(0.3f, 0.5f, 0.25f, 0.6f);
    private static readonly Color affordableColor = new Color(0.3f, 0.35f, 0.15f, 0.9f);
    private static readonly Color unaffordableColor = new Color(0.25f, 0.18f, 0.12f, 0.6f);
    private static readonly Color progressBgColor = new Color(0.2f, 0.18f, 0.14f);
    private static readonly Color progressFillColor = new Color(0.5f, 0.4f, 0.15f);

    public void Show()
    {
        Debug.Log("[MetaProgressionUI] Showing upgrades panel.");
        if (canvas == null)
            BuildUI();
        canvas.gameObject.SetActive(true);
        RefreshDisplay();
    }

    public void Hide()
    {
        Debug.Log("[MetaProgressionUI] Hidden.");
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    public void RefreshDisplay()
    {
        Debug.Log("[MetaProgressionUI] Refreshing upgrade display.");
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
            Destroy(contentContainer.GetChild(i).gameObject);

        if (trophyText != null)
            trophyText.text = $"War Trophies: {MetaProgressionManager.WarTrophies}  (Lifetime: {MetaProgressionManager.TotalWarTrophiesEarned})";

        foreach (var upgradeDef in MetaProgressionManager.AllUpgrades)
        {
            CreateUpgradeRow(upgradeDef);
        }

        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    private void CreateUpgradeRow(MetaUpgradeDef def)
    {
        int currentLevel = MetaProgressionManager.GetUpgradeLevel(def.id);
        bool maxed = currentLevel >= def.maxLevel;
        bool canAfford = MetaProgressionManager.CanPurchaseUpgrade(def.id);
        int cost = MetaProgressionManager.GetUpgradeCost(def.id);

        // Row container
        var rowObj = new GameObject("Upg_" + def.id);
        rowObj.transform.SetParent(contentContainer, false);
        var rowImg = rowObj.AddComponent<Image>();
        rowImg.color = maxed ? maxedColor : rowColor;
        var rowLe = rowObj.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 85;
        rowLe.flexibleWidth = 1;

        var rowHlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        rowHlg.spacing = 10;
        rowHlg.padding = new RectOffset(10, 10, 5, 5);
        rowHlg.childAlignment = TextAnchor.MiddleLeft;
        rowHlg.childControlWidth = true;
        rowHlg.childControlHeight = true;
        rowHlg.childForceExpandWidth = false;
        rowHlg.childForceExpandHeight = true;

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

        // Name + level
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(infoObj.transform, false);
        var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = maxed
            ? $"{def.name} <color=#7FBF50>(MAX)</color>"
            : $"{def.name} ({currentLevel}/{def.maxLevel})";
        nameTmp.fontSize = 20;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = maxed ? goldColor : textColor;
        nameTmp.alignment = TextAlignmentOptions.Left;
        nameTmp.richText = true;
        var nameLe = nameObj.AddComponent<LayoutElement>();
        nameLe.preferredHeight = 24;

        // Description
        var descObj = new GameObject("Desc");
        descObj.transform.SetParent(infoObj.transform, false);
        var descTmp = descObj.AddComponent<TextMeshProUGUI>();
        descTmp.text = def.description;
        descTmp.fontSize = 15;
        descTmp.color = dimTextColor;
        descTmp.alignment = TextAlignmentOptions.Left;
        var descLe = descObj.AddComponent<LayoutElement>();
        descLe.preferredHeight = 20;

        // Progress bar (level visual)
        if (def.maxLevel > 1)
        {
            var progObj = new GameObject("Progress");
            progObj.transform.SetParent(infoObj.transform, false);
            var progBgImg = progObj.AddComponent<Image>();
            progBgImg.color = progressBgColor;
            var progLe = progObj.AddComponent<LayoutElement>();
            progLe.preferredHeight = 10;
            progLe.flexibleWidth = 1;

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(progObj.transform, false);
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.color = maxed ? goldColor : progressFillColor;
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2((float)currentLevel / def.maxLevel, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        // Buy button — fixed width, styled to match game buttons (MenuSceneBuilder.CreateMenuButton)
        var buyObj = new GameObject(maxed ? "MaxLabel" : "BuyBtn");
        buyObj.transform.SetParent(rowObj.transform, false);
        var buyImg = buyObj.AddComponent<Image>();
        var buyLe = buyObj.AddComponent<LayoutElement>();
        buyLe.preferredWidth = 120;
        buyLe.minWidth = 120;
        buyLe.preferredHeight = 50;

        if (maxed)
        {
            buyImg.color = new Color(0.2f, 0.3f, 0.15f, 0.9f);
            var maxTxtObj = new GameObject("Text");
            maxTxtObj.transform.SetParent(buyObj.transform, false);
            var maxTmp = maxTxtObj.AddComponent<TextMeshProUGUI>();
            maxTmp.text = "MAXED";
            maxTmp.fontSize = 20;
            maxTmp.fontStyle = FontStyles.Bold;
            maxTmp.color = new Color(0.5f, 0.7f, 0.35f);
            maxTmp.alignment = TextAlignmentOptions.Center;
            var maxTxtRect = maxTxtObj.GetComponent<RectTransform>();
            maxTxtRect.anchorMin = Vector2.zero;
            maxTxtRect.anchorMax = Vector2.one;
            maxTxtRect.offsetMin = Vector2.zero;
            maxTxtRect.offsetMax = Vector2.zero;
        }
        else
        {
            buyImg.color = canAfford ? new Color(0.3f, 0.2f, 0.1f, 0.9f) : unaffordableColor;
            var buyBtn = buyObj.AddComponent<Button>();

            var buyColors = buyBtn.colors;
            buyColors.normalColor = buyImg.color;
            buyColors.highlightedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
            buyColors.pressedColor = new Color(0.6f, 0.4f, 0.1f, 1f);
            buyColors.selectedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
            buyColors.disabledColor = unaffordableColor;
            buyBtn.colors = buyColors;
            buyBtn.interactable = canAfford;

            var buyTxtObj = new GameObject("Text");
            buyTxtObj.transform.SetParent(buyObj.transform, false);
            var buyTmp = buyTxtObj.AddComponent<TextMeshProUGUI>();
            buyTmp.text = $"BUY ({cost})";
            buyTmp.fontSize = 20;
            buyTmp.fontStyle = FontStyles.Bold;
            buyTmp.color = canAfford ? goldColor : dimTextColor;
            buyTmp.alignment = TextAlignmentOptions.Center;
            var buyTxtRect = buyTxtObj.GetComponent<RectTransform>();
            buyTxtRect.anchorMin = Vector2.zero;
            buyTxtRect.anchorMax = Vector2.one;
            buyTxtRect.offsetMin = Vector2.zero;
            buyTxtRect.offsetMax = Vector2.zero;

            // Drop shadow matching menu buttons
            var buyTxtShadow = buyTxtObj.AddComponent<Shadow>();
            buyTxtShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            buyTxtShadow.effectDistance = new Vector2(2f, -2f);

            string upgradeId = def.id;
            buyBtn.onClick.AddListener(() =>
            {
                if (MetaProgressionManager.PurchaseUpgrade(upgradeId))
                {
                    Debug.Log($"[MetaProgressionUI] Purchased upgrade: {upgradeId}");
                    RefreshDisplay();
                }
            });
        }
    }

    private void BuildUI()
    {
        Debug.Log("[MetaProgressionUI] Building upgrades panel UI.");

        // Canvas
        var canvasObj = new GameObject("MetaProgressionCanvas");
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
        panelRect.anchorMin = new Vector2(0.12f, 0.05f);
        panelRect.anchorMax = new Vector2(0.88f, 0.95f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(dialogPanel.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "UPGRADES";
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

        // Trophy count
        var trophyObj = new GameObject("TrophyText");
        trophyObj.transform.SetParent(dialogPanel.transform, false);
        trophyText = trophyObj.AddComponent<TextMeshProUGUI>();
        trophyText.text = $"War Trophies: {MetaProgressionManager.WarTrophies}";
        trophyText.fontSize = 22;
        trophyText.color = goldColor;
        trophyText.alignment = TextAlignmentOptions.Center;
        var trophyRect = trophyObj.GetComponent<RectTransform>();
        trophyRect.anchorMin = new Vector2(0.05f, 0.88f);
        trophyRect.anchorMax = new Vector2(0.95f, 0.93f);
        trophyRect.offsetMin = Vector2.zero;
        trophyRect.offsetMax = Vector2.zero;

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
        vlg.spacing = 4;
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
        Debug.Log("[MetaProgressionUI] UI built.");
    }
}
