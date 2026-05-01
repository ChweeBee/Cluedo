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

    // ensures the persistent client-settings singleton is alive before showing any menu.
    void Awake()
    {
        ClientSettings.EnsureExists();
    }

    // opens the setup panel for starting a new game.
    public void NewGame()
    {
        ShowOnly(setupPanel);
    }

    // opens the load panel for resuming a saved game.
    public void OpenLoad()
    {
        ShowOnly(loadPanel);
    }

    // returns to the main menu panel.
    public void BackToMain()
    {
        ShowOnly(mainMenuPanel);
    }

    // jumps to the dedicated settings scene.
    public void OpenSettings()
    {
        SceneManager.LoadScene(settingsSceneName);
    }

    // loads the board scene to start playing.
    public void PlayGame()
    {
        SceneManager.LoadScene(boardSceneName);
    }

    // quits the running game.
    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }

    // hides every menu panel except the one passed in.
    void ShowOnly(GameObject panel)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(panel == mainMenuPanel);
        if (setupPanel != null) setupPanel.SetActive(panel == setupPanel);
        if (loadPanel != null) loadPanel.SetActive(panel == loadPanel);
    }
}
