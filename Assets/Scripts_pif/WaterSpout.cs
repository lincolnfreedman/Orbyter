using UnityEngine;

public class WaterSpout : MonoBehaviour
{
    private PlayerController_pif playerController;
    private Vector2 spoutDirection;
    private bool isTouchingSomething = false;
    private Collider2D[] playerColliders;

    public void Initialize(PlayerController_pif player, Vector2 direction)
    {
        playerController = player;
        spoutDirection = direction;
        
        // Get all player's colliders to ignore collision with them
        playerColliders = player.GetComponents<Collider2D>();
        if (playerColliders.Length == 0)
        {
            playerColliders = player.GetComponentsInChildren<Collider2D>();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object is on the "Fire" layer and destroy it
        if (other.gameObject.layer == LayerMask.NameToLayer("Fire"))
        {
            Destroy(other.gameObject);
            return;
        }
        
        // Ignore collision with the player
        if (IsPlayerCollider(other))
            return;
            
        // Ignore collision with EventTrigger layer objects
        if (other.gameObject.layer == LayerMask.NameToLayer("EventTrigger"))
            return;
            
        isTouchingSomething = true;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Check if the object is on the "Fire" layer and destroy it
        if (other.gameObject.layer == LayerMask.NameToLayer("Fire"))
        {
            Destroy(other.gameObject);
            return;
        }
        
        // Ignore collision with the player
        if (IsPlayerCollider(other))
            return;
            
        // Ignore collision with EventTrigger layer objects
        if (other.gameObject.layer == LayerMask.NameToLayer("EventTrigger"))
            return;
            
        isTouchingSomething = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the object is on the "Fire" layer and destroy it
        if (other.gameObject.layer == LayerMask.NameToLayer("Fire"))
        {
            Destroy(other.gameObject);
            return;
        }
        
        // Ignore collision with the player
        if (IsPlayerCollider(other))
            return;
            
        // Check if we're still touching anything by doing a simple overlap check
        Collider2D spoutCollider = GetComponent<Collider2D>();
        if (spoutCollider != null)
        {
            Collider2D[] overlapping = new Collider2D[10];
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.useLayerMask = false;
            
            int count = spoutCollider.Overlap(filter, overlapping);
            
            // Check if any overlapping colliders are not the player and not Fire objects and not EventTrigger objects
            isTouchingSomething = false;
            for (int i = 0; i < count; i++)
            {
                if (overlapping[i] != null)
                {
                    // Destroy Fire objects
                    if (overlapping[i].gameObject.layer == LayerMask.NameToLayer("Fire"))
                    {
                        Destroy(overlapping[i].gameObject);
                        continue;
                    }
                    
                    // Ignore EventTrigger layer objects
                    if (overlapping[i].gameObject.layer == LayerMask.NameToLayer("EventTrigger"))
                    {
                        continue;
                    }
                    
                    // Check if it's not the player
                    if (!IsPlayerCollider(overlapping[i]))
                    {
                        isTouchingSomething = true;
                        break;
                    }
                }
            }
        }
        else
        {
            isTouchingSomething = false;
        }
    }

    private bool IsPlayerCollider(Collider2D collider)
    {
        if (playerController == null) return false;
        
        // Check if it's the player's transform
        if (collider.transform == playerController.transform)
            return true;
            
        // Check against all player colliders
        if (playerColliders != null)
        {
            for (int i = 0; i < playerColliders.Length; i++)
            {
                if (playerColliders[i] == collider)
                    return true;
            }
        }
        
        return false;
    }

    public bool IsTouchingSomething()
    {
        // Also do a manual overlap check each time this is called
        PerformOverlapCheck();
        return isTouchingSomething;
    }
    
    private void PerformOverlapCheck()
    {
        Collider2D spoutCollider = GetComponent<Collider2D>();
        if (spoutCollider != null)
        {
            Collider2D[] overlapping = new Collider2D[10];
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true; // Check for both trigger and non-trigger colliders
            filter.useLayerMask = false;
            
            int count = spoutCollider.Overlap(filter, overlapping);
            
            // Check if any overlapping colliders are not the player
            isTouchingSomething = false;
            for (int i = 0; i < count; i++)
            {
                if (overlapping[i] != null)
                {
                    // Check if the object is on the "Fire" layer and destroy it
                    if (overlapping[i].gameObject.layer == LayerMask.NameToLayer("Fire"))
                    {
                        Destroy(overlapping[i].gameObject);
                        continue;
                    }
                    
                    // Ignore EventTrigger layer objects
                    if (overlapping[i].gameObject.layer == LayerMask.NameToLayer("EventTrigger"))
                    {
                        continue;
                    }
                    
                    // Check if it's not the player
                    if (!IsPlayerCollider(overlapping[i]))
                    {
                        isTouchingSomething = true;
                        break;
                    }
                }
            }
        }
    }
}
