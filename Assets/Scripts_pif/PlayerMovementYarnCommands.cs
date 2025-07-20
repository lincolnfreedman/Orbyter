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
    
    [Header("Character Sprites")]
    [Tooltip("Samuel happy sprite")]
    public Sprite samuelHappy;
    [Tooltip("Samuel mad sprite")]
    public Sprite samuelMad;
    [Tooltip("Samuel default sprite")]
    public Sprite samuelDefault;
    [Tooltip("Molly default sprite")]
    public Sprite mollyDefault;
    [Tooltip("Molly nervous sprite")]
    public Sprite mollyNervous;
    [Tooltip("Molly sad sprite")]
    public Sprite mollySad;
    [Tooltip("Tucker default sprite")]
    public Sprite tuckerDefault;
    [Tooltip("Tucker tired sprite")]
    public Sprite tuckerTired;
    [Tooltip("Tucker scared sprite")]
    public Sprite tuckerScared;
    
    [Header("UI References")]
    [Tooltip("Left dialogue sprite UI object")]
    public GameObject leftDialogueSprite;
    [Tooltip("Right dialogue sprite UI object")]
    public GameObject rightDialogueSprite;
    [Tooltip("Dialogue text component for color changes")]
    public TMPro.TextMeshProUGUI dialogueText;
    
    [Header("Audio Settings")]
    [Tooltip("Number of times to play the character talk sound per line")]
    public int talkSoundRepeats = 3;
    [Tooltip("Delay between each talk sound repetition")]
    public float delayBetweenSounds = 0.5f;
    
    public AudioSource audioSource;
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
            dialogueRunner.AddCommandHandler("disable_dialogue", DisableDialogue);
            dialogueRunner.AddCommandHandler("enable_dialogue", EnableDialogue);
            
            // Register simple character dialogue commands
            dialogueRunner.AddCommandHandler("jay_talk", StartJayTalk);
            dialogueRunner.AddCommandHandler("molly_talk", StartMollyTalk);
            dialogueRunner.AddCommandHandler("samuel_talk", StartSamuelTalk);
            dialogueRunner.AddCommandHandler("scarf_talk", StartScarfTalk);
            dialogueRunner.AddCommandHandler("tucker_talk", StartTuckerTalk);
            dialogueRunner.AddCommandHandler("dusty_talk", StartDustyTalk);
            
            // Register sprite commands for setting right dialogue sprite
            dialogueRunner.AddCommandHandler("show_samuel_happy", ShowSamuelHappy);
            dialogueRunner.AddCommandHandler("show_samuel_mad", ShowSamuelMad);
            dialogueRunner.AddCommandHandler("show_samuel_default", ShowSamuelDefault);
            dialogueRunner.AddCommandHandler("show_molly_default", ShowMollyDefault);
            dialogueRunner.AddCommandHandler("show_molly_nervous", ShowMollyNervous);
            dialogueRunner.AddCommandHandler("show_molly_sad", ShowMollySad);
            dialogueRunner.AddCommandHandler("show_tucker_default", ShowTuckerDefault);
            dialogueRunner.AddCommandHandler("show_tucker_tired", ShowTuckerTired);
            dialogueRunner.AddCommandHandler("show_tucker_scared", ShowTuckerScared);
            
            // Register text formatting commands
            dialogueRunner.AddCommandHandler("highlight_text", () => {
                HighlightText();
            });
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

    /// <summary>
    /// Disable dialogue UI and enable player movement
    /// </summary>
    public void DisableDialogue()
    {
        if (leftDialogueSprite != null)
        {
            leftDialogueSprite.SetActive(false);
        }
        
        if (rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(false);
        }
        
        if (playerController != null)
        {
            playerController.EnableMovement();
        }
    }

    /// <summary>
    /// Enable dialogue UI and disable player movement
    /// </summary>
    public void EnableDialogue()
    {
        if (leftDialogueSprite != null)
        {
            leftDialogueSprite.SetActive(true);
        }
        
        if (playerController != null)
        {
            playerController.DisableMovement();
        }
    }

    // Sprite commands for setting right dialogue sprite
    public void ShowSamuelHappy()
    {
        if (samuelHappy != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = samuelHappy;
        }
    }

    public void ShowSamuelMad()
    {
        if (samuelMad != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = samuelMad;
        }
    }

    public void ShowSamuelDefault()
    {
        if (samuelDefault != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = samuelDefault;
        }
    }

    public void ShowMollyDefault()
    {
        if (mollyDefault != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = mollyDefault;
        }
    }

    public void ShowMollyNervous()
    {
        if (mollyNervous != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = mollyNervous;
        }
    }

    public void ShowMollySad()
    {
        if (mollySad != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = mollySad;
        }
    }

    public void ShowTuckerDefault()
    {
        if (tuckerDefault != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = tuckerDefault;
        }
    }

    public void ShowTuckerTired()
    {
        if (tuckerTired != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = tuckerTired;
        }
    }

    public void ShowTuckerScared()
    {
        if (tuckerScared != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = tuckerScared;
        }
    }

    /// <summary>
    /// Highlights dialogue text with red color for one line only
    /// Usage: <<highlight_text>>
    /// </summary>
    public void HighlightText()
    {
        if (dialogueText != null)
        {
            StartCoroutine(HighlightTextTemporarily(Color.red));
        }
        else
        {
            Debug.LogWarning("PlayerMovementYarnCommands: Dialogue text reference is null!");
        }
    }

    private System.Collections.IEnumerator HighlightTextTemporarily(Color highlightColor)
    {
        // Store the original color
        Color originalColor = dialogueText.color;
        
        // Change to highlight color
        dialogueText.color = highlightColor;
        Debug.Log("set dialogue text color to " + highlightColor + 
        " it was originally " + originalColor); 
        // Wait for the next dialogue line to be displayed
        // We'll wait for a frame, then check if the text has changed
        yield return new WaitForEndOfFrame();
        
        // Monitor for text changes or dialogue completion
        string currentText = dialogueText.text;
        float timeoutTimer = 0f;
        float maxTimeout = 10f; // Maximum time to wait before reverting color
        
        while (timeoutTimer < maxTimeout)
        {
            yield return new WaitForSeconds(0.1f);
            timeoutTimer += 0.1f;
            
            // If text has changed or dialogue runner is not running, revert color
            if (dialogueText.text != currentText || !dialogueRunner.IsDialogueRunning)
            {
                break;
            }
        }
        
        // Restore original color
        dialogueText.color = originalColor;
    }
}
