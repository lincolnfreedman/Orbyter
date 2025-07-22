using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class AdventureLog : MonoBehaviour
{
    [SerializeField]
    private Sprite[] pages;
    [SerializeField]
    private Image page1;
    [SerializeField]
    private Image page2;
    [SerializeField]
    private GameObject undiscovered1;
    [SerializeField]
    private GameObject undiscovered2;

    [SerializeField]
    private float pageFlipCooldown = 0.5f;
    private float pageFlipCooldownTimer = 0f;

    [SerializeField]
    private int currentPage;
    [SerializeField]
    private int lastPage;
    
    private Player_pip player;
    private PlayerInput playerInput;
    private InputAction moveAction;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find the player to access SFX system
        player = FindFirstObjectByType<Player_pip>();
        if (player == null)
        {
            Debug.LogWarning("Player_pip not found! Adventure log SFX will not work.");
        }
        
        // Find PlayerInput to access input actions
        playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
        }
        else
        {
            Debug.LogWarning("PlayerInput not found! Adventure log input navigation will not work.");
        }
        
        currentPage = 1;
        lastPage = pages.Length/2;
        UpdatePages();
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePageFlipCooldown();

        // Handle horizontal input for page flipping
        if (moveAction != null && gameObject.activeInHierarchy)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            
            // Check to avoid flipping pages every frame
            if (pageFlipCooldownTimer <= 0f)
            {
                // Check for horizontal input (with a threshold to avoid accidental triggers)
                if (Mathf.Abs(moveInput.x) > 0.5f)
                {
                    if (moveInput.x > 0 && currentPage < lastPage)
                    {
                        // Right input - next page
                        NextPage();
                    }
                    else if (moveInput.x < 0 && currentPage > 1)
                    {
                        // Left input - previous page
                        PreviousPage();
                    }
                    pageFlipCooldownTimer = pageFlipCooldown;
                }
            }
        }
    }

    public void NextPage()
    {
        if (currentPage < lastPage)
        {
            currentPage++;
            UpdatePages();
            
            // Play page turn sound effect
            if (player != null)
            {
                player.PlayerSFXOneShot(6);
                Debug.Log("next page playing");
            }
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            UpdatePages();
            
            // Play page turn sound effect
            if (player != null)
            {
                player.PlayerSFXOneShot(6);
            }
        }
    }

    public void CloseAdventureLog()
    {
        // Play close sound effect
        if (player != null)
        {
            player.PlayerSFXOneShot(7);
        }
        
        // Close the adventure log (assuming it's a UI panel that should be deactivated)
        gameObject.SetActive(false);
    }

    private void UpdatePages()
    {
        int page1Index = currentPage * 2 - 1;
        int page2Index = currentPage * 2 - 2;

        page2.sprite = pages[page1Index];
        page1.sprite = pages[page2Index];
        
        if (GameManager.instance.pagesUnlocked[page1Index])
        {
            undiscovered1.SetActive(false);
        }
        else if (!GameManager.instance.pagesUnlocked[page1Index])
        {
            undiscovered1.SetActive(true);
        }

        if (GameManager.instance.pagesUnlocked[page2Index])
        {
            undiscovered2.SetActive(false);
        }
        else if (!GameManager.instance.pagesUnlocked[page2Index])
        {
            undiscovered2.SetActive(true);
        }
    }

    private void UpdatePageFlipCooldown()
    {
        if (pageFlipCooldownTimer > 0f)
        {
            pageFlipCooldownTimer -= Time.fixedDeltaTime;
        }
    }
}
