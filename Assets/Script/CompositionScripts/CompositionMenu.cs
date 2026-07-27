using UnityEngine;
using UnityEngine.InputSystem;

// This script controls opening and closing
// of the composition menu.
//
// Responsibilities:
// 1. Opens the selected composition panel.
// 2. Disables player movement while composing.
// 3. Unlocks the mouse cursor for UI interaction.
// 4. Pauses the game.
// 5. Closes the panel and restores gameplay.
public class CompositionMenu : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;

    // Controls camera rotation.
    // This is disabled while the composition menu is open
    // so the player cannot look around.
    [SerializeField] private MouseLook mouseLook;

    // Stores whichever composition panel is currently open.
    // This allows the same script to work with multiple
    // instrument panels instead of creating one script
    // for every instrument.
    private GameObject currentPanel;

    // Called when the player interacts with an instrument.
    // The instrument passes its composition panel into this function.
    public void OpenComposition(GameObject panel)
    {
        // Remember which panel is currently open.
        currentPanel = panel;

        // Display the composition UI.
        panel.SetActive(true);

        // Disable player movement so the player
        // cannot walk while composing.
        playerMovement.enabled = false;

        // Disable camera movement.
        mouseLook.enabled = false;

        // Unlock and show the cursor
        // so the player can interact with the UI.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Pause the game.
        // Using Time.timeScale = 0 freezes gameplay
        // while the UI remains usable.
        Time.timeScale = 0f;
    }

    private void Update()
    {
        // If no composition panel is open,
        // there is nothing to close.
        if (currentPanel == null)
            return;

        // Allow the player to close the menu
        // by pressing either E or X.
        if (Keyboard.current.eKey.wasPressedThisFrame ||
            Keyboard.current.xKey.wasPressedThisFrame)
        {
            CloseComposition();
        }
    }

    // Closes whichever composition panel
    // is currently open.
    public void CloseComposition()
    {
        // Hide the UI.
        currentPanel.SetActive(false);

        // Remove the current panel reference.
        // This tells the script that no menu is open.
        currentPanel = null;

        // Re-enable player movement.
        playerMovement.enabled = true;

        // Re-enable camera rotation.
        mouseLook.enabled = true;

        // Lock and hide the cursor
        // so mouse movement controls the camera again.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Resume the game.
        Time.timeScale = 1f;
    }
}