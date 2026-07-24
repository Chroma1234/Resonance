using UnityEngine;
using UnityEngine.InputSystem;

public class CompositionMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject compositionPanel;
    [SerializeField] private GameObject recordingPanel;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    [Header("Composition")]
    [SerializeField] private CompositionPlayer compositionPlayer;

    private bool compositionOpen;
    private bool recordingOpen;

    private void Start()
    {
        compositionPanel.SetActive(false);
        recordingPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // E = Composition Menu
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            SetCompositionMenu(!compositionOpen);
        }

        // R = Saved Recordings
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            SetRecordingMenu(!recordingOpen);
        }
    }

    public void OpenCompositionMenu()
    {
        SetCompositionMenu(true);
    }

    public void CloseCompositionMenu()
    {
        SetCompositionMenu(false);
    }

    public void OpenRecordingMenu()
    {
        SetRecordingMenu(true);
    }

    public void CloseRecordingMenu()
    {
        SetRecordingMenu(false);
    }

    private void SetCompositionMenu(bool open)
    {
        compositionOpen = open;

        if (!open && compositionPlayer != null)
        {
            compositionPlayer.ClearComposition();
        }

        compositionPanel.SetActive(open);

        // Close the recording panel if composition opens
        if (open)
        {
            recordingOpen = false;
            recordingPanel.SetActive(false);
        }

        UpdatePlayerState();
    }

    private void SetRecordingMenu(bool open)
    {
        recordingOpen = open;

        recordingPanel.SetActive(open);

        // Close the composition panel if recordings open
        if (open)
        {
            compositionOpen = false;
            compositionPanel.SetActive(false);
        }

        UpdatePlayerState();
    }

    private void UpdatePlayerState()
    {
        bool menuOpen = compositionOpen || recordingOpen;

        playerMovement.enabled = !menuOpen;
        mouseLook.enabled = !menuOpen;

        Time.timeScale = menuOpen ? 0f : 1f;

        Cursor.lockState = menuOpen
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = menuOpen;
    }
}