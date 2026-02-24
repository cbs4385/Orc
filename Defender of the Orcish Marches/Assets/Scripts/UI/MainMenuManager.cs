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
    [SerializeField] private Button bugReportButton;

    [Header("Bug Report")]
    [SerializeField] private BugReportPanel bugReportPanel;

    [Header("Stats")]
    [SerializeField] private StatsDashboardPanel statsDashboardPanel;

    [Header("Mutators")]
    [SerializeField] private MutatorUI mutatorUI;

    [Header("Difficulty")]
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private TextMeshProUGUI difficultyLabel;

    private SceneLoader sceneLoader;

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
        if (bugReportButton != null)
            bugReportButton.onClick.AddListener(OnBugReportClicked);

        if (difficultySlider != null)
        {
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
        if (bugReportButton != null)
            bugReportButton.onClick.RemoveListener(OnBugReportClicked);
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

    private void OnBugReportClicked()
    {
        Debug.Log("[MainMenuManager] Bug Report clicked.");
        if (bugReportPanel != null)
            bugReportPanel.Show();
        else
            Debug.LogWarning("[MainMenuManager] BugReportPanel reference is null.");
    }
}
