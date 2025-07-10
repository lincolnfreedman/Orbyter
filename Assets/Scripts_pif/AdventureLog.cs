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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            UpdatePages();
        }
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
