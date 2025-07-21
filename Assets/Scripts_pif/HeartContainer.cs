using UnityEngine;

public class HeartContainer : MonoBehaviour
{
    [Header("Collection Settings")]
    [Tooltip("Tag that can collect this heart container (e.g., 'Player')")]
    public string playerTag = "Player";

    [Tooltip("Whether this heart container has been collected (for debugging)")]
    [SerializeField] private bool isCollected = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object is the player and hasn't been collected yet
        if (other.CompareTag(playerTag) && !isCollected)
        {
            CollectHeartContainer(other.gameObject);
        }
    }

    private void CollectHeartContainer(GameObject player)
    {
        // Mark as collected to prevent double collection
        isCollected = true;

        // Get the PlayerController component
        PlayerController_pif playerController = player.GetComponent<PlayerController_pif>();
        if (playerController != null)
        {
            // Add to heart container count
            playerController.AddHeartContainer();

            // Play collection sound effect
            Player_pip playerSFX = player.GetComponent<Player_pip>();
            if (playerSFX != null)
            {
                playerSFX.PlayerSFX(8); // Same as ability unlock sound
            }

            Debug.Log("Heart Container collected!");
        }
        else
        {
            Debug.LogWarning("HeartContainer: Could not find PlayerController_pif component on player!");
        }

        // Destroy the heart container
        Destroy(gameObject);
    }
}
