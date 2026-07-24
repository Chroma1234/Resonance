using UnityEngine;

public class CompositionUIManager : MonoBehaviour
{
    [SerializeField] private GameObject composeButton;

    private GameObject selectedPanel;
    private bool playerNearby;

    private void Start()
    {
        composeButton.SetActive(false);
    }

    public void EnterInstrument(GameObject panel)
    {
        selectedPanel = panel;
        playerNearby = true;

        composeButton.SetActive(true);
    }

    public void ExitInstrument(GameObject panel)
    {
        // Prevent another trigger from hiding the button.
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
    }

    public void ClosePanel(GameObject panel)
    {
        panel.SetActive(false);

        if (playerNearby)
        {
            composeButton.SetActive(true);
        }
    }
}