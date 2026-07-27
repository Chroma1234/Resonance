using UnityEngine;
using UnityEngine.InputSystem;

// This script controls the player's movement.
//
// Responsibilities:
// - Read keyboard input (WASD).
// - Calculate the movement direction.
// - Move the player using Rigidbody physics.
// - Prevent unwanted physics rotation.
public class PlayerMovement : MonoBehaviour
{
    // Controls how fast the player moves.
    [SerializeField] private float moveSpeed = 5f;

    // Rigidbody is used for physics movement.
    private Rigidbody rb;

    // Stores the movement direction
    // calculated from keyboard input.
    private Vector3 movement;

    private void Awake()
    {
        // Get the Rigidbody attached to this player.
        rb = GetComponent<Rigidbody>();

        // Freeze all physics rotation.
        // This prevents collisions or physics
        // from rotating the player unexpectedly.
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;

        // Remove any existing rotation speed.
        rb.angularVelocity = Vector3.zero;
    }

    private void Update()
    {
        // Reset the movement direction every frame.
        movement = Vector3.zero;

        // Make sure a keyboard is connected.
        if (Keyboard.current == null)
        {
            return;
        }

        // W moves the player forward.
        if (Keyboard.current.wKey.isPressed)
            movement += transform.forward;

        // S moves the player backward.
        if (Keyboard.current.sKey.isPressed)
            movement -= transform.forward;

        // A moves the player left.
        if (Keyboard.current.aKey.isPressed)
            movement -= transform.right;

        // D moves the player right.
        if (Keyboard.current.dKey.isPressed)
            movement += transform.right;

        // Ignore vertical movement.
        // The player only moves on the XZ plane.
        movement.y = 0f;

        // Normalize keeps the movement speed consistent.
        //
        // Without normalization,
        // moving diagonally would be faster.
        movement = movement.normalized;
    }

    private void FixedUpdate()
    {
        // Remove any unwanted physics rotation.
        rb.angularVelocity = Vector3.zero;

        // Calculate the player's next position.
        //
        // Time.fixedDeltaTime makes movement
        // independent of the physics frame rate.
        Vector3 nextPosition =
            rb.position +
            movement *
            moveSpeed *
            Time.fixedDeltaTime;

        // Move the Rigidbody using Unity's
        // physics system.
        rb.MovePosition(nextPosition);
    }
}