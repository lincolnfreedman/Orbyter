using UnityEngine;

public class SFXManager_pip : MonoBehaviour
{
    public static SFXManager_pip Instance { get; private set; }
    
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource walkingSource;
    [SerializeField] private AudioClip footstepsClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip galaxseaSprayClip;
    [SerializeField] private AudioClip wallClingClip;
    [SerializeField] private float walkingSoundDelay = 0.3f; // Delay between walking sound repeats
    
    private bool isWalkingSoundActive = false;
    private float walkingSoundTimer = 0f;
    private float walkingGraceTimer = 0f; // Prevents rapid start/stop spam

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Update grace timer
        if (walkingGraceTimer > 0f)
        {
            walkingGraceTimer -= Time.deltaTime;
        }
        
        // Handle walking sound timing
        if (isWalkingSoundActive)
        {
            walkingSoundTimer -= Time.deltaTime;
            
            if (walkingSoundTimer <= 0f)
            {
                // Play the footstep sound
                if (walkingSource != null && footstepsClip != null)
                {
                    walkingSource.PlayOneShot(footstepsClip);
                }
                
                // Reset the timer
                walkingSoundTimer = walkingSoundDelay;
            }
        }
    }

    public void PlayWalking()
    {
        if (!isWalkingSoundActive && walkingGraceTimer <= 0f)
        {
            isWalkingSoundActive = true;
            walkingSoundTimer = walkingSoundDelay; // Set timer for first sound
        }
        // If already active or in grace period, don't start - let it continue its cycle
    }

    public void StopPlayingWalking()
    {
        if (isWalkingSoundActive)
        {
            isWalkingSoundActive = false;
            walkingSoundTimer = 0f;
            walkingGraceTimer = 0.1f; // Short grace period to prevent immediate restart
        }
    }

    public void PlayJump()
    {
        if (audioSource != null && jumpClip != null)
        {
            audioSource.PlayOneShot(jumpClip);
        }
    }

    public void PlayGalaxseaSpray()
    {
        if (audioSource != null && galaxseaSprayClip != null)
        {
            audioSource.PlayOneShot(galaxseaSprayClip);
        }
    }

    public void PlayWallCling()
    {
        if (audioSource != null && wallClingClip != null)
        {
            audioSource.PlayOneShot(wallClingClip);
        }
    }
}
