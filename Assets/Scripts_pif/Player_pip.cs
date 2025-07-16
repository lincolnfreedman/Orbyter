using UnityEngine;

public class Player_pip : MonoBehaviour
{
    [SerializeField]
    private AudioClip[] sfx;

    [SerializeField]
    private AudioSource audioSource;
    
    [SerializeField]
    private AudioSource oneShotAudioSource; // Dedicated audio source for one-shot sounds

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        // Create a dedicated AudioSource for one-shot sounds if not assigned
        if (oneShotAudioSource == null)
        {
            oneShotAudioSource = gameObject.AddComponent<AudioSource>();
            // Copy settings from main audio source
            if (audioSource != null)
            {
                oneShotAudioSource.volume = audioSource.volume;
                oneShotAudioSource.pitch = audioSource.pitch;
                oneShotAudioSource.spatialBlend = audioSource.spatialBlend;
                // Don't copy clip or loop settings
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayerSFX(int id)
    {
        Debug.Log($"PlayerSFX called with id: {id}");
        
        if (sfx == null || sfx.Length <= id || sfx[id] == null)
        {
            Debug.LogError($"SFX array is null, too short, or clip at index {id} is null");
            return;
        }
        
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is null!");
            return;
        }
        
        audioSource.clip = sfx[id];
        
        // Enable looping for glide sound effect (index 3) and dig phase sound effect (index 5)
        if (id == 3 || id == 5)
        {
            audioSource.loop = true;
            Debug.Log($"Set looping to true for SFX id: {id}");
        }
        else
        {
            audioSource.loop = false;
            Debug.Log($"Set looping to false for SFX id: {id}");
        }
        
        audioSource.Play();
        Debug.Log($"Audio clip '{sfx[id].name}' started playing");
    }
    
    public void PlayerSFXOneShot(int id)
    {
        Debug.Log($"PlayerSFXOneShot called with id: {id}");
        
        if (sfx == null || sfx.Length <= id || sfx[id] == null)
        {
            Debug.LogError($"SFX array is null, too short, or clip at index {id} is null");
            return;
        }
        
        if (oneShotAudioSource == null)
        {
            Debug.LogError("One-shot AudioSource is null!");
            return;
        }
        
        // Use dedicated one-shot audio source - this will NEVER be interrupted
        oneShotAudioSource.PlayOneShot(sfx[id]);
        Debug.Log($"One-shot audio clip '{sfx[id].name}' started playing on dedicated audio source");
    }
    
    public void StopSFX()
    {
        // Only stop the main audio source, never touch the one-shot audio source
        audioSource.Stop();
        audioSource.loop = false;
        Debug.Log("Main AudioSource stopped - one-shot sounds unaffected");
    }
}
