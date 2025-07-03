using UnityEngine;
using System.Collections.Generic;

public static class EnvironmentTracker
{
    public static bool IsStandingOnSurface(Rigidbody2D rb, bool isDigPhasing)
    {
        // Don't consider grounded during dig phasing
        if (isDigPhasing)
        {
            return false;
        }

        return HasContactWithNormal(rb, contact => IsGroundSurface(contact.collider) && IsUpwardFacing(contact.normal));
    }

    public static bool HasContactWithNormal(Rigidbody2D rb, System.Func<ContactPoint2D, bool> condition)
    {
        ContactPoint2D[] contacts = new ContactPoint2D[10];
        int contactCount = rb.GetContacts(contacts);

        for (int i = 0; i < contactCount; i++)
        {
            if (condition(contacts[i]))
            {
                return true;
            }
        }

        return false;
    }


    public static bool IsGroundSurface(Collider2D collider)
    {
        return collider.CompareTag("Ground") || collider.CompareTag("Climbable");
    }


    public static bool IsUpwardFacing(Vector2 normal)
    {
        return normal.y > 0.7f; // 0.7 allows for slightly sloped surfaces
    }
    public static bool IsTouchingClimbableWall(Rigidbody2D rb)
    {
        // Get all current collisions
        ContactPoint2D[] contacts = new ContactPoint2D[10];
        int contactCount = rb.GetContacts(contacts);
        
        for (int i = 0; i < contactCount; i++)
        {
            // Check if touching a climbable wall (vertical surface)
            // Wall normals point horizontally (left or right)
            if (contacts[i].collider.CompareTag("Climbable") && Mathf.Abs(contacts[i].normal.x) > 0.7f)
            {
                return true;
            }
        }  
        return false;
    }
    public static bool IsTouchingDiggableWall(Collider2D normalCollider, Collider2D glidingCollider, LayerMask diggableLayer)
    {
        // Use layer-based detection for climbable objects
        // Get the currently active collider (normal or gliding)
        Collider2D activeCollider = normalCollider.enabled ? normalCollider : glidingCollider;
        
        // Create a contact filter for the climbable layer
        ContactFilter2D contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(diggableLayer);
        contactFilter.useLayerMask = true;
        contactFilter.useTriggers = true; // Include triggers in overlap check
        
        // Check for overlap with any colliders on the climbable layer
        List<Collider2D> overlappingColliders = new List<Collider2D>();
        int overlapCount = activeCollider.Overlap(contactFilter, overlappingColliders);
        
        return overlapCount > 0;
    }
    
    public static int GetWallDirection(Rigidbody2D rb)
    {
        // Get all current collisions
        ContactPoint2D[] contacts = new ContactPoint2D[10];
        int contactCount = rb.GetContacts(contacts);

        for (int i = 0; i < contactCount; i++)
        {
            // Check if touching a climbable wall (vertical surface)
            if (contacts[i].collider.CompareTag("Climbable") && Mathf.Abs(contacts[i].normal.x) > 0.7f)
            {
                // Return the direction of the wall normal
                // If normal points right (positive x), wall is on the left (-1)
                // If normal points left (negative x), wall is on the right (1)
                return contacts[i].normal.x > 0 ? -1 : 1;
            }
        }

        return 0; // No wall found
    }
}
