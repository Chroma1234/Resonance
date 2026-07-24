using UnityEngine;
using UnityEngine.InputSystem;

public class RecordingMenu : MonoBehaviour
{
    [SerializeField] private GameObject recordingPanel;

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    private bool isOpen;

    private void Start()
    {
        recordingPanel.SetActive(false);
    }

    public void ToggleRecording()
    {
        isOpen = !isOpen;

        recordingPanel.SetActive(isOpen);

        playerMovement.enabled = !isOpen;
        mouseLook.enabled = !isOpen;

        Cursor.lockState = isOpen
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = isOpen;

        Time.timeScale = isOpen ? 0f : 1f;
    }
}