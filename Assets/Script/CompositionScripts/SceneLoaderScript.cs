using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoaderScript : MonoBehaviour
{
    public void LoadEnvironment()
    {
        SceneManager.LoadScene("Environment");
    }

    public void LoadComposition()
    {
        SceneManager.LoadScene("SceneComposition");
    }
}