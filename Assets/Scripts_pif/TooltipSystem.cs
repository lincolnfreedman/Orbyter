using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TooltipSystem : MonoBehaviour
{
    public static TooltipSystem Instance { get; private set; }
    
    [Header("UI References")]
    [Tooltip("The TextMeshPro component to update")]
    public TextMeshProUGUI textComponent;
    
    [Header("Player Input")]
    [Tooltip("Manually assign the PlayerInput component")]
    public PlayerInput playerInput;
    
    private string currentDismissAction;
    private InputAction dismissAction;
    private float displayTimer = 0f;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple TooltipSystems found! Destroying duplicate on " + gameObject.name);
            Destroy(this);
            return;
        }
        
        // Validate setup
        if (textComponent == null)
        {
            textComponent = GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent == null)
            {
                Debug.LogError("TooltipSystem: No TextMeshProUGUI component found! Please assign one or add it to a child object.");
            }
        }
        
        // Start hidden
        gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    public void ShowTooltip(string text, string dismissInputAction, float duration = 0f)
    {
        if (textComponent != null)
        {
            textComponent.text = text;
        }
        
        currentDismissAction = dismissInputAction;
        displayTimer = duration;
        SetupInputAction();
        
        gameObject.SetActive(true);
    }
    
    public void HideTooltip()
    {
        gameObject.SetActive(false);
        ClearInputAction();
    }
    
    private void SetupInputAction()
    {
        if (string.IsNullOrEmpty(currentDismissAction)) 
        {
            return;
        }
        
        // Check if player input is assigned
        if (playerInput == null)
        {
            Debug.LogWarning("TooltipSystem: PlayerInput not assigned! Please assign it in the inspector.");
            return;
        }
        
        // Set up the dismiss input action
        try
        {
            dismissAction = playerInput.actions[currentDismissAction];
            if (dismissAction == null)
            {
                Debug.LogWarning($"TooltipSystem: Input action '{currentDismissAction}' not found!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"TooltipSystem: Failed to set up input action '{currentDismissAction}': {e.Message}");
        }
    }
    
    private void ClearInputAction()
    {
        dismissAction = null;
        currentDismissAction = null;
        displayTimer = 0f;
    }
    
    private void Update()
    {
        // Handle timed dismissal
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
        if (dismissAction != null && dismissAction.triggered)
        {
            Debug.Log($"TooltipSystem: Dismiss action '{currentDismissAction}' triggered - hiding tooltip");
            HideTooltip();
        }
    }
}