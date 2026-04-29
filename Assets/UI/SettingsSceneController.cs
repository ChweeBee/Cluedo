using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsSceneController : MonoBehaviour
{
    [SerializeField] string mainMenuSceneName = "MainMenu";

    public void Back()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
