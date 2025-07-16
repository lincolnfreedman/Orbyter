using UnityEngine;

public class CameraController_pip : MonoBehaviour
{
    [SerializeField] private PlayerController_pif playerController;
    private int cameraFacingDirection = 1; // 1 for right, -1 for left
    [SerializeField] private float biasLerpSpeed = 1f; // How quickly to transition to/from biased position
    private Transform playerTransform;

    void Start()
    {
        playerTransform = playerController.gameObject.transform;
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = playerTransform.position;
        TurnCheck();
    }
    private void TurnCamera(int direction)
    {
        if (direction == 1)
        {
            LeanTween.rotateY(gameObject, 0, biasLerpSpeed).setEaseOutSine();
        }
        else
        {
            LeanTween.rotateY(gameObject, 180, biasLerpSpeed).setEaseOutSine();
        }
        Debug.Log($"Camera turned to direction: {direction}");
    }
    private void TurnCheck()
    {
        if (cameraFacingDirection != playerController.facingDirection)
        {
            cameraFacingDirection *= -1;
            TurnCamera(cameraFacingDirection);
        }
    }
}
