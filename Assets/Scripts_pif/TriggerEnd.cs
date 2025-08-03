using UnityEngine;

public class TriggerEnd : MonoBehaviour
{
    [Header("References")]
    public GameObject cutscene;
    public GameObject credits;
    public GameObject badEndImage;

    private Animator cutsceneAnimator;
    private bool cutscenePlaying = false;
    private bool creditsShown = false;
    private bool endingTriggered = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (cutscene != null)
        {
            cutsceneAnimator = cutscene.GetComponent<Animator>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the colliding object has the Player tag
        if (other.CompareTag("Player") && !endingTriggered)
        {
            endingTriggered = true;
            
            // Disable player movement permanently
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.IsCutscenePlaying = true;
            }
            var playerController = other.GetComponent<PlayerController_pif>();
            if (playerController != null)
            {
                playerController.DisableMovement();
            }

            if (gm != null && gm.forestCleansed)
            {
                // Good ending
                if (cutscene != null && !cutscenePlaying)
                {
                    cutscene.SetActive(true);
                    cutscenePlaying = true;
                }
            }
            else
            {
                // Bad ending
                if (badEndImage != null)
                {
                    badEndImage.SetActive(true);
                    StartCoroutine(ShowBadEndThenCredits());
                }
            }
        }
    }

    private System.Collections.IEnumerator ShowBadEndThenCredits()
    {
        yield return new WaitForSeconds(7f);
        if (badEndImage != null)
            badEndImage.SetActive(false);
        if (credits != null)
        {
            credits.SetActive(true);
            creditsShown = true;
            // Start the credits timer for bad ending
            StartCoroutine(WaitForCreditsToFinish(true)); // true = bad ending
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (cutscenePlaying && cutsceneAnimator != null)
        {
            // Check if the cutscene animation is done
            if (cutsceneAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f &&
                !cutsceneAnimator.IsInTransition(0))
            {
                cutscene.SetActive(false);
                if (credits != null)
                {
                    credits.SetActive(true);
                    creditsShown = true;
                    // Start the credits timer for good ending
                    StartCoroutine(WaitForCreditsToFinish(false)); // false = good ending
                }
                cutscenePlaying = false;
            }
        }
    }

    /// <summary>
    /// Waits 15 seconds after credits start, then loads main menu with appropriate try again message setting
    /// </summary>
    /// <param name="isBadEnding">True if bad ending was reached</param>
    private System.Collections.IEnumerator WaitForCreditsToFinish(bool isBadEnding)
    {
        // Wait 15 seconds while keeping everything disabled
        yield return new WaitForSeconds(15f);
        
        // Re-enable all game functionality immediately before loading main menu
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            // Re-enable cutscene state (allows adventure log and menu to be opened)
            gm.IsCutscenePlaying = false;
            
            
            // Show try again message for bad ending, hide it for good ending
            gm.LoadMainMenuWithTryAgainControl(isBadEnding);
        }
        else
        {
            Debug.LogWarning("TriggerEnd: GameManager not found, loading main menu normally");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}
