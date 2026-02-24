using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Displays milestone completion notifications on the game over screen.
/// Shows newly completed milestones and their War Trophy rewards.
/// </summary>
public class MilestoneNotificationUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI milestoneText;

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    /// <summary>
    /// Display newly completed milestones. Called by GameOverScreen.
    /// </summary>
    public void ShowMilestones(List<MilestoneDef> milestones, int trophiesEarned)
    {
        if (panelRoot == null || milestoneText == null) return;
        if (milestones == null || (milestones.Count == 0 && trophiesEarned == 0))
        {
            panelRoot.SetActive(false);
            return;
        }

        panelRoot.SetActive(true);
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"<color=#FFD700>+{trophiesEarned} War Trophies earned!</color>");

        if (milestones.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>Milestones Completed:</b>");
            foreach (var m in milestones)
            {
                sb.AppendLine($"  <color=#00FF00>{m.name}</color> — {m.description} (+{m.trophyReward} Trophies)");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"Total War Trophies: {MetaProgressionManager.WarTrophies}");
        sb.AppendLine($"Milestones: {MilestoneManager.CompletedCount}/{MilestoneManager.TotalCount}");

        milestoneText.text = sb.ToString();
        Debug.Log($"[MilestoneNotificationUI] Displayed {milestones.Count} new milestones, {trophiesEarned} trophies earned.");
    }
}
