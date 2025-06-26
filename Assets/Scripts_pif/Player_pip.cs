using UnityEngine;

public class Player_pip : MonoBehaviour
{
    [SerializeField]
    private AudioClip[] sfx;

    [SerializeField]
    private AudioSource audioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayerSFX(int id)
    {
        audioSource.clip = sfx[id];
        audioSource.Play();
    }
}
