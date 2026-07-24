using UnityEngine;

public class CompositionUIManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject composeButton;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    private GameObject selectedPanel;
    private bool playerNearby;

    private void Start()
    {
        composeButton.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void EnterInstrument(GameObject panel)
    {
        selectedPanel = panel;
        playerNearby = true;

        composeButton.SetActive(true);
    }

    public void ExitInstrument(GameObject panel)
    {
        if (selectedPanel != panel)
            return;

        playerNearby = false;
        selectedPanel = null;

        composeButton.SetActive(false);
    }

    public void OpenSelectedPanel()
    {
        if (selectedPanel == null)
            return;

        selectedPanel.SetActive(true);
        composeButton.SetActive(false);

        SetPlayerControls(false);

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseSelectedPanel()
    {
        if (selectedPanel != null)
        {
            selectedPanel.SetActive(false);
        }

        SetPlayerControls(true);

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playerNearby)
        {
            composeButton.SetActive(true);
        }
    }

    private void SetPlayerControls(bool enabled)
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = enabled;
        }

        if (mouseLook != null)
        {
            mouseLook.enabled = enabled;
        }
    }
}