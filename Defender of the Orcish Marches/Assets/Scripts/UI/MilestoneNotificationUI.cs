using System.Collections.Generic;
using UnityEngine;
using TMPro;
using static LocalizationManager;

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

        sb.AppendLine($"<color=#FFD700>{L("milestone.ui.trophies_earned", trophiesEarned)}</color>");

        if (milestones.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"<b>{L("milestone.ui.completed_header")}</b>");
            foreach (var m in milestones)
            {
                string mName = MilestoneManager.GetLocalizedName(m.id);
                string mDesc = MilestoneManager.GetLocalizedDesc(m.id);
                sb.AppendLine(L("milestone.ui.entry", mName, mDesc, m.trophyReward));
            }
        }

        sb.AppendLine();
        sb.AppendLine(L("milestone.ui.total_trophies", MetaProgressionManager.WarTrophies));
        sb.AppendLine(L("milestone.ui.progress", MilestoneManager.CompletedCount, MilestoneManager.TotalCount));

        milestoneText.text = sb.ToString();
        Debug.Log($"[MilestoneNotificationUI] Displayed {milestones.Count} new milestones, {trophiesEarned} trophies earned.");
    }
}
