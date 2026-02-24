using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel for viewing and purchasing meta-progression upgrades with War Trophies.
/// Accessible from the main menu.
/// </summary>
public class MetaProgressionUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI trophyCountText;
    [SerializeField] private Transform upgradeListParent;
    [SerializeField] private GameObject upgradeEntryPrefab;
    [SerializeField] private Button closeButton;

    private void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        RefreshDisplay();
        Debug.Log("[MetaProgressionUI] Shown.");
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        Debug.Log("[MetaProgressionUI] Hidden.");
    }

    public void RefreshDisplay()
    {
        if (trophyCountText != null)
        {
            trophyCountText.text = $"War Trophies: {MetaProgressionManager.WarTrophies}";
        }

        // Clear existing entries
        if (upgradeListParent != null)
        {
            foreach (Transform child in upgradeListParent)
            {
                Destroy(child.gameObject);
            }
        }

        // Create entries for each upgrade
        if (upgradeListParent != null && upgradeEntryPrefab != null)
        {
            foreach (var upgradeDef in MetaProgressionManager.AllUpgrades)
            {
                var go = Instantiate(upgradeEntryPrefab, upgradeListParent);
                var nameText = go.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
                var descText = go.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
                var costText = go.transform.Find("Cost")?.GetComponent<TextMeshProUGUI>();
                var buyButton = go.transform.Find("BuyButton")?.GetComponent<Button>();

                int currentLevel = MetaProgressionManager.GetUpgradeLevel(upgradeDef.id);
                bool maxed = currentLevel >= upgradeDef.maxLevel;

                if (nameText != null)
                {
                    nameText.text = maxed
                        ? $"{upgradeDef.name} (MAX)"
                        : $"{upgradeDef.name} ({currentLevel}/{upgradeDef.maxLevel})";
                }
                if (descText != null) descText.text = upgradeDef.description;
                if (costText != null)
                {
                    costText.text = maxed ? "Maxed" : $"{MetaProgressionManager.GetUpgradeCost(upgradeDef.id)} Trophies";
                }
                if (buyButton != null)
                {
                    string upgradeId = upgradeDef.id;
                    buyButton.interactable = MetaProgressionManager.CanPurchaseUpgrade(upgradeId);
                    buyButton.onClick.AddListener(() =>
                    {
                        if (MetaProgressionManager.PurchaseUpgrade(upgradeId))
                        {
                            RefreshDisplay();
                            Debug.Log($"[MetaProgressionUI] Purchased upgrade: {upgradeId}");
                        }
                    });
                }
            }
        }
    }
}
