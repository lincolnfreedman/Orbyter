using UnityEngine;

public class CameraController_pip : MonoBehaviour
{
    [SerializeField] private PlayerController_pif playerController;
    [SerializeField] private float facingBias = 2f; // How much to offset camera based on facing direction
    [SerializeField] private float biasLerpSpeed = 2f; // How quickly to transition to/from biased position
    
    private float currentBiasOffset = 0f; // Current horizontal offset being applied
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (playerController != null)
        {
            // Follow the player's position
            Transform playerTransform = playerController.transform;
            
            // Check if player is idle (no velocity)
            Rigidbody2D playerRb = playerController.GetComponent<Rigidbody2D>();
            bool isIdle = playerRb != null && Mathf.Approximately(playerRb.linearVelocity.magnitude, 0f);
            
            // Calculate target bias offset
            float targetBiasOffset = 0f;
            if (isIdle)
            {
                // Apply bias towards facing direction when idle
                targetBiasOffset = playerController.facingDirection * facingBias;
            }
            
            // Smoothly transition to target bias
            currentBiasOffset = Mathf.Lerp(currentBiasOffset, targetBiasOffset, biasLerpSpeed * Time.deltaTime);
            
            // Apply position with bias
            transform.position = new Vector3(
                playerTransform.position.x + currentBiasOffset, 
                playerTransform.position.y, 
                transform.position.z
            );
        }
    }
}
