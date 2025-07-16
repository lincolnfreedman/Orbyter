using UnityEngine;
using UnityEngine.UI;

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
    private int currentPage;
    [SerializeField]
    private int lastPage;
    
    private Player_pip player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find the player to access SFX system
        player = FindFirstObjectByType<Player_pip>();
        if (player == null)
        {
            Debug.LogWarning("Player_pip not found! Adventure log SFX will not work.");
        }
        
        currentPage = 1;
        lastPage = pages.Length/2;
        UpdatePages();
    }

    // Update is called once per frame
    void Update()
    {
        
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
}
