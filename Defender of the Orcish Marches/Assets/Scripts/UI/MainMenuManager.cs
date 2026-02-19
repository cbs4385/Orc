using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button exitButton;

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
}
