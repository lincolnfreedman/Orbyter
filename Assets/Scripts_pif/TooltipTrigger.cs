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
    
    // Static reference to track which tooltip currently has an active dismiss action
    private static TooltipTrigger currentActiveTooltip = null;
    
    private bool hasTriggered = false;
    private bool playerInTrigger = false;
    private float displayTimer = 0f;
    private InputAction dismissAction;
    private PlayerInput playerInput;

    void Start()
    {
        // Validate setup
        ValidateSetup();
        
        // Ensure tooltip UI starts hidden
        if (tooltipUI != null)
        {
            tooltipUI.SetActive(false);
        }
    }
    
    void Update()
    {
        // Handle timed tooltips
        if (displayDuration > 0f && tooltipUI != null && tooltipUI.activeSelf)
        {
            displayTimer -= Time.deltaTime;
            if (displayTimer <= 0f)
            {
                HideTooltip();
            }
        }
        
        // Handle input-based dismissal
        if (requireInputToDismiss && tooltipUI != null && tooltipUI.activeSelf && dismissAction != null)
        {
            if (dismissAction.triggered)
            {
                HideTooltip();
            }
        }
    }
    
    private void SetupInputAction()
    {
        // Try to find player input in the scene
        if (playerInput == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(triggerTag);
            if (playerObject != null)
            {
                playerInput = playerObject.GetComponent<PlayerInput>();
            }
        }
        
        // Set up the dismiss input action
        if (playerInput != null && !string.IsNullOrEmpty(dismissInputAction))
        {
            try
            {
                dismissAction = playerInput.actions[dismissInputAction];
                if (dismissAction == null)
                {
                    Debug.LogWarning($"TooltipTrigger on {gameObject.name}: Input action '{dismissInputAction}' not found!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"TooltipTrigger on {gameObject.name}: Failed to set up input action '{dismissInputAction}': {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"TooltipTrigger on {gameObject.name}: Could not find PlayerInput component for dismiss functionality!");
        }
    }

    private void ValidateSetup()
    {
        if (tooltipUI == null)
        {
            Debug.LogWarning($"TooltipTrigger on {gameObject.name}: No tooltip UI assigned!");
            return;
        }
        
        // If no text component is assigned, try to find one
        if (textComponent == null)
        {
            textComponent = tooltipUI.GetComponent<TextMeshProUGUI>();
            if (textComponent == null)
            {
                textComponent = tooltipUI.GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        
        if (textComponent == null)
        {
            Debug.LogWarning($"TooltipTrigger on {gameObject.name}: No TextMeshProUGUI component found in tooltip UI!");
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
                HideTooltip();
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
        
        if (tooltipUI == null)
        {
            Debug.LogError($"TooltipTrigger on {gameObject.name}: Cannot show tooltip - no UI element assigned!");
            return;
        }
        
        // Set the text if we have a text component
        if (textComponent != null && !string.IsNullOrEmpty(tooltipText))
        {
            textComponent.text = tooltipText;
        }
        
        // Show the tooltip UI
        tooltipUI.SetActive(true);
        
        // Set up dismiss input action if required
        if (requireInputToDismiss)
        {
            // Clear any previously active tooltip's dismiss action
            if (currentActiveTooltip != null && currentActiveTooltip != this)
            {
                currentActiveTooltip.ClearDismissAction();
            }
            
            // Set this as the current active tooltip
            currentActiveTooltip = this;
            SetupInputAction();
        }
        
        // Set up timer if using timed display
        if (displayDuration > 0f)
        {
            displayTimer = displayDuration;
        }
        
        // Mark as triggered
        hasTriggered = true;
        
        Debug.Log($"TooltipTrigger on {gameObject.name}: Showing tooltip - '{tooltipText}'");
    }
    
    private void HideTooltip()
    {
        if (tooltipUI != null)
        {
            tooltipUI.SetActive(false);
            Debug.Log($"TooltipTrigger on {gameObject.name}: Hiding tooltip");
        }
        
        // Clear dismiss action when tooltip is hidden
        ClearDismissAction();
    }
    
    private void ClearDismissAction()
    {
        dismissAction = null;
        
        // Clear static reference if this was the active tooltip
        if (currentActiveTooltip == this)
        {
            currentActiveTooltip = null;
        }
    }
}
