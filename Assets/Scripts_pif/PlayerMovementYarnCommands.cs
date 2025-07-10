using UnityEngine;
using Yarn.Unity;

public class PlayerMovementYarnCommands : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Reference to the player controller")]
    public PlayerController_pif playerController;
    
    [Header("Dialogue System")]
    [Tooltip("Reference to the DialogueRunner")]
    public DialogueRunner dialogueRunner;
    
    private void Awake()
    {
        // Try to find the player controller if not assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController_pif>();
        }
        
        // Try to find the dialogue runner if not assigned
        if (dialogueRunner == null)
        {
            dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        }
        
        if (playerController == null)
        {
            Debug.LogError("PlayerMovementYarnCommands: Could not find PlayerController_pif!");
        }
        
        if (dialogueRunner == null)
        {
            Debug.LogError("PlayerMovementYarnCommands: Could not find DialogueRunner!");
        }
    }
    
    private void Start()
    {
        // Register commands with the DialogueRunner
        if (dialogueRunner != null)
        {
            dialogueRunner.AddCommandHandler("disable_movement", DisablePlayerMovement);
            dialogueRunner.AddCommandHandler("enable_movement", EnablePlayerMovement);
        }
    }
    
    public void DisablePlayerMovement()
    {
        if (playerController != null)
        {
            playerController.DisableMovement();
        }
        else
        {
            Debug.LogWarning("PlayerMovementYarnCommands: PlayerController reference is null!");
        }
    }
    
    public void EnablePlayerMovement()
    {
        if (playerController != null)
        {
            playerController.EnableMovement();
        }
        else
        {
            Debug.LogWarning("PlayerMovementYarnCommands: PlayerController reference is null!");
        }
    }
}
