using System;
using UnityEngine;

public class AreaTransition : MonoBehaviour
{
    [SerializeField]
    private GameObject[] backgrounds;
    [SerializeField]
    private GameObject[] forestBgs;
    [SerializeField]
    private AudioClip[] bgms;

    public AudioSource musicPlayer;

    [Header("Area Triggers")]
    public Collider2D forestTrigger;
    public Collider2D burrowsTrigger;

    private bool isForestRevitalized = false;
    public Area currentArea = Area.Forest;

    private void Awake()
    {
        // Ensure triggers are set to be triggers
        if (forestTrigger != null) forestTrigger.isTrigger = true;
        if (burrowsTrigger != null) burrowsTrigger.isTrigger = true;
    }

    private void Update()
    {
        // Check for player in forest trigger
        if (forestTrigger != null && IsPlayerInTrigger(forestTrigger))
        {
            if (currentArea != Area.Forest)
            {
                Debug.Log("Entered Forest Area");
                ChangeToForest();
            }
        }

        // Check for player in burrows trigger
        if (burrowsTrigger != null && IsPlayerInTrigger(burrowsTrigger))
        {
            Debug.Log("Entered Burrows Area1 ");
            if (currentArea != Area.Burrows)
            {
                Debug.Log("Entered Burrows Area 2");
                ChangeToBurrows();
            }
        }
    }

    private bool IsPlayerInTrigger(Collider2D trigger)
    {
        // Use OverlapBox for BoxCollider2D, respecting rotation
        if (trigger is BoxCollider2D box)
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(
                box.bounds.center,
                box.size * box.transform.lossyScale,
                box.transform.eulerAngles.z,
                ~0 // all layers
            );
            foreach (var hit in hits)
            {
                if (hit != null && hit.CompareTag("Player"))
                    return true;
            }
            return false;
        }
        // Fallback for other collider types
        Collider2D[] fallbackHits = Physics2D.OverlapAreaAll(trigger.bounds.min, trigger.bounds.max);
        foreach (var hit in fallbackHits)
        {
            if (hit != null && hit.CompareTag("Player"))
                return true;
        }
        return false;
    }

    private void ChangeToForest()
    {
        backgrounds[1].SetActive(false);
        backgrounds[0].SetActive(true);
        currentArea = Area.Forest;
        ChangeBGMForForest();
    }

    private void ChangeToBurrows()
    {
        backgrounds[0].SetActive(false);
        backgrounds[1].SetActive(true);
        currentArea = Area.Burrows;
        ChangeBGMForBurrows();
    }

    private void ChangeBGMForForest()
    {
        if (isForestRevitalized)
        {
            musicPlayer.clip = bgms[2];
            musicPlayer.Play();
        }
        else
        {
            musicPlayer.clip = bgms[0];
            musicPlayer.Play();
        }
    }

    private void ChangeBGMForBurrows()
    {
        musicPlayer.clip = bgms[1];
        musicPlayer.Play();
    }

    public void CleanseForest()
    {
        forestBgs[0].SetActive(false);
        forestBgs[1].SetActive(true);
        musicPlayer.resource = bgms[2];
        musicPlayer.Play();
        isForestRevitalized = true;

        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.forestCleansed = true;
        }
    }

    public enum Area
    {
        Forest,
        Burrows
    }
}
