using UnityEngine;
using UnityEngine.InputSystem;

public class CompositionMenu : MonoBehaviour
{
    [SerializeField] private GameObject compositionPanel;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private CompositionPlayer compositionPlayer;

    private bool isOpen;

    private void Start()
    {
        SetMenu(false);
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            SetMenu(!isOpen);
        }
    }

    public void OpenMenu()
    {
        SetMenu(true);
    }

    public void CloseMenu()
    {
        SetMenu(false);
    }

    private void SetMenu(bool open)
    {
        isOpen = open;

        if (!open && compositionPlayer != null)
        {
            compositionPlayer.ClearComposition();
        }

        compositionPanel.SetActive(open);
        playerMovement.enabled = !open;
        mouseLook.enabled = !open;

        Time.timeScale = open ? 0f : 1f;

        Cursor.lockState = open
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Cursor.visible = open;
    }
}