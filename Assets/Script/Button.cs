using UnityEngine;

public class Button : MonoBehaviour
{
    public void OpenMood()
    {
        if (SceneManagement.Instance != null)
        {
            SceneManagement.Instance.OpenMood();
        }
        else
        {
            Debug.LogError("SceneManagement instance is missing from the scene!", this);
        }
    }

    public void OpenGameplay()
    {
        if (SceneManagement.Instance != null)
        {
            SceneManagement.Instance.OpenGameplay();
        }
        else
        {
            Debug.LogError("SceneManagement instance is missing from the scene!", this);
        }
    }

    public void Quit()
    {
        if (SceneManagement.Instance != null)
        {
            SceneManagement.Instance.Exit();
        }
        else
        {
            Debug.LogError("SceneManagement instance is missing from the scene!", this);
        }
    }
}
