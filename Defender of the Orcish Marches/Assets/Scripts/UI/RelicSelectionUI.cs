using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel shown during night phase for relic/boon selection.
/// Displays 3 relic choices and a skip button.
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

        // Show choices
        if (choices.Length > 0)
        {
            if (choice1Button != null) choice1Button.gameObject.SetActive(true);
            if (choice1Name != null) choice1Name.text = choices[0].name;
            if (choice1Desc != null) choice1Desc.text = choices[0].description;
        }
        else if (choice1Button != null) choice1Button.gameObject.SetActive(false);

        if (choices.Length > 1)
        {
            if (choice2Button != null) choice2Button.gameObject.SetActive(true);
            if (choice2Name != null) choice2Name.text = choices[1].name;
            if (choice2Desc != null) choice2Desc.text = choices[1].description;
        }
        else if (choice2Button != null) choice2Button.gameObject.SetActive(false);

        if (choices.Length > 2)
        {
            if (choice3Button != null) choice3Button.gameObject.SetActive(true);
            if (choice3Name != null) choice3Name.text = choices[2].name;
            if (choice3Desc != null) choice3Desc.text = choices[2].description;
        }
        else if (choice3Button != null) choice3Button.gameObject.SetActive(false);

        // Show collected count
        if (collectedText != null && RelicManager.Instance != null)
        {
            int count = RelicManager.Instance.CollectedCount;
            collectedText.text = count > 0
                ? $"Relics: {RelicManager.Instance.GetCollectedNamesDisplay()}"
                : "";
        }

        Debug.Log("[RelicSelectionUI] Relic choices displayed.");
    }

    private void OnChoiceClicked(int index)
    {
        if (RelicManager.Instance != null)
        {
            RelicManager.Instance.SelectRelic(index);
        }
        if (panelRoot != null) panelRoot.SetActive(false);
        Debug.Log($"[RelicSelectionUI] Player chose relic {index}.");
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
