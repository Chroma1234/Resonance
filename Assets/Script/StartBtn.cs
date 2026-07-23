using UnityEngine;

public class StartBtn : MonoBehaviour
{
    public void OpenGameplay()
    {
        //SceneManagement.Instance.OpenGameplay();
        if (SceneManagement.Instance != null)
        {
            SceneManagement.Instance.OpenGameplay();
        }
        else
        {
            Debug.LogError("SceneManagement instance is missing from the scene!", this);
        }
    }
}
