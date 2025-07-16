using UnityEngine;

public class AbilityUnlock : MonoBehaviour
{
    [Header("Ability Settings")]
    [Tooltip("The name of the ability to unlock (e.g., 'Spray', 'Dig')")]
    public string abilityName = "Spray";
    
    [Tooltip("Tag that can trigger this unlock (e.g., 'Player')")]
    public string triggerTag = "Player";
    
    [Tooltip("Whether this unlock can only be triggered once")]
    public bool triggerOnce = true;
    
    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(triggerTag))
        {
            // Check if we should prevent triggering
            if (triggerOnce && hasTriggered)
            {
                return;
            }
            
            // Unlock the ability
            AbilityManager.UnlockAbility(abilityName);
            
            // Mark as triggered
            hasTriggered = true;
            
            Debug.Log($"AbilityUnlock: Player unlocked ability '{abilityName}'");
            
            // Optionally disable the GameObject after unlock
            if (triggerOnce)
            {
                gameObject.SetActive(false);
            }
        }
    }
}

public static class AbilityManager
{
    // Dictionary to track which abilities are unlocked
    private static System.Collections.Generic.Dictionary<string, bool> unlockedAbilities = 
        new System.Collections.Generic.Dictionary<string, bool>();
    
    // Initialize default abilities (unlocked from start)
    static AbilityManager()
    {
        InitializeDefaultAbilities();
    }
    
    private static void InitializeDefaultAbilities()
    {
        // These abilities are unlocked from the start
        unlockedAbilities["Jump"] = true;
        unlockedAbilities["Glide"] = true;
        unlockedAbilities["FastFall"] = true;
        unlockedAbilities["WallCling"] = true;
        unlockedAbilities["Interact"] = true;
        unlockedAbilities["WideScan"] = true;
        unlockedAbilities["Pause"] = true;
        unlockedAbilities["Move"] = true;
        unlockedAbilities["Vertical"] = true;
        
        // These abilities start locked
        unlockedAbilities["Spray"] = false;
        unlockedAbilities["Dig"] = false;
        
        Debug.Log("AbilityManager: Initialized with default abilities");
    }
    
    /// <summary>
    /// Unlock a specific ability
    /// </summary>
    public static void UnlockAbility(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName))
        {
            Debug.LogWarning("AbilityManager: Cannot unlock ability with empty name");
            return;
        }
        
        bool wasLocked = !IsAbilityUnlocked(abilityName);
        unlockedAbilities[abilityName] = true;
        
        if (wasLocked)
        {
            Debug.Log($"AbilityManager: Unlocked ability '{abilityName}'");
        }
    }
    
    /// <summary>
    /// Check if a specific ability is unlocked
    /// </summary>
    public static bool IsAbilityUnlocked(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName))
        {
            return false;
        }
        
        return unlockedAbilities.ContainsKey(abilityName) && unlockedAbilities[abilityName];
    }
    
    /// <summary>
    /// Lock a specific ability (for testing or special cases)
    /// </summary>
    public static void LockAbility(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName))
        {
            Debug.LogWarning("AbilityManager: Cannot lock ability with empty name");
            return;
        }
        
        unlockedAbilities[abilityName] = false;
        Debug.Log($"AbilityManager: Locked ability '{abilityName}'");
    }
    
    /// <summary>
    /// Get all unlocked abilities (for debugging or UI)
    /// </summary>
    public static string[] GetUnlockedAbilities()
    {
        var unlocked = new System.Collections.Generic.List<string>();
        foreach (var kvp in unlockedAbilities)
        {
            if (kvp.Value)
            {
                unlocked.Add(kvp.Key);
            }
        }
        return unlocked.ToArray();
    }
    
    /// <summary>
    /// Reset all abilities to their default state (for new game)
    /// </summary>
    public static void ResetAbilities()
    {
        unlockedAbilities.Clear();
        InitializeDefaultAbilities();
        Debug.Log("AbilityManager: Reset all abilities to default state");
    }
}
