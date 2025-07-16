using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TooltipTrigger : MonoBehaviour
{
    [Header("Tooltip Settings")]
    [Tooltip("The text to display in the tooltip")]
    [TextArea(2, 4)]
    public string tooltipText = "Enter tooltip text here";
    
    [Tooltip("The UI element to show/hide (should contain TextMeshPro component)")]
    public GameObject tooltipUI;
    
    [Tooltip("The TextMeshPro component to update (if null, will search in tooltipUI)")]
    public TextMeshProUGUI textComponent;
    
    [Header("Trigger Settings")]
    [Tooltip("Tag that can trigger this tooltip (e.g., 'Player')")]
    public string triggerTag = "Player";
    
    [Tooltip("Whether this tooltip can only be triggered once")]
    public bool triggerOnce = false;
    
    [Tooltip("Duration to show tooltip (0 = show indefinitely until player leaves or input is pressed)")]
    public float displayDuration = 0f;
    
    [Header("Dismiss Settings")]
    [Tooltip("If true, tooltip stays until input is pressed. If false, tooltip hides when player leaves trigger")]
    public bool requireInputToDismiss = false;
    
    [Tooltip("The input action name required to dismiss the tooltip (e.g., 'Interact', 'Jump')")]
    public string dismissInputAction;
    
    private bool hasTriggered = false;
    private bool playerInTrigger = false;

    void Start()
    {
        // Initialize the static tooltip system with this trigger's UI elements
        if (tooltipUI != null)
        {
            TooltipSystem.Initialize(tooltipUI, textComponent);
        }
        else
        {
            Debug.LogWarning($"TooltipTrigger on {gameObject.name}: No tooltip UI assigned! Please assign a tooltip UI element.");
        }
        
        if (string.IsNullOrEmpty(tooltipText))
        {
            Debug.LogWarning($"TooltipTrigger on {gameObject.name}: Tooltip text is empty!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag))
        {
            playerInTrigger = true;
            ShowTooltip();
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag))
        {
            playerInTrigger = false;
            
            // Only hide if not using timed display AND not requiring input to dismiss
            if (displayDuration <= 0f && !requireInputToDismiss)
            {
                TooltipSystem.HideTooltip();
            }
        }
    }
    
    private void ShowTooltip()
    {
        // Check if we should prevent triggering
        if (triggerOnce && hasTriggered)
        {
            return;
        }
        
        // Show tooltip through static system
        TooltipSystem.ShowTooltip(
            tooltipText, 
            displayDuration, 
            requireInputToDismiss, 
            dismissInputAction,
            triggerTag
        );
        
        // Mark as triggered
        hasTriggered = true;
    }
}

public static class TooltipSystem
{
    private static GameObject tooltipUI;
    private static TextMeshProUGUI textComponent;
    private static bool isInitialized = false;
    private static float displayTimer = 0f;
    private static bool requiresInput = false;
    private static InputAction dismissAction;
    private static PlayerInput playerInput;
    private static TooltipUpdater updater;
    
    public static void Initialize(GameObject uiElement, TextMeshProUGUI textComp)
    {
        if (isInitialized) return;
        
        tooltipUI = uiElement;
        textComponent = textComp;
        
        // Validate setup
        if (tooltipUI == null)
        {
            Debug.LogError("TooltipSystem: No tooltip UI assigned! Please assign a tooltip UI element.");
            return;
        }
        
        // If no text component provided, try to find one
        if (textComponent == null && tooltipUI != null)
        {
            textComponent = tooltipUI.GetComponent<TextMeshProUGUI>();
            if (textComponent == null)
            {
                textComponent = tooltipUI.GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        
        if (textComponent == null)
        {
            Debug.LogError("TooltipSystem: No TextMeshProUGUI component found! Please assign a text component or ensure your tooltip UI has one.");
            return;
        }
        
        // Make tooltip UI persist across scenes
        // First check if there's already a Canvas with DontDestroyOnLoad
        Canvas tooltipCanvas = tooltipUI.GetComponentInParent<Canvas>();
        if (tooltipCanvas != null)
        {
            // Check if this canvas is already marked as DontDestroyOnLoad
            GameObject rootObject = tooltipCanvas.gameObject;
            while (rootObject.transform.parent != null)
            {
                rootObject = rootObject.transform.parent.gameObject;
            }
            
            // Mark the root canvas object as DontDestroyOnLoad
            Object.DontDestroyOnLoad(rootObject);
            Debug.Log($"TooltipSystem: Marked {rootObject.name} as DontDestroyOnLoad for cross-scene persistence.");
        }
        else
        {
            // If tooltip UI is not under a canvas, just mark the tooltip itself
            Object.DontDestroyOnLoad(tooltipUI);
            Debug.Log($"TooltipSystem: Marked {tooltipUI.name} as DontDestroyOnLoad for cross-scene persistence.");
        }
        
        // Ensure tooltip starts hidden
        tooltipUI.SetActive(false);
        
        // Set up updater component
        updater = tooltipUI.GetComponent<TooltipUpdater>();
        if (updater == null)
        {
            updater = tooltipUI.AddComponent<TooltipUpdater>();
        }
        
        isInitialized = true;
        Debug.Log("TooltipSystem: Successfully initialized with custom UI elements and cross-scene persistence.");
    }
    
    public static void ShowTooltip(string text, float duration, bool requireInput, string inputAction, string playerTag)
    {
        if (!isInitialized)
        {
            Debug.LogError("TooltipSystem: System not initialized! Make sure at least one TooltipTrigger has valid UI elements assigned.");
            return;
        }
        
        // Check if UI elements still exist (in case of scene changes or manual deletion)
        if (tooltipUI == null || textComponent == null)
        {
            Debug.LogWarning("TooltipSystem: UI elements were destroyed. Resetting system - please reinitialize in the new scene.");
            ResetSystem();
            return;
        }
        
        // Set the text
        if (textComponent != null)
        {
            textComponent.text = text;
        }
        
        // Show the tooltip
        tooltipUI.SetActive(true);
        
        // Set up timing and input
        if (duration > 0f)
        {
            displayTimer = duration;
        }
        else
        {
            displayTimer = 0f;
        }
        
        requiresInput = requireInput;
        
        // Set up input action if required
        if (requireInput && !string.IsNullOrEmpty(inputAction))
        {
            SetupInputAction(inputAction, playerTag);
        }
        
        Debug.Log($"TooltipSystem: Showing tooltip - '{text}'");
    }
    
    public static void HideTooltip()
    {
        if (tooltipUI != null)
        {
            tooltipUI.SetActive(false);
            Debug.Log("TooltipSystem: Hiding tooltip");
        }
        
        // Clear input action
        dismissAction = null;
        displayTimer = 0f;
        requiresInput = false;
    }
    
    public static void ResetSystem()
    {
        tooltipUI = null;
        textComponent = null;
        isInitialized = false;
        displayTimer = 0f;
        requiresInput = false;
        dismissAction = null;
        playerInput = null;
        updater = null;
        Debug.Log("TooltipSystem: System reset. Will need to be reinitialized in the new scene.");
    }
    
    public static bool IsInitialized()
    {
        return isInitialized && tooltipUI != null && textComponent != null;
    }
    
    private static void SetupInputAction(string inputAction, string playerTag)
    {
        // Try to find player input (search again in case of scene change)
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerInput = playerObject.GetComponent<PlayerInput>();
        }
        
        // Set up the dismiss input action
        if (playerInput != null)
        {
            try
            {
                dismissAction = playerInput.actions[inputAction];
                if (dismissAction == null)
                {
                    Debug.LogWarning($"TooltipSystem: Input action '{inputAction}' not found!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"TooltipSystem: Failed to set up input action '{inputAction}': {e.Message}");
            }
        }
    }
    
    // Called by TooltipUpdater component
    public static void Update()
    {
        if (!isInitialized || tooltipUI == null || !tooltipUI.activeSelf) return;
        
        // Handle timed tooltips
        if (displayTimer > 0f)
        {
            displayTimer -= Time.deltaTime;
            if (displayTimer <= 0f)
            {
                HideTooltip();
                return;
            }
        }
        
        // Handle input-based dismissal
        if (requiresInput && dismissAction != null && dismissAction.triggered)
        {
            HideTooltip();
        }
    }
}

// Helper component to call TooltipSystem.Update()
public class TooltipUpdater : MonoBehaviour
{
    void Update()
    {
        TooltipSystem.Update();
    }
}
