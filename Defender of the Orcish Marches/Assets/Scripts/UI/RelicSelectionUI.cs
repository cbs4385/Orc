using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel shown during night phase for relic/boon selection.
/// Displays 3 relic choices with synergy hints and a skip button.
/// Shows a synergy notification when a combo is completed.
/// </summary>
public class RelicSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Choice Buttons")]
    [SerializeField] private Button choice1Button;
    [SerializeField] private TextMeshProUGUI choice1Name;
    [SerializeField] private TextMeshProUGUI choice1Desc;

    [SerializeField] private Button choice2Button;
    [SerializeField] private TextMeshProUGUI choice2Name;
    [SerializeField] private TextMeshProUGUI choice2Desc;

    [SerializeField] private Button choice3Button;
    [SerializeField] private TextMeshProUGUI choice3Name;
    [SerializeField] private TextMeshProUGUI choice3Desc;

    [SerializeField] private Button skipButton;

    [Header("Collected Display")]
    [SerializeField] private TextMeshProUGUI collectedText;

    private bool relicManagerSubscribed;

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (choice1Button != null) choice1Button.onClick.AddListener(() => OnChoiceClicked(0));
        if (choice2Button != null) choice2Button.onClick.AddListener(() => OnChoiceClicked(1));
        if (choice3Button != null) choice3Button.onClick.AddListener(() => OnChoiceClicked(2));
        if (skipButton != null) skipButton.onClick.AddListener(OnSkipClicked);
    }

    private void OnEnable()
    {
        relicManagerSubscribed = false;
        TrySubscribe();
    }

    private void Update()
    {
        if (!relicManagerSubscribed) TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (relicManagerSubscribed) return;
        if (RelicManager.Instance != null)
        {
            RelicManager.Instance.OnRelicsOffered += ShowRelicChoices;
            relicManagerSubscribed = true;
        }
    }

    private void OnDisable()
    {
        if (RelicManager.Instance != null)
            RelicManager.Instance.OnRelicsOffered -= ShowRelicChoices;
        relicManagerSubscribed = false;
    }

    private void ShowRelicChoices(RelicDef[] choices)
    {
        if (panelRoot == null) return;

        panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = "CHOOSE A RELIC";

        // Show choices with synergy hints
        SetupChoice(choices, 0, choice1Button, choice1Name, choice1Desc);
        SetupChoice(choices, 1, choice2Button, choice2Name, choice2Desc);
        SetupChoice(choices, 2, choice3Button, choice3Name, choice3Desc);

        // Re-show skip button (may have been hidden by synergy notification)
        if (skipButton != null) skipButton.gameObject.SetActive(true);

        // Show collected count and active synergies
        if (collectedText != null && RelicManager.Instance != null)
        {
            int count = RelicManager.Instance.CollectedCount;
            string synText = RelicManager.Instance.ActiveSynergyCount > 0
                ? $" | Synergies: {RelicManager.Instance.GetActiveSynergiesDisplay()}"
                : "";
            collectedText.text = count > 0
                ? $"Relics: {RelicManager.Instance.GetCollectedNamesDisplay()}{synText}"
                : "";
        }

        Debug.Log("[RelicSelectionUI] Relic choices displayed.");
    }

    private void SetupChoice(RelicDef[] choices, int index, Button button, TextMeshProUGUI nameText, TextMeshProUGUI descText)
    {
        if (index < choices.Length)
        {
            if (button != null) button.gameObject.SetActive(true);
            if (nameText != null) nameText.text = choices[index].name;
            if (descText != null)
            {
                string desc = choices[index].description;

                // Check for synergy hints — show when picking this relic would complete a synergy
                if (RelicManager.Instance != null)
                {
                    var pending = RelicManager.Instance.GetPendingSynergiesFor(choices[index].id);
                    if (pending.Count > 0)
                    {
                        desc += $"\n<color=#FFD700>>>> Synergy: {pending[0].name}</color>";
                        if (pending.Count > 1)
                            desc += $" <color=#FFD700>(+{pending.Count - 1} more)</color>";
                    }
                }

                descText.text = desc;
            }
        }
        else if (button != null)
        {
            button.gameObject.SetActive(false);
        }
    }

    private void OnChoiceClicked(int index)
    {
        List<RelicSynergyDef> activatedSynergies = new List<RelicSynergyDef>();

        void SynergyHandler(RelicSynergyDef syn)
        {
            activatedSynergies.Add(syn);
        }

        if (RelicManager.Instance != null)
        {
            RelicManager.Instance.OnSynergyActivated += SynergyHandler;
            RelicManager.Instance.SelectRelic(index);
            RelicManager.Instance.OnSynergyActivated -= SynergyHandler;
        }

        if (activatedSynergies.Count > 0)
        {
            Debug.Log($"[RelicSelectionUI] Player chose relic {index}. {activatedSynergies.Count} synergy(s) activated!");
            ShowSynergyNotification(activatedSynergies);
        }
        else
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            Debug.Log($"[RelicSelectionUI] Player chose relic {index}.");
        }
    }

    private void ShowSynergyNotification(List<RelicSynergyDef> synergies)
    {
        // Hide choice buttons and skip
        if (choice1Button != null) choice1Button.gameObject.SetActive(false);
        if (choice2Button != null) choice2Button.gameObject.SetActive(false);
        if (choice3Button != null) choice3Button.gameObject.SetActive(false);
        if (skipButton != null) skipButton.gameObject.SetActive(false);

        // Build title with synergy names
        var titleSb = new System.Text.StringBuilder();
        titleSb.Append("<color=#FFD700>SYNERGY UNLOCKED!</color>");
        foreach (var syn in synergies)
        {
            titleSb.AppendFormat("\n<size=80%>{0}</size>", syn.name);
        }
        if (titleText != null)
            titleText.text = titleSb.ToString();

        // Build description with synergy effects
        var descSb = new System.Text.StringBuilder();
        foreach (var syn in synergies)
        {
            if (descSb.Length > 0) descSb.Append("\n");
            descSb.AppendFormat("<color=#CCCCCC>{0}</color>", syn.description);
        }
        if (collectedText != null)
            collectedText.text = descSb.ToString();

        Debug.Log($"[RelicSelectionUI] Showing synergy notification for {synergies.Count} synergy(s).");
        StartCoroutine(HidePanelAfterDelay(3f));
    }

    private IEnumerator HidePanelAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnSkipClicked()
    {
        if (RelicManager.Instance != null)
        {
            RelicManager.Instance.SkipOffer();
        }
        if (panelRoot != null) panelRoot.SetActive(false);
        Debug.Log("[RelicSelectionUI] Player skipped relic choice.");
    }
}
