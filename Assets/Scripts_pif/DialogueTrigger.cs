using UnityEngine;
using Yarn.Unity;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [Tooltip("The name of the Yarn dialogue node to start when triggered")]
    public string dialogueName = "Start";
    [Tooltip("The name of the second Yarn dialogue node to start after the first")]
    public string dialogue2Name;
    [Tooltip("The name of the third Yarn dialogue node to start after the second")]
    public string dialogue3Name;
    [Tooltip("The name of the backtrack Yarn dialogue node to start (optional)")]
    public string dialogueBacktrackName;

    [Header("Trigger Settings")]
    [Tooltip("Tag that can trigger this dialogue (e.g., 'Player')")]
    string triggerTag = "Player";

    private int dialogueStage = 0; // 0 = first, 1 = second, 2 = third
    private bool backtrackPlayed = false;
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
        // Check for DialogueRunner
        if (dialogueRunner == null)
        {
            Debug.LogError($"DialogueTrigger on {gameObject.name}: Cannot start dialogue - no DialogueRunner found!");
            return;
        }

        // Prevent triggering if dialogue is already running
        if (dialogueRunner.IsDialogueRunning)
        {
            Debug.Log($"DialogueTrigger on {gameObject.name}: Dialogue already running, skipping trigger.");
            return;
        }

        // Check GameManager for forestCleansed
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null && gm.forestCleansed && !backtrackPlayed && !string.IsNullOrEmpty(dialogueBacktrackName))
        {
            // Play backtrack dialogue only once when forestCleansed is true
            try
            {
                dialogueRunner.StartDialogue(dialogueBacktrackName);
                backtrackPlayed = true;
                Debug.Log($"DialogueTrigger on {gameObject.name}: Started backtrack dialogue node '{dialogueBacktrackName}'");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DialogueTrigger on {gameObject.name}: Failed to start backtrack dialogue '{dialogueBacktrackName}': {e.Message}");
            }
            return;
        }
        if (backtrackPlayed)
        {
            // No more dialogue should ever play from this trigger
            return;
        }

        string nodeToPlay = null;
        if (dialogueStage == 0 && !string.IsNullOrEmpty(dialogueName))
        {
            nodeToPlay = dialogueName;
        }
        else if (dialogueStage == 1 && !string.IsNullOrEmpty(dialogue2Name))
        {
            nodeToPlay = dialogue2Name;
        }
        else if (dialogueStage == 2 && !string.IsNullOrEmpty(dialogue3Name))
        {
            nodeToPlay = dialogue3Name;
        }
        else
        {
            // No more dialogues to play
            return;
        }

        try
        {
            dialogueRunner.StartDialogue(nodeToPlay);
            dialogueStage++;
            Debug.Log($"DialogueTrigger on {gameObject.name}: Started dialogue node '{nodeToPlay}'");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"DialogueTrigger on {gameObject.name}: Failed to start dialogue '{nodeToPlay}': {e.Message}");
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
        dialogueStage = 0;
        backtrackPlayed = false;
    }
}
