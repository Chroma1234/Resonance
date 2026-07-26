using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody rb;
    private Vector3 movement;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Prevent physics from rotating the player by itself.
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;

        rb.angularVelocity = Vector3.zero;
    }

    private void Update()
    {
        movement = Vector3.zero;

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.wKey.isPressed)
            movement += transform.forward;

        if (Keyboard.current.sKey.isPressed)
            movement -= transform.forward;

        if (Keyboard.current.aKey.isPressed)
            movement -= transform.right;

        if (Keyboard.current.dKey.isPressed)
            movement += transform.right;

        movement.y = 0f;
        movement = movement.normalized;
    }

    private void FixedUpdate()
    {
        // Remove any unwanted physics rotation.
        rb.angularVelocity = Vector3.zero;

        Vector3 nextPosition =
            rb.position +
            movement *
            moveSpeed *
            Time.fixedDeltaTime;

        rb.MovePosition(nextPosition);
    }
}