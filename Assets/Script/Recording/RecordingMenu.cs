using UnityEngine;

public class RecordingMenu : MonoBehaviour
{
    [Header("Recording Panel")]
    [SerializeField] private GameObject recordingPanel;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    private CanvasGroup recordingCanvasGroup;
    private bool isOpen;

    private void Awake()
    {
        isOpen = false;

        if (recordingPanel == null)
        {
            Debug.LogError("Recording Panel is not assigned.");
            return;
        }

        // Keep the panel active so WAV playback continues.
        recordingPanel.SetActive(true);

        // Find the Canvas Group on the panel.
        recordingCanvasGroup =
            recordingPanel.GetComponent<CanvasGroup>();

        if (recordingCanvasGroup == null)
        {
            recordingCanvasGroup =
                recordingPanel.AddComponent<CanvasGroup>();
        }

        HidePanel();
    }

    private void Start()
    {
        UpdatePlayerControls();
    }

    public void ToggleRecording()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            ShowPanel();
        }
        else
        {
            HidePanel();
        }

        UpdatePlayerControls();
    }

    private void ShowPanel()
    {
        if (recordingCanvasGroup == null)
        {
            return;
        }

        recordingCanvasGroup.alpha = 1f;
        recordingCanvasGroup.interactable = true;
        recordingCanvasGroup.blocksRaycasts = true;
    }

    private void HidePanel()
    {
        if (recordingCanvasGroup == null)
        {
            return;
        }

        recordingCanvasGroup.alpha = 0f;
        recordingCanvasGroup.interactable = false;
        recordingCanvasGroup.blocksRaycasts = false;
    }

    private void UpdatePlayerControls()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = !isOpen;
        }

        if (mouseLook != null)
        {
            mouseLook.enabled = !isOpen;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = isOpen ? 0f : 1f;
    }
}