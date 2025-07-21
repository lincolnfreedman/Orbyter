using UnityEngine;
using UnityEngine.InputSystem;

public class DoorOpeningLever : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("The door object to destroy when lever is activated")]
    public GameObject door;
    
    [Header("Interaction Settings")]
    [Tooltip("Tag that can interact with this lever (e.g., 'Player')")]
    public string playerTag = "Player";
    
    [Tooltip("Whether this lever can only be used once")]
    public bool singleUse = true;
    
    [Header("Visual/Audio Feedback")]
    [Tooltip("Optional animator for lever animations")]
    public Animator leverAnimator;
    
    [Tooltip("Optional audio source for lever sound effects")]
    public AudioSource audioSource;
    
    [Tooltip("Sprite to switch to when lever is activated")]
    public Sprite flippedLeverSprite;
    
    private bool playerInRange = false;
    private bool leverActivated = false;
    private PlayerController_pif playerController;
    private InputAction interactAction;
    private SpriteRenderer spriteRenderer;
    private Sprite originalSprite;

    void Start()
    {
        // Validate setup
        if (door == null)
        {
            Debug.LogWarning($"DoorOpeningLever on {gameObject.name}: No door assigned!");
        }
        
        // Get and cache the sprite renderer and original sprite
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
        }
        else
        {
            Debug.LogWarning($"DoorOpeningLever on {gameObject.name}: No SpriteRenderer found!");
        }
        
        if (flippedLeverSprite == null)
        {
            Debug.LogWarning($"DoorOpeningLever on {gameObject.name}: No flipped lever sprite assigned!");
        }
    }

    void Update()
    {
        // Check for interaction input when player is in range
        if (playerInRange && !leverActivated && interactAction != null)
        {
            if (interactAction.triggered)
            {
                ActivateLever();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            SetupPlayerInput(other.gameObject);
            Debug.Log($"DoorOpeningLever on {gameObject.name}: Player entered interaction range");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            Debug.Log($"DoorOpeningLever on {gameObject.name}: Player left interaction range");
        }
    }
    
    private void SetupPlayerInput(GameObject playerObject)
    {
        // Get the PlayerController component from the player
        if (playerController == null)
        {
            playerController = playerObject.GetComponent<PlayerController_pif>();
        }
        
        // Set up the interact input action from PlayerController
        if (playerController != null)
        {
            interactAction = playerController.InteractAction;
            if (interactAction == null)
            {
                Debug.LogWarning($"DoorOpeningLever on {gameObject.name}: Interact action not found in PlayerController!");
            }
        }
        else
        {
            Debug.LogWarning($"DoorOpeningLever on {gameObject.name}: Could not find PlayerController component!");
        }
    }
    
    private void ActivateLever()
    {
        // Check if we can activate (single use check)
        if (singleUse && leverActivated)
        {
            return;
        }
        
        // Validate door exists
        if (door == null)
        {
            Debug.LogError($"DoorOpeningLever on {gameObject.name}: Cannot activate - no door assigned!");
            return;
        }
        
        // Play animation if available
        if (leverAnimator != null)
        {
            leverAnimator.SetTrigger("Activate");
        }
        
        // Play sound effect if available
        if (audioSource != null)
        {
            audioSource.Play();
        }
        
        // Switch to flipped lever sprite
        if (spriteRenderer != null && flippedLeverSprite != null)
        {
            spriteRenderer.sprite = flippedLeverSprite;
            Debug.Log($"DoorOpeningLever on {gameObject.name}: Switched to flipped lever sprite");
        }
        
        // Destroy the door
        Debug.Log($"DoorOpeningLever on {gameObject.name}: Activating lever - destroying door '{door.name}'");
        Destroy(door);
        
        // Mark as activated
        leverActivated = true;
        
        // Optional: Disable this lever if single use
        if (singleUse)
        {
            // Could disable the collider or change visual state here
            GetComponent<Collider2D>().enabled = false;
            Debug.Log($"DoorOpeningLever on {gameObject.name}: Lever deactivated (single use)");
        }
    }
    
    // Optional: Public method for manual activation
    public void ManualActivate()
    {
        ActivateLever();
    }
    
    // Optional: Reset the lever (useful for testing or multi-use scenarios)
    public void ResetLever()
    {
        leverActivated = false;
        GetComponent<Collider2D>().enabled = true;
        
        // Reset to original sprite
        if (spriteRenderer != null && originalSprite != null)
        {
            spriteRenderer.sprite = originalSprite;
        }
        
        Debug.Log($"DoorOpeningLever on {gameObject.name}: Lever reset");
    }
    
    // Optional: Check if lever can be activated
    public bool CanActivate()
    {
        return playerInRange && !leverActivated && door != null;
    }
}
