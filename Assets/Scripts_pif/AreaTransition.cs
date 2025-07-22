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

    private bool isForestRevitalized = false;

    private Area currentArea = Area.Forest;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Entered Area");
        ChangeBGM(); 
        ChangeBackground();
    }

    private void ChangeBackground()
    {
        switch (currentArea)
        {
            case Area.Forest: // Exiting Forest
                backgrounds[0].SetActive(false);
                backgrounds[1].SetActive(true);
                currentArea = Area.Burrows;
                break;
            case Area.Burrows: // Exiting Burrows
                backgrounds[1].SetActive(false);
                backgrounds[0].SetActive(true);
                currentArea = Area.Forest;
                break;
        }
    }

    private void ChangeBGM()
    {
        switch (currentArea)
        {
            case Area.Forest: // Exiting Forest
                musicPlayer.resource = bgms[1];
                musicPlayer.Play();
                break;
            case Area.Burrows: // Exiting Burrows
                if (isForestRevitalized)
                {
                    musicPlayer.resource = bgms[2];
                    musicPlayer.Play();
                }
                else
                {
                    musicPlayer.resource = bgms[0];
                    musicPlayer.Play();
                }
                break;
        }
    }

    public void CleanseForest()
    {
        forestBgs[0].SetActive(false);
        forestBgs[1].SetActive(true);
        musicPlayer.resource = bgms[2];
        musicPlayer.Play();
        isForestRevitalized = true;
    }

    public enum Area
    {
        Forest,
        Burrows
    } 
}
