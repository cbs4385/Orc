using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Modal overlay showing 3 save slots. Used by both main menu (Load) and pause menu (Save).
/// Builds its own UI at runtime (no scene authoring needed).
/// </summary>
public class SaveSlotPicker : MonoBehaviour
{
    public enum Mode { Save, Load }

    private const int SLOT_COUNT = 3;

    private Mode currentMode;
    private Action<int> onSlotSelected;
    private GameObject panel;
    private bool confirmingDelete;
    private int confirmDeleteSlot;
    private GameObject confirmPanel;

    /// <summary>Show the slot picker overlay.</summary>
    public void Show(Mode mode, Action<int> callback)
    {
        currentMode = mode;
        onSlotSelected = callback;
        BuildUI();
        panel.SetActive(true);
        Debug.Log($"[SaveSlotPicker] Opened in {mode} mode.");
    }

    /// <summary>Hide the slot picker overlay.</summary>
    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        Debug.Log("[SaveSlotPicker] Closed.");
    }

    public bool IsVisible => panel != null && panel.activeSelf;

    private void BuildUI()
    {
        // Destroy previous panel if it exists
        if (panel != null) Destroy(panel);

        // Find or create canvas
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[SaveSlotPicker] No Canvas found in scene.");
            return;
        }

        // Full-screen overlay
        panel = new GameObject("SaveSlotPanel");
        panel.transform.SetParent(canvas.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Dimmed background
        var bgImage = panel.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);

        // Center container
        var container = new GameObject("Container");
        container.transform.SetParent(panel.transform, false);
        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(700, 500);
        containerRect.anchoredPosition = Vector2.zero;

        var containerImage = container.AddComponent<Image>();
        containerImage.color = new Color(0.12f, 0.1f, 0.08f, 0.95f);

        // Title
        var titleObj = CreateText("Title",
            currentMode == Mode.Save ? "SAVE GAME" : "LOAD GAME",
            container.transform, 36, new Color(0.9f, 0.75f, 0.3f));
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.85f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // Slot buttons
        for (int i = 0; i < SLOT_COUNT; i++)
        {
            CreateSlotRow(container.transform, i);
        }

        // Cancel button
        var cancelBtn = CreateButton("Cancel", "CANCEL", container.transform, () =>
        {
            Hide();
        });
        var cancelRect = cancelBtn.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.35f, 0.02f);
        cancelRect.anchorMax = new Vector2(0.65f, 0.12f);
        cancelRect.offsetMin = Vector2.zero;
        cancelRect.offsetMax = Vector2.zero;
    }

    private void CreateSlotRow(Transform parent, int slotIndex)
    {
        float rowTop = 0.82f - slotIndex * 0.25f;
        float rowBottom = rowTop - 0.22f;

        var row = new GameObject($"Slot_{slotIndex}");
        row.transform.SetParent(parent, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.03f, rowBottom);
        rowRect.anchorMax = new Vector2(0.97f, rowTop);
        rowRect.offsetMin = Vector2.zero;
        rowRect.offsetMax = Vector2.zero;

        var rowImage = row.AddComponent<Image>();
        rowImage.color = new Color(0.2f, 0.17f, 0.12f, 0.9f);

        // Read slot metadata
        var meta = SaveManager.GetSlotMetadata(slotIndex);
        bool hasData = meta != null;

        // Slot info text
        string infoText;
        if (hasData)
        {
            string diffName = ((Difficulty)meta.metaDifficulty).ToString();
            infoText = $"Slot {slotIndex + 1}: Day {meta.metaDayNumber} - {diffName} - {meta.metaTreasure}g\n{meta.timestamp}";
        }
        else
        {
            infoText = $"Slot {slotIndex + 1}: Empty";
        }

        var infoObj = CreateText($"Info_{slotIndex}", infoText, row.transform, 22, Color.white);
        var infoRect = infoObj.GetComponent<RectTransform>();
        infoRect.anchorMin = new Vector2(0.02f, 0f);
        infoRect.anchorMax = new Vector2(0.6f, 1f);
        infoRect.offsetMin = Vector2.zero;
        infoRect.offsetMax = Vector2.zero;
        var infoTmp = infoObj.GetComponent<TextMeshProUGUI>();
        infoTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Action button (Save/Load)
        int capturedSlot = slotIndex;
        string actionLabel = currentMode == Mode.Save ? "SAVE" : "LOAD";
        bool actionEnabled = currentMode == Mode.Save || hasData;

        var actionBtn = CreateButton($"Action_{slotIndex}", actionLabel, row.transform, () =>
        {
            OnSlotAction(capturedSlot);
        });
        var actionRect = actionBtn.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0.62f, 0.15f);
        actionRect.anchorMax = new Vector2(0.8f, 0.85f);
        actionRect.offsetMin = Vector2.zero;
        actionRect.offsetMax = Vector2.zero;

        if (!actionEnabled)
        {
            actionBtn.GetComponent<Button>().interactable = false;
            var btnImg = actionBtn.GetComponent<Image>();
            if (btnImg != null) btnImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }

        // Delete button (only if slot has data)
        if (hasData)
        {
            var deleteBtn = CreateButton($"Delete_{slotIndex}", "DEL", row.transform, () =>
            {
                OnDeleteSlot(capturedSlot);
            });
            var deleteRect = deleteBtn.GetComponent<RectTransform>();
            deleteRect.anchorMin = new Vector2(0.82f, 0.15f);
            deleteRect.anchorMax = new Vector2(0.98f, 0.85f);
            deleteRect.offsetMin = Vector2.zero;
            deleteRect.offsetMax = Vector2.zero;
            var delImg = deleteBtn.GetComponent<Image>();
            if (delImg != null) delImg.color = new Color(0.5f, 0.15f, 0.1f, 0.9f);
        }
    }

    private void OnSlotAction(int slot)
    {
        if (currentMode == Mode.Save)
        {
            // Check if slot has existing data — show confirmation
            if (SaveManager.SlotExists(slot))
            {
                ShowConfirmOverwrite(slot);
                return;
            }
        }

        Debug.Log($"[SaveSlotPicker] Slot {slot} selected ({currentMode}).");
        Hide();
        onSlotSelected?.Invoke(slot);
    }

    private void OnDeleteSlot(int slot)
    {
        ShowConfirmDelete(slot);
    }

    private void ShowConfirmOverwrite(int slot)
    {
        ShowConfirmDialog($"Overwrite Slot {slot + 1}?", () =>
        {
            Debug.Log($"[SaveSlotPicker] Overwriting slot {slot}.");
            DismissConfirm();
            Hide();
            onSlotSelected?.Invoke(slot);
        });
    }

    private void ShowConfirmDelete(int slot)
    {
        ShowConfirmDialog($"Delete Slot {slot + 1}?", () =>
        {
            SaveManager.DeleteSlot(slot);
            Debug.Log($"[SaveSlotPicker] Deleted slot {slot}.");
            DismissConfirm();
            // Rebuild UI to reflect deletion
            BuildUI();
            panel.SetActive(true);
        });
    }

    private void ShowConfirmDialog(string message, Action onConfirm)
    {
        DismissConfirm();

        confirmPanel = new GameObject("ConfirmDialog");
        confirmPanel.transform.SetParent(panel.transform, false);
        var confirmRect = confirmPanel.AddComponent<RectTransform>();
        confirmRect.anchorMin = Vector2.zero;
        confirmRect.anchorMax = Vector2.one;
        confirmRect.offsetMin = Vector2.zero;
        confirmRect.offsetMax = Vector2.zero;

        // Dim overlay
        var dimImage = confirmPanel.AddComponent<Image>();
        dimImage.color = new Color(0, 0, 0, 0.6f);

        // Dialog box
        var dialogBox = new GameObject("DialogBox");
        dialogBox.transform.SetParent(confirmPanel.transform, false);
        var dialogRect = dialogBox.AddComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.sizeDelta = new Vector2(400, 180);
        var dialogImage = dialogBox.AddComponent<Image>();
        dialogImage.color = new Color(0.15f, 0.12f, 0.1f, 0.98f);

        // Message
        var msgObj = CreateText("ConfirmMsg", message, dialogBox.transform, 28, Color.white);
        var msgRect = msgObj.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0f, 0.5f);
        msgRect.anchorMax = new Vector2(1f, 0.95f);
        msgRect.offsetMin = Vector2.zero;
        msgRect.offsetMax = Vector2.zero;

        // Yes button
        var yesBtn = CreateButton("ConfirmYes", "YES", dialogBox.transform, onConfirm);
        var yesRect = yesBtn.GetComponent<RectTransform>();
        yesRect.anchorMin = new Vector2(0.1f, 0.08f);
        yesRect.anchorMax = new Vector2(0.45f, 0.42f);
        yesRect.offsetMin = Vector2.zero;
        yesRect.offsetMax = Vector2.zero;

        // No button
        var noBtn = CreateButton("ConfirmNo", "NO", dialogBox.transform, DismissConfirm);
        var noRect = noBtn.GetComponent<RectTransform>();
        noRect.anchorMin = new Vector2(0.55f, 0.08f);
        noRect.anchorMax = new Vector2(0.9f, 0.42f);
        noRect.offsetMin = Vector2.zero;
        noRect.offsetMax = Vector2.zero;
    }

    private void DismissConfirm()
    {
        if (confirmPanel != null)
        {
            Destroy(confirmPanel);
            confirmPanel = null;
        }
    }

    // ─── UI Helpers ───

    private static GameObject CreateText(string name, string text, Transform parent, float fontSize, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return obj;
    }

    private static GameObject CreateButton(string name, string label, Transform parent, Action onClick)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        var btn = btnObj.AddComponent<Button>();

        var colors = btn.colors;
        colors.normalColor = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        colors.highlightedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        colors.pressedColor = new Color(0.6f, 0.4f, 0.1f, 1f);
        colors.selectedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        btn.colors = colors;

        btn.onClick.AddListener(() => onClick?.Invoke());

        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.9f, 0.8f, 0.5f);
        tmp.alignment = TextAlignmentOptions.Center;
        var txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        return btnObj;
    }
}
