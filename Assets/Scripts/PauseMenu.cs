using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;

public class PauseMenu : MonoBehaviour
{
    public static bool isPaused = false;

    public GameObject pauseMenuUI;

    private StarterAssetsInputs playerInputs;
    private UnityEngine.InputSystem.PlayerInput playerInputComponent;

    private void FindPlayerInputs()
    {
        if (playerInputs == null || playerInputComponent == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerInputs = player.GetComponent<StarterAssetsInputs>();
                playerInputComponent = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();

                // CRITICAL: PlayerInput must NOT pair with uiInputModule because StarterAssets has no UI map!
                if (playerInputComponent != null && playerInputComponent.uiInputModule != null)
                {
                    playerInputComponent.uiInputModule = null;
                }
            }
        }
    }

    void Start()
    {
        FindPlayerInputs();
        HookButtonListeners();

        if (playerInputs != null)
        {
            playerInputs.cursorLocked = true;
            playerInputs.cursorInputForLook = true;
        }

        // Ensure cursor is locked when game starts
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HookButtonListeners()
    {
        if (pauseMenuUI != null)
        {
            var resumeBtn = pauseMenuUI.transform.Find("Panel/buttonsalignment/Resume BTN")?.GetComponent<UnityEngine.UI.Button>();
            if (resumeBtn != null)
            {
                resumeBtn.onClick.RemoveListener(Resume);
                resumeBtn.onClick.AddListener(Resume);
            }

            var menuBtn = pauseMenuUI.transform.Find("Panel/buttonsalignment/MainMenu BTN")?.GetComponent<UnityEngine.UI.Button>();
            if (menuBtn != null)
            {
                menuBtn.onClick.RemoveListener(LoadMenu);
                menuBtn.onClick.AddListener(LoadMenu);
            }

            var quitBtn = pauseMenuUI.transform.Find("Panel/buttonsalignment/QuitGame BTN")?.GetComponent<UnityEngine.UI.Button>();
            if (quitBtn != null)
            {
                quitBtn.onClick.RemoveListener(QuitGame);
                quitBtn.onClick.AddListener(QuitGame);
            }
        }
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
        else if (isPaused)
        {
            // Keep cursor unlocked and visible while paused
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }
        }
    }

    public void Resume()
    {
        Debug.Log("Resuming Game...");
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        Time.timeScale = 1f;
        isPaused = false;
        
        // Resume all audio
        AudioListener.pause = false;

        // Re-enable PlayerInput for gameplay
        FindPlayerInputs();
        if (playerInputComponent != null)
        {
            playerInputComponent.enabled = true;
        }

        // Lock cursor when resuming
        if (playerInputs != null)
        {
            playerInputs.cursorLocked = true;
            playerInputs.cursorInputForLook = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Pause()
    {
        // Pause all audio
        AudioListener.pause = true;
        
        // Find player and disable player character input while in pause menu
        FindPlayerInputs();
        if (playerInputComponent != null)
        {
            playerInputComponent.enabled = false;
        }

        if (playerInputs != null)
        {
            playerInputs.cursorLocked = false;
            playerInputs.cursorInputForLook = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ensure the UI Action Map and click actions are enabled for UI clicks
        var uiModule = FindFirstObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (uiModule != null)
        {
            if (uiModule.actionsAsset != null)
            {
                var uiMap = uiModule.actionsAsset.FindActionMap("UI");
                if (uiMap != null && !uiMap.enabled)
                {
                    uiMap.Enable();
                }
            }
            if (uiModule.point != null && uiModule.point.action != null && !uiModule.point.action.enabled)
            {
                uiModule.point.action.Enable();
            }
            if (uiModule.leftClick != null && uiModule.leftClick.action != null && !uiModule.leftClick.action.enabled)
            {
                uiModule.leftClick.action.Enable();
            }
        }

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
            HookButtonListeners();
        }

        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            var resumeBtn = pauseMenuUI != null ? pauseMenuUI.transform.Find("Panel/buttonsalignment/Resume BTN") : null;
            if (resumeBtn != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(resumeBtn.gameObject);
            }
            else
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }
        }

        Time.timeScale = 0f;
        isPaused = true;
        Debug.Log("Game Paused.");
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
