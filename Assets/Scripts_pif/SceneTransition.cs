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
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;
    
    private bool hasTriggered = false; // Prevent multiple triggers
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object has the "Player" tag
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            
            if (enableDebugLog)
            {
                Debug.Log($"Player entered scene transition trigger. Loading scene in {transitionDelay} seconds...");
            }
            
            // Load the scene with optional delay
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
