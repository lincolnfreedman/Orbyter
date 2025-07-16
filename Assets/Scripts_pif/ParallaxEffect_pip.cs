using UnityEngine;

public class ParallaxEffect_pip : MonoBehaviour
{
    private float startPos;
    private float length;
    [SerializeField]
    private Camera mainCam;
    [SerializeField]
    private float parallaxEffect;
    [SerializeField]
    private Vector2 offset = Vector2.zero; // X and Y offset relative to camera

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = gameObject.transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float distance = mainCam.transform.position.x * parallaxEffect;
        float movement = mainCam.transform.position.x * (1 - parallaxEffect);

        transform.position = new Vector3(
            startPos + distance + offset.x, 
            mainCam.transform.position.y + offset.y, 
            transform.position.z
        );

        if (movement > startPos + length)
        {
            startPos += length;
        }
        else if (movement < startPos - length)
        {
            startPos -= length;
        }
    }
}
