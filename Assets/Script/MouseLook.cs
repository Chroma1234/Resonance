using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    public float sensitivity = 100f;
    public Transform cameraTransform;

    private float xRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Vector2 mouse = Mouse.current.delta.ReadValue();

        if (mouse.sqrMagnitude < 0.01f)
            return;

        mouse *= sensitivity * Time.deltaTime;

        xRotation -= mouse.y;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouse.x);
    }
}