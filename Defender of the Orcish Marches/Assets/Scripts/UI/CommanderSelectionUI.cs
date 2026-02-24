using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI panel on the main menu for selecting a Commander class before starting a run.
/// </summary>
public class CommanderSelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI selectedLabel;

    [Header("Commander Buttons")]
    [SerializeField] private Button noneButton;
    [SerializeField] private Button wardenButton;
    [SerializeField] private Button captainButton;
    [SerializeField] private Button artificerButton;
    [SerializeField] private Button merchantButton;

    [Header("Info Display")]
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Navigation")]
    [SerializeField] private Button closeButton;

    private void Start()
    {
        CommanderManager.LoadSelection();

        if (noneButton != null) noneButton.onClick.AddListener(() => SelectCommander(CommanderDefs.NONE_ID));
        if (wardenButton != null) wardenButton.onClick.AddListener(() => SelectCommander("warden"));
        if (captainButton != null) captainButton.onClick.AddListener(() => SelectCommander("captain"));
        if (artificerButton != null) artificerButton.onClick.AddListener(() => SelectCommander("artificer"));
        if (merchantButton != null) merchantButton.onClick.AddListener(() => SelectCommander("merchant"));
        if (closeButton != null) closeButton.onClick.AddListener(Hide);

        UpdateDisplay();
    }

    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        UpdateDisplay();
        Debug.Log("[CommanderSelectionUI] Shown.");
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        Debug.Log("[CommanderSelectionUI] Hidden.");
    }

    private void SelectCommander(string commanderId)
    {
        CommanderManager.SelectCommander(commanderId);
        UpdateDisplay();
        Debug.Log($"[CommanderSelectionUI] Commander selected: {commanderId}");
    }

    private void UpdateDisplay()
    {
        if (selectedLabel != null)
        {
            selectedLabel.text = $"Commander: {CommanderManager.GetActiveDisplayName()}";
        }

        if (descriptionText != null)
        {
            var def = CommanderManager.ActiveCommander;
            if (def != null)
            {
                descriptionText.text = def.Value.description;
            }
            else
            {
                descriptionText.text = "No commander selected. Play with default settings.";
            }
        }
    }
}
