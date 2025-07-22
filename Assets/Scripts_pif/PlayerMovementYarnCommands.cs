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
    [Tooltip("Jay sing sprite")]
    public Sprite jaySing;
    [Tooltip("Jay sigh sprite")]
    public Sprite jaySigh;
    [Tooltip("Jay default sprite")]
    public Sprite jayDefault;
    [Tooltip("Dusty default sprite")]
    public Sprite dustyDefault;
    [Tooltip("Dusty sweat sprite")]
    public Sprite dustySweat;
    [Tooltip("Dusty relief sprite")]
    public Sprite dustyRelief;
    [Tooltip("Scarf default sprite")]
    public Sprite scarfDefault;
    [Tooltip("Scarf nervous sprite")]
    public Sprite scarfNervous;
    [Tooltip("Scarf angry sprite")]
    public Sprite scarfAngry;
    
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
    
    [Header("Fire Chase")]
    [Tooltip("GameObject to enable when fire chase starts")]
    public GameObject chasingFires;
    
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
            dialogueRunner.AddCommandHandler("enable_fire_chase", EnableFireChase);
            
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
            dialogueRunner.AddCommandHandler("show_jay_sing", ShowJaySing);
            dialogueRunner.AddCommandHandler("show_jay_sigh", ShowJaySigh);
            dialogueRunner.AddCommandHandler("show_jay_default", ShowJayDefault);
            dialogueRunner.AddCommandHandler("show_dusty_default", ShowDustyDefault);
            dialogueRunner.AddCommandHandler("show_dusty_sweat", ShowDustySweat);
            dialogueRunner.AddCommandHandler("show_dusty_relief", ShowDustyRelief);
            
            // Register sprite commands for setting left dialogue sprite (Scarf)
            dialogueRunner.AddCommandHandler("show_scarf_default", ShowScarfDefault);
            dialogueRunner.AddCommandHandler("show_scarf_nervous", ShowScarfNervous);
            dialogueRunner.AddCommandHandler("show_scarf_angry", ShowScarfAngry);
            
            // Register text formatting commands
            dialogueRunner.AddCommandHandler("highlight_text", () => {
                HighlightText();
            });
            
            // Register adventure log commands
            dialogueRunner.AddCommandHandler<int>("unlock_adventure_page", UnlockAdventurePage);
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
            // Always set left dialogue sprite to scarfDefault
            if (scarfDefault != null)
            {
                leftDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = scarfDefault;
            }
        }
        
        if (playerController != null)
        {
            playerController.DisableMovement();
        }
    }

    public void EnableFireChase()
    {
        if (chasingFires != null)
        {
            chasingFires.SetActive(true);
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

    public void ShowJaySing()
    {
        if (jaySing != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = jaySing;
        }
    }

    public void ShowJaySigh()
    {
        if (jaySigh != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = jaySigh;
        }
    }

    public void ShowJayDefault()
    {
        if (jayDefault != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = jayDefault;
        }
    }

    public void ShowDustyDefault()
    {
        if (dustyDefault != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = dustyDefault;
        }
    }

    public void ShowDustySweat()
    {
        if (dustySweat != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = dustySweat;
        }
    }

    public void ShowDustyRelief()
    {
        if (dustyRelief != null && rightDialogueSprite != null)
        {
            rightDialogueSprite.SetActive(true);
            rightDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = dustyRelief;
        }
    }

    // Scarf sprite commands for setting left dialogue sprite
    public void ShowScarfDefault()
    {
        if (scarfDefault != null && leftDialogueSprite != null)
        {
            leftDialogueSprite.SetActive(true);
            leftDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = scarfDefault;
        }
    }

    public void ShowScarfNervous()
    {
        if (scarfNervous != null && leftDialogueSprite != null)
        {
            leftDialogueSprite.SetActive(true);
            leftDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = scarfNervous;
        }
    }

    public void ShowScarfAngry()
    {
        if (scarfAngry != null && leftDialogueSprite != null)
        {
            leftDialogueSprite.SetActive(true);
            leftDialogueSprite.GetComponent<UnityEngine.UI.Image>().sprite = scarfAngry;
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

    /// <summary>
    /// Unlocks the adventure log page at the specified index.
    /// Usage in Yarn: <<unlock_adventure_page 3>>
    /// </summary>
    public void UnlockAdventurePage(int pageIndex)
    {
        if (GameManager.instance != null && GameManager.instance.pagesUnlocked != null)
        {
            if (pageIndex >= 0 && pageIndex < GameManager.instance.pagesUnlocked.Length)
            {
                GameManager.instance.pagesUnlocked[pageIndex] = true;
                Debug.Log($"Adventure log page {pageIndex} unlocked via Yarn command.");
            }
            else
            {
                Debug.LogWarning($"UnlockAdventurePage: pageIndex {pageIndex} is out of bounds.");
            }
        }
        else
        {
            Debug.LogWarning("UnlockAdventurePage: GameManager or pagesUnlocked array is null.");
        }
    }
}
