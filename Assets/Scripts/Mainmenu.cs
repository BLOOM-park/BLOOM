using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuCommands : MonoBehaviour
{
    public void StartGame()
    {
        // Make sure "GameScene" matches your actual gameplay scene name
        SceneManager.LoadScene("Game");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }
}