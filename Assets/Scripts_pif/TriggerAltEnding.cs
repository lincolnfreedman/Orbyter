using UnityEngine;
using System.Linq;

public class TriggerAltEnding : MonoBehaviour
{
    private AreaTransition areaTransition;

    [Header("Ship Fires")]
    public GameObject[] shipFires = new GameObject[4];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        areaTransition = GameObject.Find("AreaTransition").GetComponent<AreaTransition>();
    }

    void Update()
    {
        // Efficiently check if all ship fires have been destroyed (null)
        if (shipFires.All(fire => fire == null))
        {
            Debug.Log("Revitalized Forest");
            areaTransition.CleanseForest();
            enabled = false; // Prevent repeated calls
        }
    }
}
