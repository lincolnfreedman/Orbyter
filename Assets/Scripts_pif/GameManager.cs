using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private bool paused = false;

    [SerializeField]
    private GameObject adventureLog;
    public bool[] pagesUnlocked;

    [SerializeField]
    private PlayerInput playerInput;
    private InputAction pause;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
        }

        DontDestroyOnLoad(this);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pause = playerInput.actions["Pause"];
    }

    // Update is called once per frame
    void Update()
    {
        if (pause.triggered)
        {
            if (paused)
            {
                paused = false;
                adventureLog.SetActive(false);
                Time.timeScale = 1f;
            }
            else if (!paused)
            {
                paused = true;
                adventureLog.SetActive(true);
                Time.timeScale = 0f;
            }
        }
    }
}
