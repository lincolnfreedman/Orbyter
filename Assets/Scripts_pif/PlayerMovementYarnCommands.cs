using UnityEngine;
using Yarn.Unity;
using System.Collections;

public class PlayerMovementYarnCommands : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("Reference to the player controller")]
    public PlayerController_pif playerController;
    
    [Header("Dialogue System")]
    [Tooltip("Reference to the DialogueRunner")]
    public DialogueRunner dialogueRunner;
    
    [Header("Character Audio Clips")]
    [Tooltip("Audio clip for Jay talking")]
    public AudioClip jayTalk;
    [Tooltip("Audio clip for Molly talking")]
    public AudioClip mollyTalk;
    [Tooltip("Audio clip for Samuel talking")]
    public AudioClip samuelTalk;
    [Tooltip("Audio clip for Scarf talking")]
    public AudioClip scarfTalk;
    [Tooltip("Audio clip for Tucker talking")]
    public AudioClip tuckerTalk;
    [Tooltip("Audio clip for Dusty talking")]
    public AudioClip dustyTalk;
    
    [Header("Audio Settings")]
    [Tooltip("Number of times to play the character talk sound per line")]
    public int talkSoundRepeats = 3;
    [Tooltip("Delay between each talk sound repetition")]
    public float delayBetweenSounds = 0.5f;
    
    private AudioSource audioSource;
    private Coroutine currentTalkCoroutine;
    
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
        
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        if (playerController == null)
        {
            Debug.LogError("PlayerMovementYarnCommands: Could not find PlayerController_pif!");
        }
        
        if (dialogueRunner == null)
        {
            Debug.LogError("PlayerMovementYarnCommands: Could not find DialogueRunner!");
        }
        // Register commands with the DialogueRunner
        if (dialogueRunner != null)
        {
            dialogueRunner.AddCommandHandler("disable_movement", DisablePlayerMovement);
            dialogueRunner.AddCommandHandler("enable_movement", EnablePlayerMovement);
            
            // Register simple character dialogue commands
            dialogueRunner.AddCommandHandler("jay_talk", StartJayTalk);
            dialogueRunner.AddCommandHandler("molly_talk", StartMollyTalk);
            dialogueRunner.AddCommandHandler("samuel_talk", StartSamuelTalk);
            dialogueRunner.AddCommandHandler("scarf_talk", StartScarfTalk);
            dialogueRunner.AddCommandHandler("tucker_talk", StartTuckerTalk);
            dialogueRunner.AddCommandHandler("dusty_talk", StartDustyTalk);
        }
    }
    
    private void Start()
    {
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
    
    // Character talk methods
    public void StartJayTalk()
    {
        PlayCharacterDialogue(jayTalk, "Jay");
    }
    
    public void StartMollyTalk()
    {
        PlayCharacterDialogue(mollyTalk, "Molly");
    }
    
    public void StartSamuelTalk()
    {
        PlayCharacterDialogue(samuelTalk, "Samuel");
    }
    
    public void StartScarfTalk()
    {
        PlayCharacterDialogue(scarfTalk, "Scarf");
    }
    
    public void StartTuckerTalk()
    {
        PlayCharacterDialogue(tuckerTalk, "Tucker");
    }
    
    public void StartDustyTalk()
    {
        PlayCharacterDialogue(dustyTalk, "Dusty");
    }
    
    private void PlayCharacterDialogue(AudioClip clip, string characterName)
    {
        if (audioSource != null && clip != null)
        {
            // Stop any currently playing audio
            if (currentTalkCoroutine != null)
            {
                StopCoroutine(currentTalkCoroutine);
            }
            
            // Start the coroutine to play the sound multiple times
            currentTalkCoroutine = StartCoroutine(PlayDialogueRepeated(clip, characterName));
        }
        else
        {
            Debug.LogWarning($"PlayerMovementYarnCommands: Cannot play {characterName} dialogue audio - AudioSource or AudioClip is null!");
        }
    }
    
    private IEnumerator PlayDialogueRepeated(AudioClip clip, string characterName)
    {
        // Play the sound the specified number of times
        for (int i = 0; i < talkSoundRepeats; i++)
        {
            audioSource.clip = clip;
            audioSource.loop = false;
            audioSource.Play();
            
            // Wait for the clip to finish playing, then add the delay
            yield return new WaitForSeconds(clip.length + delayBetweenSounds);
        }
        
        // Clear the coroutine reference when done
        currentTalkCoroutine = null;
    }
}
