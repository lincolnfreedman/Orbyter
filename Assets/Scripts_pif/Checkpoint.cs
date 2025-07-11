using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Visual effect or sound when checkpoint is activated")]
    public GameObject activationEffect;
    [Tooltip("Should this checkpoint be activated only once?")]
    public bool oneTimeUse = false;
    
    private bool hasBeenActivated = false;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the player has entered the checkpoint trigger
        if (other.CompareTag("Player"))
        {
            // Don't activate if this is a one-time use checkpoint that's already been used
            if (oneTimeUse && hasBeenActivated)
                return;
            
            // Get the PlayerController component
            PlayerController_pif playerController = other.GetComponent<PlayerController_pif>();
            if (playerController != null)
            {
                // Set this checkpoint as the new respawn point
                playerController.SetCheckpoint(transform.position);
                
                // Restore player health to full
                playerController.RestoreFullHealth();
                
                // Mark as activated
                hasBeenActivated = true;
                
                // Play activation effect if available
                if (activationEffect != null)
                {
                    Instantiate(activationEffect, transform.position, transform.rotation);
                }
                
                Debug.Log($"Checkpoint activated at position: {transform.position} - Health restored!");
            }
        }
    }
}
