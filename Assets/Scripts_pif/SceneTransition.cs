using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [Header("Scene Transition Settings")]
    [SerializeField] private string sceneName;
    [Tooltip("Optional: Use scene build index instead of name. Set to -1 to use scene name.")]
    [SerializeField] private int sceneIndex = -1;
    [Tooltip("Delay before transitioning to the new scene")]
    [SerializeField] private float transitionDelay = 0f;
    
    [Header("Cutscene Settings")]
    [Tooltip("Cutscene object to enable when transition is triggered")]
    [SerializeField] private GameObject cutsceneObject;
    [Tooltip("Music player audio source to mute during cutscene")]
    [SerializeField] private AudioSource musicPlayer;
    [Tooltip("Audio source for crash landing sound effect")]
    [SerializeField] private AudioSource crashLandingAudioSource;
    [Tooltip("Crash landing sound effect clip")]
    [SerializeField] private AudioClip crashLandingSound;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;
    
    private bool hasTriggered = false; // Prevent multiple triggers
    private PlayerController_pif playerController;
    private float originalMusicVolume;
    private Animator cutsceneAnimator;
    
    private void Start()
    {
        // Store original music volume
        if (musicPlayer != null)
        {
            originalMusicVolume = musicPlayer.volume;
        }
        
        // Get cutscene animator if cutscene object is assigned
        if (cutsceneObject != null)
        {
            cutsceneAnimator = cutsceneObject.GetComponent<Animator>();
            if (cutsceneAnimator == null)
            {
                Debug.LogWarning("SceneTransition: Cutscene object has no Animator component!");
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object has the "Player" tag
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            
            // Get player controller reference
            playerController = other.GetComponent<PlayerController_pif>();
            
            if (enableDebugLog)
            {
                Debug.Log($"Player entered scene transition trigger. Starting cutscene...");
            }
            
            StartCutscene();
        }
    }
    
    private void StartCutscene()
    {
        // Disable player movement
        if (playerController != null)
        {
            playerController.DisableMovement();
            if (enableDebugLog)
            {
                Debug.Log("Player movement disabled for cutscene");
            }
        }
        
        // Mute music
        if (musicPlayer != null)
        {
            musicPlayer.volume = 0f;
            if (enableDebugLog)
            {
                Debug.Log("Music volume set to 0");
            }
        }
        
        // Play crash landing sound effect
        if (crashLandingAudioSource != null && crashLandingSound != null)
        {
            crashLandingAudioSource.PlayOneShot(crashLandingSound);
            if (enableDebugLog)
            {
                Debug.Log("Playing crash landing sound effect");
            }
        }
        
        // Enable cutscene object
        if (cutsceneObject != null)
        {
            cutsceneObject.SetActive(true);
            if (enableDebugLog)
            {
                Debug.Log("Cutscene object enabled");
            }
            
            // Start monitoring for animation completion
            if (cutsceneAnimator != null)
            {
                StartCoroutine(WaitForAnimationComplete());
            }
            else
            {
                // If no animator, wait for the transition delay then load scene
                if (transitionDelay > 0f)
                {
                    Invoke(nameof(LoadScene), transitionDelay);
                }
                else
                {
                    LoadScene();
                }
            }
        }
        else
        {
            // No cutscene object, proceed with normal delay/loading
            if (transitionDelay > 0f)
            {
                Invoke(nameof(LoadScene), transitionDelay);
            }
            else
            {
                LoadScene();
            }
        }
    }
    
    private System.Collections.IEnumerator WaitForAnimationComplete()
    {
        // Wait one frame to ensure animation has started
        yield return null;
        
        // Wait until the current animation state is finished
        while (cutsceneAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            yield return null;
        }
        
        if (enableDebugLog)
        {
            Debug.Log("Cutscene animation completed, loading scene...");
        }
        
        LoadScene();
    }
    
    private void LoadScene()
    {
        
        // Use scene index if specified (not -1), otherwise use scene name
        if (sceneIndex >= 0)
        {
            if (enableDebugLog)
            {
                Debug.Log($"Loading scene by index: {sceneIndex}");
            }
            SceneManager.LoadScene(sceneIndex);
        }
        else if (!string.IsNullOrEmpty(sceneName))
        {
            if (enableDebugLog)
            {
                Debug.Log($"Loading scene by name: {sceneName}");
            }
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("SceneTransition: No valid scene name or index specified!");
        }
    }
    
    // Public method to trigger scene transition manually from other scripts
    public void TriggerTransition()
    {
        if (!hasTriggered)
        {
            hasTriggered = true;
            LoadScene();
        }
    }
    
    // Reset the trigger state (useful for testing or special cases)
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
