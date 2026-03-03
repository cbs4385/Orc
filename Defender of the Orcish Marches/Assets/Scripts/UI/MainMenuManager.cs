using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static LocalizationManager;

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
    private TextMeshProUGUI versionLabel;

    private void Awake()
    {
        sceneLoader = GetComponent<SceneLoader>();
        if (sceneLoader == null)
            sceneLoader = gameObject.AddComponent<SceneLoader>();

        CreateVersionLabel();
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

        // Apply current language to all labels (scene was built with English baked in)
        RefreshLabels();
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshLabels;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshLabels;
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

    private void RefreshLabels()
    {
        SetButtonLabel(playButton, L("menu.play"));
        SetButtonLabel(continueButton, L("menu.continue"));
        SetButtonLabel(tutorialButton, L("menu.tutorial"));
        SetButtonLabel(optionsButton, L("menu.options"));
        SetButtonLabel(statsButton, L("menu.statistics"));
        SetButtonLabel(mutatorsButton, L("menu.mutators"));
        SetButtonLabel(achievementsButton, L("menu.achievements"));
        SetButtonLabel(legacyButton, L("menu.legacy"));
        SetButtonLabel(commanderButton, L("menu.commander"));
        SetButtonLabel(metaProgressionButton, L("menu.upgrades"));
        SetButtonLabel(bestiaryButton, L("menu.bestiary"));
        SetButtonLabel(exitButton, L("menu.exit"));
        SetButtonLabel(bugReportButton, L("menu.report_bug"));
        UpdateDifficultyLabel(difficultySlider != null ? difficultySlider.value : 1);

        // Refresh title text
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            var titleTransform = canvas.transform.Find("TitleText");
            if (titleTransform != null)
            {
                var tmp = titleTransform.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = L("menu.title");
            }

            // Refresh "DIFFICULTY" header
            var diffPanel = canvas.transform.Find("DifficultyPanel");
            if (diffPanel != null)
            {
                var headerTransform = diffPanel.Find("DifficultyHeader");
                if (headerTransform != null)
                {
                    var tmp = headerTransform.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = L("menu.difficulty");
                }

                // Refresh difficulty tick labels (Easy/Normal/Hard/Nightmare)
                // Labels are created in order in the hierarchy
                string[] diffKeys = { "difficulty.easy", "difficulty.normal", "difficulty.hard", "difficulty.nightmare" };
                int labelIdx = 0;
                foreach (Transform child in diffPanel)
                {
                    if (child.name.StartsWith("DiffLabel_") && labelIdx < diffKeys.Length)
                    {
                        var labelTmp = child.GetComponent<TextMeshProUGUI>();
                        if (labelTmp != null)
                            labelTmp.text = L(diffKeys[labelIdx]);
                        labelIdx++;
                    }
                }
            }
        }

        // Version label
        if (versionLabel != null)
            versionLabel.text = L("menu.version", Application.version);

        Debug.Log("[MainMenuManager] Labels refreshed for language change.");
    }

    private void CreateVersionLabel()
    {
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        var versionObj = new GameObject("VersionLabel");
        versionObj.transform.SetParent(canvas.transform, false);
        var rect = versionObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-10f, 10f);
        rect.sizeDelta = new Vector2(200f, 25f);

        versionLabel = versionObj.AddComponent<TextMeshProUGUI>();
        versionLabel.text = L("menu.version", Application.version);
        versionLabel.fontSize = 16;
        versionLabel.color = new Color(0.5f, 0.48f, 0.4f, 0.7f);
        versionLabel.alignment = TextAlignmentOptions.BottomRight;
        versionLabel.raycastTarget = false;
        Debug.Log($"[MainMenuManager] Version label created: {Application.version}");
    }

    private void SetButtonLabel(Button button, string text)
    {
        if (button == null) return;
        var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
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
