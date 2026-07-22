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
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yRotation = transform.eulerAngles.y;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (Cursor.lockState != CursorLockMode.Locked)
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
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        transform.rotation = Quaternion.Euler(
            0f,
            yRotation,
            0f
        );

        cameraTransform.localRotation = Quaternion.Euler(
            xRotation,
            0f,
            0f
        );
    }
}