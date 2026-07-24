using UnityEngine;

public class RecordingMenu : MonoBehaviour
{
    [SerializeField] private GameObject recordingPanel;

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    private bool isOpen;

    private void Start()
    {
        isOpen = false;
        recordingPanel.SetActive(false);

        // Cursor stays visible during normal gameplay.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ToggleRecording()
    {
        isOpen = !isOpen;

        recordingPanel.SetActive(isOpen);

        if (playerMovement != null)
        {
            playerMovement.enabled = !isOpen;
        }

        if (mouseLook != null)
        {
            mouseLook.enabled = !isOpen;
        }

        // Always keep the cursor visible.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = isOpen ? 0f : 1f;
    }

    public void CloseRecording()
    {
        isOpen = false;

        recordingPanel.SetActive(false);

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        if (mouseLook != null)
        {
            mouseLook.enabled = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 1f;
    }
}