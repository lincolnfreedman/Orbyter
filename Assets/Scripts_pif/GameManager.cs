using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private bool adventureLogOpen = false;
    private bool menuOpen = false;

    [SerializeField]
    private GameObject adventureLog;
    [SerializeField]
    private GameObject pauseMenu;
    public bool[] pagesUnlocked;

    [SerializeField]
    private PlayerInput playerInput;
    private InputAction adventureLogAction;
    private InputAction menuAction;

    // Public properties to check current state
    public bool IsAdventureLogOpen => adventureLogOpen;
    public bool IsMenuOpen => menuOpen;
    public bool IsAnyUIOpen => adventureLogOpen || menuOpen;

    [Header("Heart Container Tracking")]
    [Tooltip("Whether the heart container in the first scene has been collected")]
    public bool firstSceneHeartContainerCollected = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
        }

        DontDestroyOnLoad(this);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        adventureLogAction = playerInput.actions["AdventureLog"];
        menuAction = playerInput.actions["Menu"];
    }

    // Update is called once per frame
    void Update()
    {
        // Check for missing components and try to find them
        FindMissingComponents();
        
        // Handle Adventure Log action
        if (adventureLogAction != null && adventureLogAction.triggered)
        {
            // Can't open adventure log if menu is already open
            if (!menuOpen)
            {
                if (adventureLogOpen)
                {
                    CloseAdventureLog();
                }
                else
                {
                    OpenAdventureLog();
                }
            }
        }

        // Handle Menu action
        if (menuAction != null && menuAction.triggered)
        {
            if (menuOpen)
            {
                CloseMenu();
            }
            else
            {
                // Close adventure log if it's open before opening menu
                if (adventureLogOpen)
                {
                    CloseAdventureLog();
                }
                OpenMenu();
            }
        }
    }

    private void FindMissingComponents()
    {
        // Find PlayerInput if missing
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
            if (playerInput != null)
            {
                // Re-initialize input actions
                adventureLogAction = playerInput.actions["AdventureLog"];
                menuAction = playerInput.actions["Menu"];
                Debug.Log("GameManager: Found PlayerInput and re-initialized actions");
            }
        }
        
        // Find Adventure Log if missing (works with inactive objects)
        if (adventureLog == null)
        {
            AdventureLog[] allLogs = Resources.FindObjectsOfTypeAll<AdventureLog>();
            foreach (AdventureLog log in allLogs)
            {
                // Make sure it's in the current scene, not a prefab or DontDestroyOnLoad
                if (log.gameObject.scene.name != null && log.gameObject.hideFlags == HideFlags.None)
                {
                    adventureLog = log.gameObject;
                    Debug.Log($"GameManager: Found Adventure Log: {adventureLog.name} (Active: {adventureLog.activeInHierarchy})");
                    break;
                }
            }
            
            if (adventureLog == null)
            {
                Debug.LogWarning("GameManager: Adventure Log not found in scene");
            }
        }
        
        // Find Pause Menu if missing (works with inactive objects)
        if (pauseMenu == null)
        {
            MainMenu_pip[] allMenus = Resources.FindObjectsOfTypeAll<MainMenu_pip>();
            foreach (MainMenu_pip menu in allMenus)
            {
                // Make sure it's in the current scene, not a prefab or DontDestroyOnLoad
                if (menu.gameObject.scene.name != null && menu.gameObject.hideFlags == HideFlags.None)
                {
                    pauseMenu = menu.gameObject;
                    Debug.Log($"GameManager: Found Menu: {pauseMenu.name} (Active: {pauseMenu.activeInHierarchy})");
                    break;
                }
            }
            
            if (pauseMenu == null)
            {
                Debug.LogWarning("GameManager: Menu not found in scene");
            }
        }
    }

    private void OpenAdventureLog()
    {
        if (adventureLog == null) return;
        
        adventureLogOpen = true;
        adventureLog.SetActive(true);
        Time.timeScale = 0f;
    }

    private void CloseAdventureLog()
    {
        if (adventureLog == null) return;
        
        adventureLogOpen = false;
        
        // Use the AdventureLog's close method to play close SFX
        AdventureLog logScript = adventureLog.GetComponent<AdventureLog>();
        if (logScript != null)
        {
            logScript.CloseAdventureLog();
        }
        else
        {
            // Fallback if no script found
            adventureLog.SetActive(false);
        }
        
        // Only resume time if menu is not open
        if (!menuOpen)
        {
            Time.timeScale = 1f;
        }
    }

    private void OpenMenu()
    {
        menuOpen = true;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }
        Time.timeScale = 0f;
    }

    private void CloseMenu()
    {
        menuOpen = false;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
        
        // Only resume time if adventure log is not open
        if (!adventureLogOpen)
        {
            Time.timeScale = 1f;
        }
    }
}
