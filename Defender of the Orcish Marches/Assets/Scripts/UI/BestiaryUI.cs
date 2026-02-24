using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel for viewing the bestiary — enemy kill counts, lore, and tactical tips.
/// Accessible from the main menu Stats panel.
/// </summary>
public class BestiaryUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
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
        Debug.Log("[BestiaryUI] Shown.");
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        Debug.Log("[BestiaryUI] Hidden.");
    }

    private void RefreshDisplay()
    {
        if (titleText != null)
        {
            int unlocked = BestiaryManager.UnlockedLoreCount();
            titleText.text = $"BESTIARY ({unlocked}/6 Unlocked)";
        }

        if (contentText != null)
        {
            var sb = new System.Text.StringBuilder();
            var entries = BestiaryManager.GetAllEntries();

            foreach (var entry in entries)
            {
                sb.AppendLine($"<b>{entry.enemyName}</b> ({entry.enemyType})");
                sb.AppendLine($"  Kills: {entry.killCount}");

                if (entry.loreUnlocked)
                {
                    sb.AppendLine($"  <color=#FFD700>TIP:</color> {entry.loreTip}");
                }
                else
                {
                    float progress = BestiaryManager.GetUnlockProgress(entry.enemyName);
                    sb.AppendLine($"  Lore: {entry.loreTip} ({progress:P0})");
                }
                sb.AppendLine();
            }

            contentText.text = sb.ToString();
        }
    }
}
