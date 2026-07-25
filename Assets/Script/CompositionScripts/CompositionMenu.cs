using UnityEngine;
using UnityEngine.InputSystem;

public class CompositionMenu : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    private GameObject currentPanel;

    public void OpenComposition(GameObject panel)
    {
        currentPanel = panel;

        panel.SetActive(true);

        playerMovement.enabled = false;
        mouseLook.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (currentPanel == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame ||
            Keyboard.current.xKey.wasPressedThisFrame)
        {
            CloseComposition();
        }
    }

    public void CloseComposition()
    {
        currentPanel.SetActive(false);

        currentPanel = null;

        playerMovement.enabled = true;
        mouseLook.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Time.timeScale = 1f;
    }
}