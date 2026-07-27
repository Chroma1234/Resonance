using UnityEngine;
using UnityEngine.InputSystem;

// This script controls the player's camera.
//
// Responsibilities:
// - Read mouse movement.
// - Rotate the player horizontally.
// - Rotate the camera vertically.
// - Limit the camera angle.
// - Rotate only while holding the right mouse button.
public class MouseLook : MonoBehaviour
{
    // Controls how sensitive the mouse movement feels.
    [SerializeField] private float sensitivity = 0.08f;

    // Limits extremely large mouse movements
    // to prevent sudden camera jumps.
    [SerializeField] private float maxMouseDelta = 50f;

    // Reference to the player's camera.
    // The camera rotates vertically,
    // while the player rotates horizontally.
    [SerializeField] private Transform cameraTransform;

    // Stores the current vertical (X) rotation
    // and horizontal (Y) rotation.
    private float xRotation;
    private float yRotation;

    private void Start()
    {
        // Read the player's current horizontal rotation.
        yRotation = transform.eulerAngles.y;

        // Read the camera's current vertical rotation.
        // This prevents the camera from snapping
        // back to zero when the game starts.
        xRotation = cameraTransform.localEulerAngles.x;

        // Keep the cursor visible for UI interaction.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        // Stop if no mouse is connected.
        if (Mouse.current == null)
            return;

        // Only rotate the camera while
        // the player is holding the right mouse button.
        if (!Mouse.current.rightButton.isPressed)
            return;

        // Read how far the mouse moved
        // since the previous frame.
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // Clamp the mouse movement.
        // This prevents sudden spikes from causing
        // extremely fast camera rotation.
        mouseDelta.x = Mathf.Clamp(
            mouseDelta.x,
            -maxMouseDelta,
            maxMouseDelta
        );

        mouseDelta.y = Mathf.Clamp(
            mouseDelta.y,
            -maxMouseDelta,
            maxMouseDelta
        );

        // Apply the mouse sensitivity.
        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity;

        // Horizontal rotation (left and right)
        // is applied to the player object.
        yRotation += mouseX;

        // Vertical rotation (up and down)
        // is applied only to the camera.
        xRotation -= mouseY;

        // Limit the vertical camera angle.
        // This prevents the player from
        // looking too far up or down.
        xRotation = Mathf.Clamp(
            xRotation,
            -80f,
            80f
        );

        // Rotate the player left and right.
        transform.rotation =
            Quaternion.Euler(
                0f,
                yRotation,
                0f
            );

        // Rotate the camera up and down.
        // Using localRotation means only the camera tilts,
        // while the player's body stays upright.
        cameraTransform.localRotation =
            Quaternion.Euler(
                xRotation,
                0f,
                0f
            );
    }
}