using UnityEngine;

public class TriggerAltEnding : MonoBehaviour
{
    private AreaTransition areaTransition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        areaTransition = GameObject.Find("AreaTransition").GetComponent<AreaTransition>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Revitalized Forest");
        areaTransition.CleanseForest();
    }
}
