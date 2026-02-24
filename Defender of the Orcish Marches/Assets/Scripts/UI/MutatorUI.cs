using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Code-generated mutator selection panel for the main menu.
/// Displays a scrollable list of mutators that can be toggled on/off.
/// Follows BugReportPanel/StatsDashboardPanel BuildUI() pattern.
/// </summary>
public class MutatorUI : MonoBehaviour
{
    private Canvas canvas;
    private GameObject dialogPanel;
    private RectTransform contentContainer;
    private ScrollRect scrollRect;
    private TextMeshProUGUI multiplierText;

    // Colors
    private static readonly Color bgColor = new Color(0.15f, 0.12f, 0.08f, 0.95f);
    private static readonly Color goldColor = new Color(0.9f, 0.75f, 0.3f);
    private static readonly Color textColor = new Color(0.85f, 0.82f, 0.75f);
    private static readonly Color dimTextColor = new Color(0.55f, 0.52f, 0.45f);
    private static readonly Color lockedColor = new Color(0.5f, 0.3f, 0.3f);

    public void Show()
    {
        Debug.Log("[MutatorUI] Showing mutator panel.");
        if (canvas == null)
            BuildUI();
        canvas.gameObject.SetActive(true);
        RefreshAll();
    }

    public void Hide()
    {
        Debug.Log("[MutatorUI] Hiding mutator panel.");
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void RefreshAll()
    {
        Debug.Log("[MutatorUI] Refreshing all mutator rows.");
        // Clear content
        for (int i = contentContainer.childCount - 1; i >= 0; i--)
            Destroy(contentContainer.GetChild(i).gameObject);

        // Rebuild mutator rows
        for (int i = 0; i < MutatorDefs.All.Length; i++)
        {
            var def = MutatorDefs.All[i];
            CreateMutatorRow(def);
        }

        // Update combined multiplier display
        float combined = MutatorManager.GetScoreMultiplier();
        multiplierText.text = $"Score Multiplier: {combined:F2}x";
        Debug.Log($"[MutatorUI] Combined score multiplier: {combined:F2}x");

        // Reset scroll to top
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    private void CreateMutatorRow(MutatorDef def)
    {
        bool unlocked = MutatorManager.IsUnlocked(def.id);
        bool active = MutatorManager.IsActive(def.id);

        // Row container
        var rowObj = new GameObject("Row_" + def.id);
        rowObj.transform.SetParent(contentContainer, false);

        var rowHlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        rowHlg.spacing = 10;
        rowHlg.childAlignment = TextAnchor.MiddleLeft;
        rowHlg.childControlWidth = true;
        rowHlg.childControlHeight = true;
        rowHlg.childForceExpandWidth = false;
        rowHlg.childForceExpandHeight = true;
        rowHlg.padding = new RectOffset(10, 10, 4, 4);

        var rowLe = rowObj.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 60;
        rowLe.flexibleWidth = 1;

        // Subtle row background
        var rowImg = rowObj.AddComponent<Image>();
        rowImg.color = new Color(0, 0, 0, 0.15f);

        // --- Toggle checkbox ---
        var toggleObj = new GameObject("Toggle");
        toggleObj.transform.SetParent(rowObj.transform, false);
        var toggleLe = toggleObj.AddComponent<LayoutElement>();
        toggleLe.preferredWidth = 36;
        toggleLe.preferredHeight = 36;

        // Toggle background
        var toggleBgImg = toggleObj.AddComponent<Image>();
        toggleBgImg.color = new Color(0.2f, 0.18f, 0.12f, 1f);

        var toggle = toggleObj.AddComponent<Toggle>();

        // Checkmark
        var checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(toggleObj.transform, false);
        var checkTmp = checkObj.AddComponent<TextMeshProUGUI>();
        checkTmp.text = "\u2714"; // checkmark unicode
        checkTmp.fontSize = 24;
        checkTmp.fontStyle = FontStyles.Bold;
        checkTmp.color = goldColor;
        checkTmp.alignment = TextAlignmentOptions.Center;
        var checkRect = checkObj.GetComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;

        toggle.isOn = active;
        toggle.graphic = checkTmp;
        toggle.targetGraphic = toggleBgImg;

        if (!unlocked)
        {
            toggle.interactable = false;
            toggleBgImg.color = new Color(0.15f, 0.12f, 0.1f, 0.5f);
        }
        else
        {
            // Capture id for closure
            string mutatorId = def.id;
            toggle.onValueChanged.AddListener((bool isOn) =>
            {
                Debug.Log($"[MutatorUI] Toggle changed for '{mutatorId}': {isOn}");
                MutatorManager.ToggleActive(mutatorId);
                RefreshAll();
            });
        }

        // --- Name + Description column ---
        var textCol = new GameObject("TextColumn");
        textCol.transform.SetParent(rowObj.transform, false);
        var textColLe = textCol.AddComponent<LayoutElement>();
        textColLe.flexibleWidth = 1;
        textColLe.preferredHeight = 50;

        var textColVlg = textCol.AddComponent<VerticalLayoutGroup>();
        textColVlg.spacing = 2;
        textColVlg.childAlignment = TextAnchor.MiddleLeft;
        textColVlg.childControlWidth = true;
        textColVlg.childControlHeight = true;
        textColVlg.childForceExpandWidth = true;
        textColVlg.childForceExpandHeight = false;

        // Mutator name
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(textCol.transform, false);
        var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = def.name;
        nameTmp.fontSize = 20;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = unlocked ? goldColor : dimTextColor;
        nameTmp.alignment = TextAlignmentOptions.Left;
        var nameLe = nameObj.AddComponent<LayoutElement>();
        nameLe.preferredHeight = 24;

        // Description
        var descObj = new GameObject("Description");
        descObj.transform.SetParent(textCol.transform, false);
        var descTmp = descObj.AddComponent<TextMeshProUGUI>();
        descTmp.text = def.description;
        descTmp.fontSize = 15;
        descTmp.color = dimTextColor;
        descTmp.alignment = TextAlignmentOptions.Left;
        var descLe = descObj.AddComponent<LayoutElement>();
        descLe.preferredHeight = 20;

        // --- Right-side info column ---
        var rightCol = new GameObject("RightColumn");
        rightCol.transform.SetParent(rowObj.transform, false);
        var rightColLe = rightCol.AddComponent<LayoutElement>();
        rightColLe.preferredWidth = 90;
        rightColLe.preferredHeight = 50;

        var rightColVlg = rightCol.AddComponent<VerticalLayoutGroup>();
        rightColVlg.spacing = 2;
        rightColVlg.childAlignment = TextAnchor.MiddleCenter;
        rightColVlg.childControlWidth = true;
        rightColVlg.childControlHeight = true;
        rightColVlg.childForceExpandWidth = true;
        rightColVlg.childForceExpandHeight = false;

        // Score multiplier badge
        var badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(rightCol.transform, false);

        var badgeBg = badgeObj.AddComponent<Image>();
        badgeBg.color = new Color(0.25f, 0.2f, 0.1f, 0.8f);

        var badgeLe = badgeObj.AddComponent<LayoutElement>();
        badgeLe.preferredHeight = 24;

        var badgeTextObj = new GameObject("BadgeText");
        badgeTextObj.transform.SetParent(badgeObj.transform, false);
        var badgeTmp = badgeTextObj.AddComponent<TextMeshProUGUI>();
        badgeTmp.text = $"{def.scoreMultiplier:F1}x";
        badgeTmp.fontSize = 18;
        badgeTmp.fontStyle = FontStyles.Bold;
        badgeTmp.color = goldColor;
        badgeTmp.alignment = TextAlignmentOptions.Center;
        var badgeTextRect = badgeTextObj.GetComponent<RectTransform>();
        badgeTextRect.anchorMin = Vector2.zero;
        badgeTextRect.anchorMax = Vector2.one;
        badgeTextRect.offsetMin = Vector2.zero;
        badgeTextRect.offsetMax = Vector2.zero;

        // Locked indicator (if locked)
        if (!unlocked)
        {
            var lockedObj = new GameObject("LockedText");
            lockedObj.transform.SetParent(rightCol.transform, false);
            var lockedTmp = lockedObj.AddComponent<TextMeshProUGUI>();
            lockedTmp.text = "Locked";
            lockedTmp.fontSize = 15;
            lockedTmp.fontStyle = FontStyles.Bold;
            lockedTmp.color = lockedColor;
            lockedTmp.alignment = TextAlignmentOptions.Center;
            var lockedLe = lockedObj.AddComponent<LayoutElement>();
            lockedLe.preferredHeight = 20;
        }
    }

    // ===================== BUILD UI =====================

    private void BuildUI()
    {
        Debug.Log("[MutatorUI] Building UI.");

        // Overlay canvas
        var canvasObj = new GameObject("MutatorUICanvas");
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
        titleTmp.text = "MUTATORS";
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

        // "UNLOCK ALL" debug button (small, top-right corner)
        var unlockAllBtn = CreateSmallButton("UnlockAllButton", "UNLOCK ALL", panelObj.transform);
        var unlockAllRect = unlockAllBtn.GetComponent<RectTransform>();
        unlockAllRect.anchorMin = new Vector2(1, 1);
        unlockAllRect.anchorMax = new Vector2(1, 1);
        unlockAllRect.pivot = new Vector2(1, 1);
        unlockAllRect.sizeDelta = new Vector2(130, 30);
        unlockAllRect.anchoredPosition = new Vector2(-10, -15);
        unlockAllBtn.onClick.AddListener(() =>
        {
            Debug.Log("[MutatorUI] UNLOCK ALL pressed (debug).");
            MutatorManager.UnlockAll();
            RefreshAll();
        });

        // ScrollRect area
        var scrollObj = new GameObject("ScrollArea");
        scrollObj.transform.SetParent(panelObj.transform, false);
        var scrollRectTransform = scrollObj.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.03f, 0.15f);
        scrollRectTransform.anchorMax = new Vector2(0.97f, 0.9f);
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
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(6, 6, 6, 6);

        var csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentContainer;

        // Combined multiplier display (bottom bar, above close button)
        var multiplierObj = new GameObject("MultiplierDisplay");
        multiplierObj.transform.SetParent(panelObj.transform, false);
        multiplierText = multiplierObj.AddComponent<TextMeshProUGUI>();
        multiplierText.text = "Score Multiplier: 1.00x";
        multiplierText.fontSize = 24;
        multiplierText.fontStyle = FontStyles.Bold;
        multiplierText.color = goldColor;
        multiplierText.alignment = TextAlignmentOptions.Center;
        var multiplierRect = multiplierObj.GetComponent<RectTransform>();
        multiplierRect.anchorMin = new Vector2(0.1f, 0.08f);
        multiplierRect.anchorMax = new Vector2(0.9f, 0.14f);
        multiplierRect.offsetMin = Vector2.zero;
        multiplierRect.offsetMax = Vector2.zero;

        // Close button
        var closeBtn = CreateDialogButton("CloseButton", "CLOSE", panelObj.transform);
        var closeBtnRect = closeBtn.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(0.35f, 0.01f);
        closeBtnRect.anchorMax = new Vector2(0.65f, 0.07f);
        closeBtnRect.offsetMin = Vector2.zero;
        closeBtnRect.offsetMax = Vector2.zero;
        closeBtn.onClick.AddListener(Hide);

        canvas.gameObject.SetActive(false);
        Debug.Log("[MutatorUI] UI built.");
    }

    // ===================== UI HELPERS =====================

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

    private Button CreateSmallButton(string name, string label, Transform parent)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.15f, 0.1f, 0.8f);
        var btn = btnObj.AddComponent<Button>();

        var colors = btn.colors;
        colors.normalColor = new Color(0.25f, 0.15f, 0.1f, 0.8f);
        colors.highlightedColor = new Color(0.4f, 0.25f, 0.1f, 1f);
        colors.pressedColor = new Color(0.5f, 0.3f, 0.1f, 1f);
        colors.selectedColor = new Color(0.4f, 0.25f, 0.1f, 1f);
        btn.colors = colors;

        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = dimTextColor;
        tmp.alignment = TextAlignmentOptions.Center;
        var txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        return btn;
    }
}
