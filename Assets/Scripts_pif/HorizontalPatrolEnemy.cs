using UnityEngine;

public class HorizontalPatrolEnemy : MonoBehaviour
{
    [SerializeField] private float patrolDistance = 5f; // Distance to patrol in each direction
    [SerializeField] private float moveSpeed = 2f; // Speed of movement
    
    private Vector3 startPosition; // Starting position of the enemy
    private Vector3 leftBound; // Left boundary of patrol
    private Vector3 rightBound; // Right boundary of patrol
    private int direction = 1; // 1 for right, -1 for left
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Store the starting position
        startPosition = transform.position;
        
        // Calculate patrol boundaries
        leftBound = startPosition + Vector3.left * patrolDistance;
        rightBound = startPosition + Vector3.right * patrolDistance;
    }

    // Update is called once per frame
    void Update()
    {
        // Move the enemy
        transform.Translate(Vector3.right * direction * moveSpeed * Time.deltaTime);
        
        // Check if we've reached the boundaries and need to turn around
        if (direction == 1 && transform.position.x >= rightBound.x)
        {
            // Reached right boundary, turn left
            direction = -1;
            transform.position = new Vector3(rightBound.x, transform.position.y, transform.position.z);
        }
        else if (direction == -1 && transform.position.x <= leftBound.x)
        {
            // Reached left boundary, turn right
            direction = 1;
            transform.position = new Vector3(leftBound.x, transform.position.y, transform.position.z);
        }
    }
    
    // Draw patrol boundaries in the Scene view for debugging
    void OnDrawGizmosSelected()
    {
        Vector3 gizmoStartPos = Application.isPlaying ? startPosition : transform.position;
        Vector3 gizmoLeftBound = gizmoStartPos + Vector3.left * patrolDistance;
        Vector3 gizmoRightBound = gizmoStartPos + Vector3.right * patrolDistance;
        
        // Draw the patrol line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(gizmoLeftBound, gizmoRightBound);
        
        // Draw boundary markers
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(gizmoLeftBound, 0.2f);
        Gizmos.DrawWireSphere(gizmoRightBound, 0.2f);
        
        // Draw center position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(gizmoStartPos, 0.1f);
    }
}
