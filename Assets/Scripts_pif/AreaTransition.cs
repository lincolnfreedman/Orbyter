using System;
using UnityEngine;

public class AreaTransition : MonoBehaviour
{
    [SerializeField]
    private GameObject[] backgrounds;
    [SerializeField]
    private AudioClip[] bgms;

    public AudioSource musicPlayer;

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
                musicPlayer.resource = bgms[0];
                musicPlayer.Play();
                break;
        }
    }

    public enum Area
    {
        Forest,
        Burrows
    } 
}
