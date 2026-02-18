using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        Debug.Log("[SceneLoader] Loading MainMenu.");
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadGameScene()
    {
        Time.timeScale = 1f;
        Debug.Log("[SceneLoader] Loading GameScene.");
        SceneManager.LoadScene("GameScene");
    }

    public void LoadOptionsScene()
    {
        Debug.Log("[SceneLoader] Loading Options.");
        SceneManager.LoadScene("Options");
    }

    public void QuitGame()
    {
        Debug.Log("[SceneLoader] Quitting application.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
