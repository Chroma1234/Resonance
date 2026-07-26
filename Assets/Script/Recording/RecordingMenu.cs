using TMPro;
using UnityEngine;

public class RecordingMenu : MonoBehaviour
{
    [Header("Recording Panel")]
    [SerializeField] private GameObject recordingPanel;

    [Header("Recording Status UI")]
    [SerializeField] private GameObject recordingIndicatorUI; // The container or text object
    [SerializeField] private TMP_Text recordingIndicatorText; // Drag your TextMeshPro component here

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


    /// Shows only the small recording indicator (e.g., a red dot or icon) 
    /// without opening the full recording panel/canvas group.
    public void ShowRecordingIndicatorOnly()
    {
        if (recordingIndicatorText != null)
        {
            recordingIndicatorText.text = "Recording in progress...";
            recordingIndicatorText.gameObject.SetActive(true);
        }

        if (recordingIndicatorUI != null)
        {
            recordingIndicatorUI.SetActive(true);
        }
    }

 
    /// Hides only the small recording indicator.
    public void HideRecordingIndicatorOnly()
    {
        if (recordingIndicatorText != null)
        {
            recordingIndicatorText.gameObject.SetActive(false);
        }

        if (recordingIndicatorUI != null)
        {
            recordingIndicatorUI.SetActive(false);
        }
    }

    
    /// Changes the recording indicator text to show that the file was saved.
    public void ShowRecordingSaved()
    {
        // Make sure the indicator UI and text are active
        if (recordingIndicatorUI != null)
        {
            recordingIndicatorUI.SetActive(true);
        }

        if (recordingIndicatorText != null)
        {
            recordingIndicatorText.text = "Recording is Saved!";
            recordingIndicatorText.gameObject.SetActive(true);
        }
    }
    public void ShowAndHideRecordingSaved(float displayDuration = 3f)
    {
        // Show the saved message
        ShowRecordingSaved();

        // Start a coroutine to hide it after the duration
        StartCoroutine(HideIndicatorAfterDelay(displayDuration));
    }

    private System.Collections.IEnumerator HideIndicatorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Hide the indicator completely
        HideRecordingIndicatorOnly();
    }

   
    /// Opens the full recording panel AND shows the indicator.
    public void ShowFullRecordingPanel()
    {
        if (recordingCanvasGroup != null)
        {
            recordingCanvasGroup.alpha = 1f;
            recordingCanvasGroup.interactable = true;
            recordingCanvasGroup.blocksRaycasts = true;
        }

        // Call the indicator method as well since the full panel is open
        ShowRecordingIndicatorOnly();
    }

    /// <summary>
    /// Hides the full recording panel.
    /// </summary>
    public void HideFullRecordingPanel()
    {
        if (recordingCanvasGroup != null)
        {
            recordingCanvasGroup.alpha = 0f;
            recordingCanvasGroup.interactable = false;
            recordingCanvasGroup.blocksRaycasts = false;
        }
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

        //ShowRecordingIndicatorOnly();
        //ShowRecordingSaved();
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

        // Turn off the indicator when the panel is closed
        if (recordingIndicatorUI != null)
        {
            recordingIndicatorUI.SetActive(false);
        }
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

    // Example of how you call it when saving is complete:
    public void OnSaveComplete()
    {
        // This will change the text to "Recording is Saved!" for 3 seconds, then hide it automatically
        ShowAndHideRecordingSaved(3f);
    }
}