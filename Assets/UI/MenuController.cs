using UnityEngine;

public class MenuController : MonoBehaviour
{
    public void PlayGame()
    {
        // Load the main game scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("Board");
    }

    public void QuitGame()
    {
        // Log a message to the console (useful for testing in the editor)
        Debug.Log("Quit Game");

        // Quit the application
        Application.Quit();
    }
}
