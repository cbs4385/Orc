using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Builds and manages the two-column input binding section in the Options screen.
/// Keyboard bindings on the left, Gamepad bindings on the right.
/// Each row has an action label and two rebind buttons.
/// </summary>
public class InputBindingsUI : MonoBehaviour
{
    private Transform container;
    private List<BindingRow> rows = new List<BindingRow>();
    private TextMeshProUGUI listeningOverlay;
    private GameObject overlayRoot;
    private Button resetButton;

    private struct BindingRow
    {
        public GameAction action;
        public Button keyboardButton;
        public TextMeshProUGUI keyboardLabel;
        public Button gamepadButton;
        public TextMeshProUGUI gamepadLabel;
    }

    /// <summary>Build the full bindings UI under the given parent transform.</summary>
    public void Build(Transform parent)
    {
        container = parent;
        BuildHeader();
        BuildColumnHeaders();
        BuildRows();
        BuildResetButton();
        BuildListeningOverlay();
        RefreshAllLabels();
        Debug.Log($"[InputBindingsUI] Built {rows.Count} binding rows.");
    }

    private void OnEnable()
    {
        if (InputBindingManager.Instance != null)
            InputBindingManager.Instance.OnBindingsChanged += RefreshAllLabels;
    }

    private void OnDisable()
    {
        if (InputBindingManager.Instance != null)
            InputBindingManager.Instance.OnBindingsChanged -= RefreshAllLabels;
    }

    private void BuildHeader()
    {
        var headerObj = new GameObject("InputHeader");
        headerObj.transform.SetParent(container, false);
        var tmp = headerObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "INPUT BINDINGS";
        tmp.fontSize = 36;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.8f, 0.7f, 0.5f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        var le = headerObj.AddComponent<LayoutElement>();
        le.preferredHeight = 50;
    }

    private void BuildColumnHeaders()
    {
        var rowObj = CreateRowObject("ColumnHeaders");
        CreateCellText(rowObj.transform, "Action", 0.35f, TextAlignmentOptions.MidlineLeft, true);
        CreateCellText(rowObj.transform, "Keyboard", 0.3f, TextAlignmentOptions.Center, true);
        CreateCellText(rowObj.transform, "Gamepad", 0.3f, TextAlignmentOptions.Center, true);

        var divider = new GameObject("Divider");
        divider.transform.SetParent(container, false);
        var img = divider.AddComponent<Image>();
        img.color = new Color(0.4f, 0.35f, 0.25f, 0.6f);
        img.raycastTarget = false;
        var le = divider.AddComponent<LayoutElement>();
        le.preferredHeight = 2;
    }

    private void BuildRows()
    {
        var actions = InputBindingManager.GetRebindableActions();
        foreach (var action in actions)
        {
            var row = new BindingRow { action = action };
            var rowObj = CreateRowObject($"Row_{action}");

            // Action label
            CreateCellText(rowObj.transform, InputBindingManager.GetActionDisplayName(action),
                0.35f, TextAlignmentOptions.MidlineLeft, false);

            // Keyboard rebind button
            var kbBtn = CreateRebindButton(rowObj.transform, 0.3f, out var kbLabel);
            row.keyboardButton = kbBtn;
            row.keyboardLabel = kbLabel;
            var capturedAction = action;
            kbBtn.onClick.AddListener(() => StartRebind(capturedAction, false));

            // Gamepad rebind button
            var gpBtn = CreateRebindButton(rowObj.transform, 0.3f, out var gpLabel);
            row.gamepadButton = gpBtn;
            row.gamepadLabel = gpLabel;
            gpBtn.onClick.AddListener(() => StartRebind(capturedAction, true));

            rows.Add(row);
        }
    }

    private void BuildResetButton()
    {
        var rowObj = new GameObject("ResetRow");
        rowObj.transform.SetParent(container, false);
        var le = rowObj.AddComponent<LayoutElement>();
        le.preferredHeight = 50;
        var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        // Spacer
        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(rowObj.transform, false);
        var sle = spacer.AddComponent<LayoutElement>();
        sle.flexibleWidth = 1;

        // Reset button
        var btnObj = new GameObject("ResetBindingsButton");
        btnObj.transform.SetParent(rowObj.transform, false);
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.5f, 0.2f, 0.15f, 0.9f);
        var btnLe = btnObj.AddComponent<LayoutElement>();
        btnLe.preferredWidth = 250;
        btnLe.preferredHeight = 40;
        resetButton = btnObj.AddComponent<Button>();
        var colors = resetButton.colors;
        colors.normalColor = new Color(0.5f, 0.2f, 0.15f, 0.9f);
        colors.highlightedColor = new Color(0.7f, 0.3f, 0.2f, 1f);
        colors.pressedColor = new Color(0.8f, 0.35f, 0.2f, 1f);
        resetButton.colors = colors;
        resetButton.onClick.AddListener(OnResetClicked);

        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "RESET TO DEFAULTS";
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        // Spacer
        var spacer2 = new GameObject("Spacer2");
        spacer2.transform.SetParent(rowObj.transform, false);
        var sle2 = spacer2.AddComponent<LayoutElement>();
        sle2.flexibleWidth = 1;
    }

    private void BuildListeningOverlay()
    {
        overlayRoot = new GameObject("ListeningOverlay");
        overlayRoot.transform.SetParent(container.parent, false);

        var rect = overlayRoot.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = overlayRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);
        bg.raycastTarget = true;

        var textObj = new GameObject("OverlayText");
        textObj.transform.SetParent(overlayRoot.transform, false);
        listeningOverlay = textObj.AddComponent<TextMeshProUGUI>();
        listeningOverlay.text = "Press a key to bind...\n\n<size=20>Press Escape to cancel</size>";
        listeningOverlay.fontSize = 36;
        listeningOverlay.fontStyle = FontStyles.Bold;
        listeningOverlay.color = new Color(1f, 0.85f, 0.2f);
        listeningOverlay.alignment = TextAlignmentOptions.Center;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.2f, 0.3f);
        textRect.anchorMax = new Vector2(0.8f, 0.7f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        overlayRoot.SetActive(false);
    }

    // --- Row creation helpers ---

    private GameObject CreateRowObject(string name)
    {
        var rowObj = new GameObject(name);
        rowObj.transform.SetParent(container, false);
        var le = rowObj.AddComponent<LayoutElement>();
        le.preferredHeight = 36;
        var hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 5;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = true;
        return rowObj;
    }

    private void CreateCellText(Transform parent, string text, float flexWidth,
        TextAlignmentOptions align, bool isHeader)
    {
        var obj = new GameObject("Cell");
        obj.transform.SetParent(parent, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = isHeader ? 22 : 20;
        tmp.fontStyle = isHeader ? FontStyles.Bold : FontStyles.Normal;
        tmp.color = isHeader ? new Color(0.8f, 0.7f, 0.5f) : Color.white;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        var le = obj.AddComponent<LayoutElement>();
        le.flexibleWidth = flexWidth;
    }

    private Button CreateRebindButton(Transform parent, float flexWidth, out TextMeshProUGUI label)
    {
        var btnObj = new GameObject("RebindBtn");
        btnObj.transform.SetParent(parent, false);
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.2f, 0.15f, 0.9f);
        var le = btnObj.AddComponent<LayoutElement>();
        le.flexibleWidth = flexWidth;
        var btn = btnObj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.25f, 0.2f, 0.15f, 0.9f);
        colors.highlightedColor = new Color(0.4f, 0.3f, 0.2f, 1f);
        colors.pressedColor = new Color(0.5f, 0.4f, 0.2f, 1f);
        btn.colors = colors;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        label = textObj.AddComponent<TextMeshProUGUI>();
        label.text = "---";
        label.fontSize = 20;
        label.color = new Color(1f, 0.9f, 0.6f);
        label.alignment = TextAlignmentOptions.Center;
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5, 0);
        textRect.offsetMax = new Vector2(-5, 0);

        return btn;
    }

    // --- Interaction ---

    private void StartRebind(GameAction action, bool forGamepad)
    {
        if (InputBindingManager.Instance == null) return;

        string deviceName = forGamepad ? "gamepad button" : "key";
        listeningOverlay.text = $"Press a {deviceName} for:\n<b>{InputBindingManager.GetActionDisplayName(action)}</b>\n\n<size=20>Press Escape to cancel</size>";
        overlayRoot.SetActive(true);

        InputBindingManager.Instance.StartRebind(action, forGamepad, (success) =>
        {
            overlayRoot.SetActive(false);
            if (success) RefreshAllLabels();
        });
    }

    private void OnResetClicked()
    {
        if (InputBindingManager.Instance != null)
        {
            InputBindingManager.Instance.ResetToDefaults();
            RefreshAllLabels();
        }
    }

    private void RefreshAllLabels()
    {
        if (InputBindingManager.Instance == null) return;
        foreach (var row in rows)
        {
            if (row.keyboardLabel != null)
                row.keyboardLabel.text = InputBindingManager.Instance.GetKeyboardDisplayName(row.action);
            if (row.gamepadLabel != null)
                row.gamepadLabel.text = InputBindingManager.Instance.GetGamepadDisplayName(row.action);
        }
    }
}
