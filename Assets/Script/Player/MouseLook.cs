using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private float sensitivity = 0.08f;
    [SerializeField] private float maxMouseDelta = 50f;
    [SerializeField] private Transform cameraTransform;

    private float xRotation;
    private float yRotation;

    private void Start()
    {
        yRotation = transform.eulerAngles.y;
        xRotation = cameraTransform.localEulerAngles.x;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        // Hold right-click to rotate the camera.
        if (!Mouse.current.rightButton.isPressed)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

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

        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity;

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(
            xRotation,
            -80f,
            80f
        );

        transform.rotation =
            Quaternion.Euler(
                0f,
                yRotation,
                0f
            );

        cameraTransform.localRotation =
            Quaternion.Euler(
                xRotation,
                0f,
                0f
            );
    }
}