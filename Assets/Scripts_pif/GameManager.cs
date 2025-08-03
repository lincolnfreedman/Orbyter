using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    // Field to store pending save data between scene loads
    private SaveData pendingSaveData;

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
    public bool IsCutscenePlaying = false;

    [Header("Heart Container Tracking")]
    [Tooltip("Whether the heart container in the first scene has been collected")]
    public bool firstSceneHeartContainerCollected = false;

    // Add a public bool to track if the forest has been cleansed
    public bool forestCleansed = false;

    // Save game data container
    [Serializable]
    private class SaveData
    {
        public bool[] pagesUnlocked;
        public bool firstSceneHeartContainerCollected;
        public bool forestCleansed;
        // Replace dictionary with two parallel lists for proper serialization
        public List<string> abilityNames = new List<string>();
        public List<bool> abilityStates = new List<bool>();
        public int heartContainers;
        public int maxHealth;
        public int currentHealth;
        public Vector3 playerPosition;
        public Vector3 checkpointPosition;
        public bool hasCheckpoint;
        public List<DialogueTriggerState> dialogueTriggerStates = new List<DialogueTriggerState>();
        public string currentSceneName;
        public List<AbilityUnlockState> abilityUnlockStates = new List<AbilityUnlockState>();
        public List<HeartContainerState> heartContainerStates = new List<HeartContainerState>();
    }

    // Serializable class to store dialogue trigger state
    [Serializable]
    private class DialogueTriggerState
    {
        public string triggerID; // Scene name + object name as unique identifier
        public int dialogueStage;
        public bool backtrackPlayed;
    }

    // Serializable class to store ability unlock state
    [Serializable]
    private class AbilityUnlockState
    {
        public string objectName; // Name of the GameObject with AbilityUnlock component
        public string objectScene; // Scene where the object is located
        public bool isActive; // Whether the GameObject is active
    }

    // Serializable class to store heart container state
    [Serializable]
    private class HeartContainerState
    {
        public string objectName; // Name of the GameObject with HeartContainer component
        public string objectScene; // Scene where the object is located
        public bool isCollected; // Whether the heart container has been collected
    }

    private const string SaveKey = "OrbyteSaveData";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this.gameObject);
        }

        DontDestroyOnLoad(this);
    }

    
    // Update is called once per frame
    void Update()
    {
        // Check for missing components and try to find them
        FindMissingComponents();
        
        {
            // Can't open adventure log if menu is already open
            if (adventureLogAction != null && adventureLogAction.triggered && !IsCutscenePlaying &&!menuOpen)
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
        if (menuAction != null && menuAction.triggered && !IsCutscenePlaying)
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

    public void CloseMenu()
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
    
    /// <summary>
    /// Saves the current game state to PlayerPrefs
    /// </summary>
    public void SaveGame()
    {
        SaveData saveData = new SaveData();
        
        // Save GameManager data
        saveData.pagesUnlocked = pagesUnlocked;
        saveData.firstSceneHeartContainerCollected = firstSceneHeartContainerCollected;
        saveData.forestCleansed = forestCleansed;
        
        // Save current scene name
        saveData.currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        // Get player data if available
        PlayerController_pif player = FindFirstObjectByType<PlayerController_pif>();
        if (player != null)
        {
            saveData.playerPosition = player.transform.position;
            saveData.heartContainers = player.GetHeartContainers();
            saveData.maxHealth = player.GetMaxHealth();
            saveData.currentHealth = player.GetCurrentHealth();
            
            // Access non-public fields using reflection for checkpoint data
            var checkpointField = player.GetType().GetField("checkpointPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hasCheckpointField = player.GetType().GetField("hasCheckpoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (checkpointField != null && hasCheckpointField != null)
            {
                saveData.checkpointPosition = (Vector3)checkpointField.GetValue(player);
                saveData.hasCheckpoint = (bool)hasCheckpointField.GetValue(player);
            }
        }
        
        // Save unlocked abilities using lists instead of dictionary
        SaveAbilitiesToLists(saveData);
        
        // Save ability unlock object states
        SaveAbilityUnlockStates(saveData);
        
        // Save heart container states
        SaveHeartContainerStates(saveData);
        
        // Save dialogue trigger states
        SaveDialogueTriggerStates(saveData);
        
        // Serialize and save data
        string jsonData = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SaveKey, jsonData);
        PlayerPrefs.Save();
        
        Debug.Log("Game saved successfully!");
    }
    
    // New method to save abilities to parallel lists
    private void SaveAbilitiesToLists(SaveData saveData)
    {
        // Clear the lists to start fresh
        saveData.abilityNames.Clear();
        saveData.abilityStates.Clear();
        
        // Get all ability states using reflection
        Dictionary<string, bool> abilities = GetAbilityDictionary();
        
        // Convert dictionary to parallel lists for serialization
        foreach (var pair in abilities)
        {
            saveData.abilityNames.Add(pair.Key);
            saveData.abilityStates.Add(pair.Value);
        }
        
        Debug.Log($"Saved {saveData.abilityNames.Count} ability states to lists");
    }
    
    private void SaveAbilityUnlockStates(SaveData saveData)
    {
        // Find all ability unlock objects in the scene
        AbilityUnlock[] abilityUnlocks = FindObjectsOfType<AbilityUnlock>(true); // true to include inactive objects
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        foreach (AbilityUnlock abilityUnlock in abilityUnlocks)
        {
            // Create state object
            AbilityUnlockState state = new AbilityUnlockState
            {
                objectName = abilityUnlock.gameObject.name,
                objectScene = currentScene,
                isActive = abilityUnlock.gameObject.activeSelf
            };
            
            // Add to save data
            saveData.abilityUnlockStates.Add(state);
            
            Debug.Log($"Saved AbilityUnlock state: {state.objectName}, active: {state.isActive}");
        }
    }
    
    private void SaveHeartContainerStates(SaveData saveData)
    {
        // Find all heart containers in the scene
        HeartContainer[] heartContainers = FindObjectsOfType<HeartContainer>(true); // true to include inactive objects
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        foreach (HeartContainer heartContainer in heartContainers)
        {
            // Create state object
            HeartContainerState state = new HeartContainerState
            {
                objectName = heartContainer.gameObject.name,
                objectScene = currentScene,
                isCollected = !heartContainer.gameObject.activeSelf // If inactive, it's been collected
            };
            
            // Add to save data
            saveData.heartContainerStates.Add(state);
            
            Debug.Log($"Saved HeartContainer state: {state.objectName}, collected: {state.isCollected}");
        }
    }
    
    private void SaveDialogueTriggerStates(SaveData saveData)
    {
        // Find all dialogue triggers in the scene
        DialogueTrigger[] dialogueTriggers = FindObjectsOfType<DialogueTrigger>(true); // true to include inactive objects
        
        foreach (DialogueTrigger trigger in dialogueTriggers)
        {
            // Create a unique identifier using scene name + object name
            string sceneNameSafe = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ?? "Unknown";
            string triggerID = $"{sceneNameSafe}_{trigger.gameObject.name}";
            
            // Create state object
            DialogueTriggerState state = new DialogueTriggerState
            {
                triggerID = triggerID,
                dialogueStage = trigger.GetDialogueStage(),
                backtrackPlayed = trigger.GetBacktrackPlayed()
            };
            
            // Add to save data
            saveData.dialogueTriggerStates.Add(state);
            
            Debug.Log($"Saved dialogue trigger state: {triggerID}, stage: {state.dialogueStage}, backtrack: {state.backtrackPlayed}");
        }
    }
    
    /// <summary>
    /// Loads the saved game state from PlayerPrefs
    /// </summary>
    /// <returns>True if load was successful, false otherwise</returns>
    public bool LoadGame()
    {
        if (!HasSaveGame())
        {
            Debug.LogWarning("No save data found!");
            return false;
        }
        
        try
        {
            string jsonData = PlayerPrefs.GetString(SaveKey);
            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);
            
            // Store the save data temporarily
            pendingSaveData = saveData;
            
            // Close menus before loading the scene
            CloseMenu();
            CloseAdventureLog();

            // Get current scene name
            string currentScene = SceneManager.GetActiveScene().name;
            
            // Only load scene if it's different from the current scene
            if (saveData.currentSceneName != currentScene)
            {
                Debug.Log($"Loading saved scene: {saveData.currentSceneName}");
                
                // Register callback for scene loaded event
                SceneManager.sceneLoaded += OnSceneLoaded;
                
                // Load the saved scene
                SceneManager.LoadScene(saveData.currentSceneName);
                return true; // Return true here, actual data restoration will happen in callback
            }
            else
            {
                // We're already in the right scene, restore the game state immediately
                Debug.Log($"Already in correct scene: {currentScene}, applying save data");
                RestoreGameState(saveData);
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading save data: {e.Message}");
            pendingSaveData = null;
            return false;
        }
    }
    
    /// <summary>
    /// Callback for scene loaded event
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Unregister to prevent multiple calls
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Check if we have pending save data to apply
        if (pendingSaveData != null)
        {
            // Add a delay to ensure all objects are initialized
            StartCoroutine(RestoreGameStateDelayed(pendingSaveData));
        }
    }
    
    private IEnumerator RestoreGameStateDelayed(SaveData saveData)
    {
        // Wait a frame to let everything initialize
        yield return null;
        
        // Now apply the save data
        RestoreGameState(saveData);
        
        // Clear the pending save data
        pendingSaveData = null;
    }
    
    /// <summary>
    /// Restores the game state from save data
    /// </summary>
    private void RestoreGameState(SaveData saveData)
    {
        // Load GameManager data
        pagesUnlocked = saveData.pagesUnlocked;
        firstSceneHeartContainerCollected = saveData.firstSceneHeartContainerCollected;
        forestCleansed = saveData.forestCleansed;
        
        // Apply data to player if available
        PlayerController_pif player = FindFirstObjectByType<PlayerController_pif>();
        if (player != null)
        {
            // Set player position
            player.transform.position = saveData.playerPosition;
            
            // Access non-public fields/methods using reflection to restore checkpoint data
            var checkpointMethod = player.GetType().GetMethod("SetCheckpoint", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (checkpointMethod != null && saveData.hasCheckpoint)
            {
                checkpointMethod.Invoke(player, new object[] { saveData.checkpointPosition });
            }
            
            // Restore health values using reflection
            var maxHealthField = player.GetType().GetField("maxHealth", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var currentHealthField = player.GetType().GetField("currentHealth", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var heartContainersField = player.GetType().GetField("heartContainers", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (maxHealthField != null) maxHealthField.SetValue(player, saveData.maxHealth);
            if (currentHealthField != null) currentHealthField.SetValue(player, saveData.currentHealth);
            if (heartContainersField != null) heartContainersField.SetValue(player, saveData.heartContainers);
        }
        else
        {
            Debug.LogWarning("Player not found after loading scene! Some save data couldn't be applied.");
        }
        
        // Restore unlocked abilities from lists instead of dictionary
        RestoreAbilitiesFromLists(saveData);
        
        // Restore ability unlock object states if we're in the saved scene
        RestoreAbilityUnlockStates(saveData);
        
        // Restore heart container states
        RestoreHeartContainerStates(saveData);
        
        // Restore dialogue trigger states
        RestoreDialogueTriggerStates(saveData);
        
        Debug.Log($"Game loaded successfully! Scene: {saveData.currentSceneName}");
    }

    public bool HasSaveGame()
    {
        return PlayerPrefs.HasKey(SaveKey);
    }
    
    private void RestoreDialogueTriggerStates(SaveData saveData)
    {
        if (saveData.dialogueTriggerStates == null || saveData.dialogueTriggerStates.Count == 0)
        {
            Debug.Log("No dialogue trigger states to restore");
            return;
        }
        
        // Find all dialogue triggers in the scene
        DialogueTrigger[] dialogueTriggers = FindObjectsOfType<DialogueTrigger>(true); // true to include inactive objects
        string sceneNameSafe = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name ?? "Unknown";
        
        foreach (DialogueTrigger trigger in dialogueTriggers)
        {
            // Create the same identifier we used when saving
            string triggerID = $"{sceneNameSafe}_{trigger.gameObject.name}";
            
            // Look for matching saved state
            foreach (var state in saveData.dialogueTriggerStates)
            {
                if (state.triggerID == triggerID)
                {
                    // Restore state
                    trigger.SetDialogueStage(state.dialogueStage);
                    trigger.SetBacktrackPlayed(state.backtrackPlayed);
                    
                    Debug.Log($"Restored dialogue trigger state: {triggerID}, stage: {state.dialogueStage}, backtrack: {state.backtrackPlayed}");
                    break;
                }
            }
        }
    }

    private void RestoreAbilityUnlockStates(SaveData saveData)
    {
        if (saveData.abilityUnlockStates == null || saveData.abilityUnlockStates.Count == 0)
        {
            Debug.Log("No ability unlock states to restore");
            return;
        }
        
        // Find all ability unlock objects in the scene
        AbilityUnlock[] abilityUnlocks = FindObjectsOfType<AbilityUnlock>(true); // true to include inactive objects
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        foreach (AbilityUnlock abilityUnlock in abilityUnlocks)
        {
            // Look for matching saved state from the current scene
            foreach (var state in saveData.abilityUnlockStates)
            {
                if (state.objectName == abilityUnlock.gameObject.name && state.objectScene == currentScene)
                {
                    // Restore active state
                    abilityUnlock.gameObject.SetActive(state.isActive);
                    
                    Debug.Log($"Restored AbilityUnlock state: {state.objectName}, active: {state.isActive}");
                    break;
                }
            }
        }
    }

    private void RestoreHeartContainerStates(SaveData saveData)
    {
        // Find all heart containers in the scene
        HeartContainer[] heartContainers = FindObjectsOfType<HeartContainer>(true); // true to include inactive objects
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        foreach (HeartContainer heartContainer in heartContainers)
        {
            // Look for matching saved state from the current scene
            bool stateFound = false;
            
            // Skip checking if no save data available
            if (saveData.heartContainerStates != null && saveData.heartContainerStates.Count > 0)
            {
                foreach (var state in saveData.heartContainerStates)
                {
                    if (state.objectName == heartContainer.gameObject.name && state.objectScene == currentScene)
                    {
                        stateFound = true;
                        
                        // Set active/inactive state based on whether it's collected
                        heartContainer.gameObject.SetActive(!state.isCollected);
                        
                        // Also mark the collected flag in the component
                        var collectedField = heartContainer.GetType().GetField("isCollected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (collectedField != null)
                        {
                            collectedField.SetValue(heartContainer, state.isCollected);
                        }
                        
                        Debug.Log($"Restored HeartContainer state: {state.objectName}, collected: {state.isCollected}, active: {!state.isCollected}");
                        break;
                    }
                }
            }
            
            // If no state was found for this heart container, assume it's not collected (set active)
            if (!stateFound)
            {
                heartContainer.gameObject.SetActive(true);
                // Reset its collected state
                var collectedField = heartContainer.GetType().GetField("isCollected", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (collectedField != null)
                {
                    collectedField.SetValue(heartContainer, false);
                }
                
                Debug.Log($"No saved state found for HeartContainer: {heartContainer.gameObject.name}, assuming not collected (set active)");
            }
        }
    }

    // Helper method to get all ability states
    private Dictionary<string, bool> GetAbilityDictionary()
    {
        Dictionary<string, bool> abilities = new Dictionary<string, bool>();
        
        // Get all ability states using reflection
        Type abilityManagerType = typeof(AbilityManager);
        var unlockedAbilitiesField = abilityManagerType.GetField("unlockedAbilities", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        if (unlockedAbilitiesField != null)
        {
            var unlockedAbilities = (Dictionary<string, bool>)unlockedAbilitiesField.GetValue(null);
            if (unlockedAbilities != null)
            {
                foreach (var pair in unlockedAbilities)
                {
                    abilities[pair.Key] = pair.Value;
                }
            }
        }
        
        return abilities;
    }

    // Method to restore ability states from parallel lists
    private void RestoreAbilitiesFromLists(SaveData saveData)
    {
        if (saveData.abilityNames == null || saveData.abilityStates == null ||
            saveData.abilityNames.Count != saveData.abilityStates.Count)
        {
            Debug.LogWarning("Ability lists are missing or mismatched in save data.");
            return;
        }

        // Use reflection to get the static unlockedAbilities dictionary in AbilityManager
        Type abilityManagerType = typeof(AbilityManager);
        var unlockedAbilitiesField = abilityManagerType.GetField("unlockedAbilities",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (unlockedAbilitiesField != null)
        {
            var unlockedAbilities = (Dictionary<string, bool>)unlockedAbilitiesField.GetValue(null);
            if (unlockedAbilities != null)
            {
                unlockedAbilities.Clear();
                for (int i = 0; i < saveData.abilityNames.Count; i++)
                {
                    unlockedAbilities[saveData.abilityNames[i]] = saveData.abilityStates[i];
                }
                Debug.Log($"Restored {saveData.abilityNames.Count} ability states from lists.");
            }
            else
            {
                Debug.LogWarning("AbilityManager.unlockedAbilities dictionary is null.");
            }
        }
        else
        {
            Debug.LogWarning("Could not find AbilityManager.unlockedAbilities field via reflection.");
        }
    }

    /// <summary>
    /// Loads the main menu scene and controls the Try Again message based on ending type
    /// </summary>
    /// <param name="showTryAgainMessage">True for bad ending, false for good ending</param>
    public void LoadMainMenuWithTryAgainControl(bool showTryAgainMessage)
    {
        // Store the try again message state for after scene load
        StartCoroutine(LoadMainMenuCoroutine(showTryAgainMessage));
    }

    private IEnumerator LoadMainMenuCoroutine(bool showTryAgainMessage)
    {
        // Load the main menu scene
        SceneManager.LoadScene("MainMenu");
        
        // Wait for the scene to load
        yield return new WaitForSeconds(0.5f);
        
        // Find the Try Again MSG parent object (should always be active)
        GameObject tryAgainMsg = GameObject.Find("Try Again MSG");
        if (tryAgainMsg != null)
        {
            // Find the child object that contains the actual text/visual elements
            Transform childTransform = tryAgainMsg.transform.GetChild(0);
            if (childTransform != null)
            {
                GameObject tryAgainChild = childTransform.gameObject;
                tryAgainChild.SetActive(showTryAgainMessage);
                Debug.Log($"Try Again MSG child set to: {showTryAgainMessage}");
            }
            else
            {
                Debug.LogWarning("Try Again MSG parent found but no child object exists!");
            }
        }
        else
        {
            Debug.LogWarning("Try Again MSG object not found in MainMenu scene!");
        }
    }
}
