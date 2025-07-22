using UnityEngine;

public class TriggerEnd : MonoBehaviour
{
    [Header("References")]
    public GameObject cutscene;
    public GameObject credits;
    public GameObject badEndImage;

    private Animator cutsceneAnimator;
    private bool cutscenePlaying = false;

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
        if (other.CompareTag("Player"))
        {
            var gm = FindFirstObjectByType<GameManager>();
            if (gm != null && gm.forestCleansed)
            {
                if (cutscene != null && !cutscenePlaying)
                {
                    cutscene.SetActive(true);
                    cutscenePlaying = true;
                }
            }
            else
            {
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
            credits.SetActive(true);
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
                }
                cutscenePlaying = false;
            }
        }
    }
}
