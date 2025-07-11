using UnityEngine;
using UnityEngine.UI;

public class HealthDisplay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Sprite for full health heart")]
    public Sprite fullHeartSprite;
    
    [Header("Heart Layout")]
    [Tooltip("Position of the leftmost heart")]
    public Vector2 leftmostHeartPosition = new Vector2(-100f, 0f);
    [Tooltip("Spacing between each heart")]
    public float heartSpacing = 60f;
    [Tooltip("Size of each heart image")]
    public Vector2 heartSize = new Vector2(50f, 50f);
    
    [Header("Player Reference")]
    [Tooltip("Reference to the player controller")]
    public PlayerController_pif playerController;
    
    // Dynamic heart array
    private Image[] healthHearts;
    private int lastMaxHealth = -1;
    
    private void Start()
    {
        // Find player controller if not assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController_pif>();
        }
        
        if (playerController == null)
        {
            Debug.LogError("HealthDisplay: Could not find PlayerController_pif component!");
            return;
        }
        
        if (fullHeartSprite == null)
        {
            Debug.LogError("HealthDisplay: Full Heart Sprite is not assigned!");
            return;
        }
        
        // Initialize health display
        SetupHeartImages();
        UpdateHealthDisplay();
    }
    
    private void Update()
    {
        // Update health display every frame
        if (playerController != null)
        {
            // Check if max health has changed and recreate hearts if needed
            int currentMaxHealth = playerController.GetMaxHealth();
            if (currentMaxHealth != lastMaxHealth)
            {
                SetupHeartImages();
                lastMaxHealth = currentMaxHealth;
            }
            
            UpdateHealthDisplay();
        }
    }
    
    private void UpdateHealthDisplay()
    {
        if (healthHearts == null || healthHearts.Length == 0)
            return;
            
        int currentHealth = playerController.GetCurrentHealth();
        
        // Update each heart image
        for (int i = 0; i < healthHearts.Length; i++)
        {
            if (healthHearts[i] != null)
            {
                // Show or hide based on current health
                if (i < currentHealth)
                {
                    healthHearts[i].gameObject.SetActive(true);
                }
                else
                {
                    healthHearts[i].gameObject.SetActive(false);
                }
            }
        }
    }
    
    private void SetupHeartImages()
    {
        if (playerController == null || fullHeartSprite == null)
            return;
            
        // Clear existing hearts
        if (healthHearts != null)
        {
            for (int i = 0; i < healthHearts.Length; i++)
            {
                if (healthHearts[i] != null)
                {
                    DestroyImmediate(healthHearts[i].gameObject);
                }
            }
        }
        
        int maxHealth = playerController.GetMaxHealth();
        healthHearts = new Image[maxHealth];
        
        // Create heart images
        for (int i = 0; i < maxHealth; i++)
        {
            // Create heart GameObject
            GameObject heartObj = new GameObject($"Heart_{i}");
            heartObj.transform.SetParent(transform, false);
            
            // Add Image component
            Image heartImage = heartObj.AddComponent<Image>();
            heartImage.sprite = fullHeartSprite;
            heartImage.raycastTarget = false; // Hearts don't need to be clickable
            
            // Set up RectTransform
            RectTransform rectTransform = heartObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = heartSize;
            rectTransform.anchoredPosition = new Vector2(
                leftmostHeartPosition.x + (i * heartSpacing), 
                leftmostHeartPosition.y
            );
            
            // Store reference
            healthHearts[i] = heartImage;
        }
        
        Debug.Log($"HealthDisplay: Created {maxHealth} heart images");
    }
}
