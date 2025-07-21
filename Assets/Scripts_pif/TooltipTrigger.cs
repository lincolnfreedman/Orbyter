using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TooltipTrigger : MonoBehaviour
{
    [Header("Tooltip Settings")]
    [Tooltip("The text to display in the tooltip")]
    [TextArea(2, 4)]
    public string tooltipText = "Enter tooltip text here";
    
    [Header("Trigger Settings")]
    [Tooltip("Tag that can trigger this tooltip (e.g., 'Player')")]
    public string triggerTag = "Player";
    
    [Tooltip("Whether this tooltip can only be triggered once")]
    public bool triggerOnce = false;
    
    [Header("Dismiss Settings")]
    [Tooltip("If true, tooltip stays until input is pressed. If false, tooltip hides after displayDuration")]
    public bool requireInputToDismiss = true;
    
    [Tooltip("The input action name required to dismiss the tooltip (e.g., 'Interact', 'Jump')")]
    public string dismissInputAction = "Interact";
    
    [Tooltip("Duration to show tooltip (0 = show indefinitely). Only used when requireInputToDismiss is false")]
    public float displayDuration = 3f;
    
    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag))
        {
            ShowTooltip();
        }
    }
    
    private void ShowTooltip()
    {
        // Check if we should prevent triggering
        if (triggerOnce && hasTriggered)
        {
            return;
        }
        
        // Show tooltip through static instance
        if (TooltipSystem.Instance != null)
        {
            if (requireInputToDismiss)
            {
                TooltipSystem.Instance.ShowTooltip(tooltipText, dismissInputAction, 0f);
            }
            else
            {
                TooltipSystem.Instance.ShowTooltip(tooltipText, "", displayDuration);
            }
            hasTriggered = true;
        }
        else
        {
            Debug.LogWarning("TooltipTrigger: No TooltipSystem found in scene! Please add a TooltipSystem component to your tooltip canvas.");
        }
    }
    
    private void HideTooltip()
    {
        if (TooltipSystem.Instance != null)
        {
            TooltipSystem.Instance.HideTooltip();
        }
    }
}
