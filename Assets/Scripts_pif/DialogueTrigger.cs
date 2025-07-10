using UnityEngine;
using Yarn.Unity;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [Tooltip("The name of the Yarn dialogue node to start when triggered")]
    public string dialogueName = "Start";
    
    [Header("Trigger Settings")]
    [Tooltip("Tag that can trigger this dialogue (e.g., 'Player')")]
    string triggerTag = "Player";
    
    [Tooltip("Whether this dialogue can only be triggered once")]
    public bool triggerOnce = false;
    
    private bool hasTriggered = false;
    private DialogueRunner dialogueRunner;

    void Start()
    {
        // Find the DialogueRunner in the scene
        dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        
        if (dialogueRunner == null)
        {
            Debug.LogWarning($"DialogueTrigger on {gameObject.name}: No DialogueRunner found in scene!");
        }
        
        if (string.IsNullOrEmpty(dialogueName))
        {
            Debug.LogWarning($"DialogueTrigger on {gameObject.name}: Dialogue name is empty!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the correct object entered the trigger
        if (other.CompareTag(triggerTag))
        {
            TriggerDialogue();
        }
    }
    
    private void TriggerDialogue()
    {
        // Check if we should prevent triggering
        if (triggerOnce && hasTriggered)
        {
            return;
        }
        
        if (dialogueRunner == null)
        {
            Debug.LogError($"DialogueTrigger on {gameObject.name}: Cannot start dialogue - no DialogueRunner found!");
            return;
        }
        
        if (string.IsNullOrEmpty(dialogueName))
        {
            Debug.LogError($"DialogueTrigger on {gameObject.name}: Cannot start dialogue - dialogue name is empty!");
            return;
        }
        
        // Check if dialogue is already running
        if (dialogueRunner.IsDialogueRunning)
        {
            Debug.Log($"DialogueTrigger on {gameObject.name}: Dialogue already running, skipping trigger.");
            return;
        }
        
        // Start the dialogue
        try
        {
            dialogueRunner.StartDialogue(dialogueName);
            hasTriggered = true;
            Debug.Log($"DialogueTrigger on {gameObject.name}: Started dialogue node '{dialogueName}'");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"DialogueTrigger on {gameObject.name}: Failed to start dialogue '{dialogueName}': {e.Message}");
        }
    }
    
    // Optional: Public method to manually trigger dialogue
    public void ManualTrigger()
    {
        TriggerDialogue();
    }
    
    // Optional: Reset the trigger so it can be used again
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
