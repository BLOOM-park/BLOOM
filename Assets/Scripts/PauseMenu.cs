using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool isPaused = false;

    public GameObject pauseMenuUI;

    void Start()
    {
        // Ensure cursor is locked when game starts
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If the Fullscreen Big Map is open, let MinimapToggler close it first
            if (MinimapToggler.IsAnyBigMapOpen) return;

            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        Time.timeScale = 1f;
        isPaused = false;
        
        // Resume all audio
        AudioListener.pause = false;
        
        // Switch back to Display 1
        if (Display.displays.Length > 0)
        {
            Display.displays[0].Activate();
        }
        
        // Lock cursor when resuming
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Pause()
    {
        // Pause all audio
        AudioListener.pause = true;
        
        // Unlock cursor when paused
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Switch to Display 2
        if (Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
        }

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }

        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        Time.timeScale = 0f;
        isPaused = true;
    }

    public void LoadMenu()
    {
        Debug.Log("Loading Main Menu...");
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("Main Menu"); 
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}
