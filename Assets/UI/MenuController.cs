using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject setupPanel;
    [SerializeField] GameObject loadPanel;

    [Header("Scenes")]
    [SerializeField] string settingsSceneName = "SettingsMenu";
    [SerializeField] string boardSceneName = "Board";

    void Awake()
    {
        ClientSettings.EnsureExists();
    }

    public void NewGame()
    {
        ShowOnly(setupPanel);
    }

    public void OpenLoad()
    {
        ShowOnly(loadPanel);
    }

    public void BackToMain()
    {
        ShowOnly(mainMenuPanel);
    }

    public void OpenSettings()
    {
        SceneManager.LoadScene(settingsSceneName);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(boardSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }

    void ShowOnly(GameObject panel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(panel == mainMenuPanel);
        if (setupPanel != null) setupPanel.SetActive(panel == setupPanel);
        if (loadPanel != null) loadPanel.SetActive(panel == loadPanel);
    }
}
