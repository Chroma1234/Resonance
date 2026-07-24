using UnityEngine;
public class PauseMenuToggle : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    public void TogglePauseMenu()
    {
        if (pausePanel != null)
        {
            bool isActive = pausePanel.activeSelf;
            pausePanel.SetActive(!isActive);

            // Optional: Pause or unpause time when menu opens/closes
            Time.timeScale = isActive ? 1f : 0f;
        }
    }
}