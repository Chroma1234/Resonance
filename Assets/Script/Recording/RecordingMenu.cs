using TMPro;
using UnityEngine;

// This script manages the recording menu.
//
// Responsibilities:
// - Open and close the recording panel.
// - Show recording status messages.
// - Enable and disable player controls.
// - Pause and resume the game while recording.
public class RecordingMenu : MonoBehaviour
{
    [Header("Recording Panel")]

    // Main recording menu panel.
    [SerializeField] private GameObject recordingPanel;

    [Header("Recording Status UI")]

    // Small UI used to show
    // recording progress or save status.
    [SerializeField] private GameObject recordingIndicatorUI;

    // Text displayed inside
    // the recording indicator.
    [SerializeField] private TMP_Text recordingIndicatorText;

    [Header("Player")]

    // References used to disable
    // player controls while
    // the recording menu is open.
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    // Used to smoothly show and hide
    // the recording panel.
    private CanvasGroup recordingCanvasGroup;

    // Tracks whether the
    // recording menu is open.
    private bool isOpen;

    private void Awake()
    {
        // Menu starts closed.
        isOpen = false;

        // Ensure the panel
        // has been assigned.
        if (recordingPanel == null)
        {
            Debug.LogError("Recording Panel is not assigned.");
            return;
        }

        // Keep the panel active so
        // audio playback continues
        // even when hidden.
        recordingPanel.SetActive(true);

        // Get the CanvasGroup
        // used to control visibility.
        recordingCanvasGroup =
            recordingPanel.GetComponent<CanvasGroup>();

        // Automatically add one
        // if it doesn't exist.
        if (recordingCanvasGroup == null)
        {
            recordingCanvasGroup =
                recordingPanel.AddComponent<CanvasGroup>();
        }

        // Hide the panel
        // on startup.
        HidePanel();
    }

    private void Start()
    {
        // Make sure player controls
        // match the menu state.
        UpdatePlayerControls();
    }

    // Opens or closes
    // the recording menu.
    public void ToggleRecording()
    {
        // Switch between open
        // and closed.
        isOpen = !isOpen;

        if (isOpen)
        {
            ShowPanel();
        }
        else
        {
            HidePanel();
        }

        // Update player controls.
        UpdatePlayerControls();
    }

    // Shows only the recording indicator
    // without opening the full menu.
    public void ShowRecordingIndicatorOnly()
    {
        if (recordingIndicatorText != null)
        {
            recordingIndicatorText.text =
                "Recording in progress...";

            recordingIndicatorText
                .gameObject.SetActive(true);
        }

        if (recordingIndicatorUI != null)
        {
            recordingIndicatorUI.SetActive(true);
        }
    }

    // Hides the recording indicator.
    public void HideRecordingIndicatorOnly()
    {
        if (recordingIndicatorText != null)
        {
            recordingIndicatorText
                .gameObject.SetActive(false);
        }

        if (recordingIndicatorUI != null)
        {
            recordingIndicatorUI.SetActive(false);
        }
    }

    // Updates the indicator
    // to show recording completed.
    public void ShowRecordingSaved()
    {
        if (recordingIndicatorUI != null)
        {
            recordingIndicatorUI.SetActive(true);
        }

        if (recordingIndicatorText != null)
        {
            recordingIndicatorText.text =
                "Recording is Saved!";

            recordingIndicatorText
                .gameObject.SetActive(true);
        }
    }

    // Shows the saved message
    // for a short period.
    public void ShowAndHideRecordingSaved(
        float displayDuration = 3f)
    {
        ShowRecordingSaved();

        StartCoroutine(
            HideIndicatorAfterDelay(displayDuration)
        );
    }

    // Waits before hiding
    // the saved message.
    private System.Collections.IEnumerator
        HideIndicatorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        HideRecordingIndicatorOnly();
    }

    // Shows the full recording panel.
    public void ShowFullRecordingPanel()
    {
        if (recordingCanvasGroup != null)
        {
            recordingCanvasGroup.alpha = 1f;
            recordingCanvasGroup.interactable = true;
            recordingCanvasGroup.blocksRaycasts = true;
        }

        // Also display the
        // recording indicator.
        ShowRecordingIndicatorOnly();
    }

    // Hides the full recording panel.
    public void HideFullRecordingPanel()
    {
        if (recordingCanvasGroup != null)
        {
            recordingCanvasGroup.alpha = 0f;
            recordingCanvasGroup.interactable = false;
            recordingCanvasGroup.blocksRaycasts = false;
        }
    }

    // Makes the recording panel visible.
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

    // Hides the recording panel.
    private void HidePanel()
    {
        if (recordingCanvasGroup == null)
        {
            return;
        }

        recordingCanvasGroup.alpha = 0f;
        recordingCanvasGroup.interactable = false;
        recordingCanvasGroup.blocksRaycasts = false;

        // Hide the recording indicator
        // when the menu closes.
        if (recordingIndicatorUI != null)
        {
            recordingIndicatorUI.SetActive(false);
        }
    }

    // Enables or disables
    // player controls depending
    // on whether the menu is open.
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

        // Unlock the cursor
        // while using the menu.
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        // Pause or resume the game.
        Time.timeScale =
            isOpen ? 0f : 1f;
    }

    // Called after a recording
    // has been successfully saved.
    public void OnSaveComplete()
    {
        // Show the saved message
        // for three seconds.
        ShowAndHideRecordingSaved(3f);
    }
}