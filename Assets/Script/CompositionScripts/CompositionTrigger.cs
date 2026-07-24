using UnityEngine;
using UnityEngine.InputSystem;

public class CompositionTrigger : MonoBehaviour
{
    [Header("Composition Panel")]
    [SerializeField] private GameObject compositionPanel;

    [Header("Prompt")]
    [SerializeField] private GameObject pressEText;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    private bool playerInside;
    private bool panelOpen;

    private void Start()
    {
        // Hide the composition panel when the game begins.
        if (compositionPanel != null)
        {
            compositionPanel.SetActive(false);
        }

        if (pressEText != null)
        {
            pressEText.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        // Press E while inside the trigger to open or close the panel.
        if (playerInside &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (panelOpen)
            {
                ClosePanel();
            }
            else
            {
                OpenPanel();
            }
        }

        // Press X to close the panel.
        if (panelOpen &&
            Keyboard.current.xKey.wasPressedThisFrame)
        {
            ClosePanel();
        }
    }

    private void OpenPanel()
    {
        if (compositionPanel == null)
        {
            Debug.LogError("Composition Panel is not assigned.");
            return;
        }

        panelOpen = true;
        compositionPanel.SetActive(true);

        if (pressEText != null)
        {
            pressEText.SetActive(false);
        }

        SetPlayerEnabled(false);
    }

    private void ClosePanel()
    {
        panelOpen = false;

        if (compositionPanel != null)
        {
            compositionPanel.SetActive(false);
        }

        if (pressEText != null)
        {
            pressEText.SetActive(playerInside);
        }

        SetPlayerEnabled(true);
    }

    private void SetPlayerEnabled(bool enabled)
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = enabled;
        }

        if (mouseLook != null)
        {
            mouseLook.enabled = enabled;
        }

        Time.timeScale = enabled ? 1f : 0f;

        Cursor.lockState = enabled
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !enabled;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInside = true;

        if (!panelOpen && pressEText != null)
        {
            pressEText.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInside = false;

        if (pressEText != null)
        {
            pressEText.SetActive(false);
        }

        if (panelOpen)
        {
            ClosePanel();
        }
    }
}