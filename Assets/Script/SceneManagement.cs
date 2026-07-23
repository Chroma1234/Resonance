using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneManagement : MonoBehaviour
{
    public static SceneManagement Instance { get; private set; }

    [SerializeField] private int gameplaySceneIndex = 1;
    [SerializeField] private int moodSceneIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OpenMood()
    {
        SceneManager.LoadScene(moodSceneIndex);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenGameplay()
    {
        SceneManager.LoadScene(gameplaySceneIndex);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Exit()
    {
        Application.Quit();
    }

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (SceneManager.GetActiveScene().buildIndex == gameplaySceneIndex)
            {
                OpenMood();
            }
        }
    }
}
