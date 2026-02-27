using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button statsButton;
    [SerializeField] private Button mutatorsButton;
    [SerializeField] private Button achievementsButton;
    [SerializeField] private Button legacyButton;
    [SerializeField] private Button bugReportButton;
    [SerializeField] private Button commanderButton;
    [SerializeField] private Button metaProgressionButton;
    [SerializeField] private Button bestiaryButton;
    [SerializeField] private Button continueButton;

    [Header("Bug Report")]
    [SerializeField] private BugReportPanel bugReportPanel;

    [Header("Stats")]
    [SerializeField] private StatsDashboardPanel statsDashboardPanel;

    [Header("Mutators")]
    [SerializeField] private MutatorUI mutatorUI;

    [Header("Commander")]
    [SerializeField] private CommanderSelectionUI commanderSelectionUI;

    [Header("Meta-Progression")]
    [SerializeField] private MetaProgressionUI metaProgressionUI;

    [Header("Bestiary")]
    [SerializeField] private BestiaryUI bestiaryUI;

    [Header("Achievements")]
    [SerializeField] private AchievementUI achievementUI;

    [Header("Legacy")]
    [SerializeField] private LegacyUI legacyUI;

    [Header("Difficulty")]
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private TextMeshProUGUI difficultyLabel;

    private SceneLoader sceneLoader;
    private SaveSlotPicker saveSlotPicker;

    private void Awake()
    {
        sceneLoader = GetComponent<SceneLoader>();
        if (sceneLoader == null)
            sceneLoader = gameObject.AddComponent<SceneLoader>();

        Debug.Log("[MainMenuManager] Initialized.");
    }

    private void Start()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClicked);
        if (tutorialButton != null)
            tutorialButton.onClick.AddListener(OnTutorialClicked);
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
        if (statsButton != null)
            statsButton.onClick.AddListener(OnStatsClicked);
        if (mutatorsButton != null)
            mutatorsButton.onClick.AddListener(OnMutatorsClicked);
        if (achievementsButton != null)
            achievementsButton.onClick.AddListener(OnAchievementsClicked);
        if (legacyButton != null)
            legacyButton.onClick.AddListener(OnLegacyClicked);
        if (bugReportButton != null)
            bugReportButton.onClick.AddListener(OnBugReportClicked);
        if (commanderButton != null)
            commanderButton.onClick.AddListener(OnCommanderClicked);
        if (metaProgressionButton != null)
            metaProgressionButton.onClick.AddListener(OnMetaProgressionClicked);
        if (bestiaryButton != null)
            bestiaryButton.onClick.AddListener(OnBestiaryClicked);
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
            // Disable if no saves exist
            continueButton.gameObject.SetActive(SaveManager.HasAnySave());
        }

        if (difficultySlider != null)
        {
            // Nightmare requires mouse delta for FPS freelook — not available on iOS/Android
            if (PlatformDetector.IsMobile)
            {
                difficultySlider.maxValue = 2; // Cap at Hard (index 2)
                Debug.Log("[MainMenuManager] Mobile detected — Nightmare difficulty disabled, slider capped at Hard.");
            }
            difficultySlider.value = (int)GameSettings.CurrentDifficulty;
            difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
            UpdateDifficultyLabel(difficultySlider.value);
        }
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayClicked);
        if (optionsButton != null)
            optionsButton.onClick.RemoveListener(OnOptionsClicked);
        if (tutorialButton != null)
            tutorialButton.onClick.RemoveListener(OnTutorialClicked);
        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExitClicked);
        if (statsButton != null)
            statsButton.onClick.RemoveListener(OnStatsClicked);
        if (mutatorsButton != null)
            mutatorsButton.onClick.RemoveListener(OnMutatorsClicked);
        if (achievementsButton != null)
            achievementsButton.onClick.RemoveListener(OnAchievementsClicked);
        if (legacyButton != null)
            legacyButton.onClick.RemoveListener(OnLegacyClicked);
        if (bugReportButton != null)
            bugReportButton.onClick.RemoveListener(OnBugReportClicked);
        if (commanderButton != null)
            commanderButton.onClick.RemoveListener(OnCommanderClicked);
        if (metaProgressionButton != null)
            metaProgressionButton.onClick.RemoveListener(OnMetaProgressionClicked);
        if (bestiaryButton != null)
            bestiaryButton.onClick.RemoveListener(OnBestiaryClicked);
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
        if (difficultySlider != null)
            difficultySlider.onValueChanged.RemoveListener(OnDifficultyChanged);
    }

    private void OnDifficultyChanged(float value)
    {
        GameSettings.CurrentDifficulty = (Difficulty)Mathf.RoundToInt(value);
        UpdateDifficultyLabel(value);
    }

    private void UpdateDifficultyLabel(float value)
    {
        if (difficultyLabel == null) return;
        difficultyLabel.text = GameSettings.GetDifficultyName();
        Debug.Log($"[MainMenuManager] Difficulty slider changed to {GameSettings.GetDifficultyName()}");
    }

    private void OnPlayClicked()
    {
        Debug.Log("[MainMenuManager] Play clicked.");
        sceneLoader.LoadGameScene();
    }

    private void OnOptionsClicked()
    {
        Debug.Log("[MainMenuManager] Options clicked.");
        sceneLoader.LoadOptionsScene();
    }

    private void OnTutorialClicked()
    {
        Debug.Log("[MainMenuManager] Tutorial clicked.");
        sceneLoader.LoadTutorialScene();
    }

    private void OnExitClicked()
    {
        Debug.Log("[MainMenuManager] Exit clicked.");
        sceneLoader.QuitGame();
    }

    private void OnStatsClicked()
    {
        Debug.Log("[MainMenuManager] Stats clicked.");
        if (statsDashboardPanel != null)
            statsDashboardPanel.Show();
        else
            Debug.LogWarning("[MainMenuManager] StatsDashboardPanel reference is null.");
    }

    private void OnMutatorsClicked()
    {
        Debug.Log("[MainMenuManager] Mutators clicked.");
        if (mutatorUI != null)
            mutatorUI.Show();
        else
            Debug.LogWarning("[MainMenuManager] MutatorUI reference is null.");
    }

    private void OnAchievementsClicked()
    {
        Debug.Log("[MainMenuManager] Achievements clicked.");
        if (achievementUI != null)
            achievementUI.Show();
        else
            Debug.LogWarning("[MainMenuManager] AchievementUI reference is null.");
    }

    private void OnLegacyClicked()
    {
        Debug.Log("[MainMenuManager] Legacy clicked.");
        if (legacyUI != null)
            legacyUI.Show();
        else
            Debug.LogWarning("[MainMenuManager] LegacyUI reference is null.");
    }

    private void OnBugReportClicked()
    {
        Debug.Log("[MainMenuManager] Bug Report clicked.");
        if (bugReportPanel != null)
            bugReportPanel.Show();
        else
            Debug.LogWarning("[MainMenuManager] BugReportPanel reference is null.");
    }

    private void OnCommanderClicked()
    {
        Debug.Log("[MainMenuManager] Commander clicked.");
        if (commanderSelectionUI != null)
            commanderSelectionUI.Show();
        else
            Debug.LogWarning("[MainMenuManager] CommanderSelectionUI reference is null.");
    }

    private void OnMetaProgressionClicked()
    {
        Debug.Log("[MainMenuManager] Meta-Progression clicked.");
        if (metaProgressionUI != null)
            metaProgressionUI.Show();
        else
            Debug.LogWarning("[MainMenuManager] MetaProgressionUI reference is null.");
    }

    private void OnBestiaryClicked()
    {
        Debug.Log("[MainMenuManager] Bestiary clicked.");
        if (bestiaryUI != null)
            bestiaryUI.Show();
        else
            Debug.LogWarning("[MainMenuManager] BestiaryUI reference is null.");
    }

    private void OnContinueClicked()
    {
        Debug.Log("[MainMenuManager] Continue clicked.");

        if (saveSlotPicker == null)
        {
            var pickerObj = new GameObject("SaveSlotPicker");
            saveSlotPicker = pickerObj.AddComponent<SaveSlotPicker>();
        }

        saveSlotPicker.Show(SaveSlotPicker.Mode.Load, (slot) =>
        {
            var data = SaveManager.LoadSlot(slot);
            if (data == null)
            {
                Debug.LogError($"[MainMenuManager] Failed to load slot {slot}.");
                return;
            }

            // Set pending data and load the game scene
            SaveManager.PendingSaveData = data;
            SaveManager.LastUsedSlot = slot;

            // Set difficulty and mutators to match save
            GameSettings.CurrentDifficulty = (Difficulty)data.difficulty;

            // Restore mutator selections
            MutatorManager.ClearActive();
            if (data.activeMutatorIds != null)
            {
                foreach (var id in data.activeMutatorIds)
                    MutatorManager.SetActive(id, true);
            }

            // Restore commander
            if (!string.IsNullOrEmpty(data.commanderId))
                CommanderManager.SelectCommander(data.commanderId);

            Debug.Log($"[MainMenuManager] Loading saved game: Day {data.dayNumber}, Difficulty={(Difficulty)data.difficulty}");
            sceneLoader.LoadGameScene();
        });
    }
}
