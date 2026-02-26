using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class UpgradePanel : MonoBehaviour
{
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject panelRoot;

    private List<UpgradeButtonEntry> buttons = new List<UpgradeButtonEntry>();
    private bool isOpen = true;

    private class UpgradeButtonEntry
    {
        public UpgradeData data;
        public Button button;
        public TextMeshProUGUI label;
        public TextMeshProUGUI costText;
    }

    private bool buttonsCreated;

    private void Start()
    {
        TryCreateButtons();
    }

    // Upgrade slot actions mapped to button indices
    private static readonly GameAction[] UpgradeHotkeys = new GameAction[]
    {
        GameAction.Upgrade1, GameAction.Upgrade2, GameAction.Upgrade3,
        GameAction.Upgrade4, GameAction.Upgrade5, GameAction.Upgrade6,
        GameAction.Upgrade7, GameAction.Upgrade8, GameAction.Upgrade9
    };

    private void Update()
    {
        // Retry button creation if it failed on Start (singleton timing)
        if (!buttonsCreated)
        {
            TryCreateButtons();
        }

        if (InputBindingManager.Instance == null) { UpdateButtonStates(); return; }

        // Toggle panel
        if (InputBindingManager.Instance.WasPressedThisFrame(GameAction.ToggleUpgrades))
        {
            isOpen = !isOpen;
            if (panelRoot != null) panelRoot.SetActive(isOpen);
        }

        // Hotkeys to purchase upgrades
        if (buttonsCreated)
        {
            for (int i = 0; i < buttons.Count && i < UpgradeHotkeys.Length; i++)
            {
                if (InputBindingManager.Instance.WasPressedThisFrame(UpgradeHotkeys[i]))
                {
                    OnUpgradeClicked(buttons[i].data);
                    break;
                }
            }
        }

        UpdateButtonStates();
    }

    private void TryCreateButtons()
    {
        if (UpgradeManager.Instance != null)
        {
            CreateButtons();
            buttonsCreated = true;
            Debug.Log($"[UpgradePanel] Created {buttons.Count} upgrade buttons.");
        }
    }

    private void CreateButtons()
    {
        if (buttonPrefab == null || buttonContainer == null) return;

        int index = 0;
        foreach (var upgrade in UpgradeManager.Instance.AvailableUpgrades)
        {
            // Walls are purchased via build mode, not the upgrade panel
            if (upgrade.upgradeType == UpgradeType.NewWall) continue;

            var go = Instantiate(buttonPrefab, buttonContainer);
            go.SetActive(true);
            var entry = new UpgradeButtonEntry
            {
                data = upgrade,
                button = go.GetComponent<Button>(),
                label = go.transform.Find("Label")?.GetComponent<TextMeshProUGUI>(),
                costText = go.transform.Find("Cost")?.GetComponent<TextMeshProUGUI>()
            };

            // Show hotkey number in label (1-9)
            index++;
            if (entry.label != null)
                entry.label.text = index <= 9 ? $"[{index}] {upgrade.upgradeName}" : upgrade.upgradeName;

            if (entry.costText != null)
            {
                string cost = "";
                if (upgrade.treasureCost > 0) cost += upgrade.treasureCost + "g";
                if (upgrade.menialCost > 0) cost += " " + upgrade.menialCost + "m";
                entry.costText.text = cost.Trim();
            }

            // Color-code hire buttons to match defender colors
            Color? btnColor = GetButtonColor(upgrade.upgradeType);
            if (btnColor.HasValue)
            {
                var img = go.GetComponent<Image>();
                if (img != null) img.color = btnColor.Value;
            }

            var capturedUpgrade = upgrade;
            if (entry.button != null)
            {
                entry.button.onClick.AddListener(() => OnUpgradeClicked(capturedUpgrade));
            }

            buttons.Add(entry);
        }
    }

    private void OnUpgradeClicked(UpgradeData upgrade)
    {
        Debug.Log($"[UpgradePanel] Button clicked: {upgrade.upgradeName}, UpgradeManager.Instance={UpgradeManager.Instance != null}");
        if (UpgradeManager.Instance != null)
        {
            bool result = UpgradeManager.Instance.Purchase(upgrade);
            Debug.Log($"[UpgradePanel] Purchase result: {result}");
        }
    }

    private void UpdateButtonStates()
    {
        if (UpgradeManager.Instance == null) return;

        foreach (var entry in buttons)
        {
            if (entry.button != null)
            {
                entry.button.interactable = UpgradeManager.Instance.CanPurchase(entry.data);
            }

            // Update cost text to reflect scaling
            if (entry.costText != null)
            {
                var (treasure, menial) = UpgradeManager.Instance.GetCurrentCost(entry.data);
                string cost = "";
                if (treasure > 0) cost += treasure + "g";
                if (menial > 0) cost += " " + menial + "m";
                entry.costText.text = cost.Trim();
            }
        }
    }

    private Color? GetButtonColor(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.SpawnEngineer:    return new Color(0.9f, 0.6f, 0.1f);   // Orange
            case UpgradeType.SpawnPikeman:     return new Color(0.5f, 0.6f, 0.7f);   // Steel
            case UpgradeType.SpawnCrossbowman: return new Color(0.2f, 0.65f, 0.3f);  // Green
            case UpgradeType.SpawnWizard:      return new Color(0.53f, 0.27f, 0.8f); // Purple
            default: return null;
        }
    }
}
