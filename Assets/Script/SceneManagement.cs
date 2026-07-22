using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneManagement : MonoBehaviour
{
    [SerializeField] private int gameplaySceneIndex = 1;
    [SerializeField] private int moodSceneIndex = 0;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
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
