using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody rb;
    private Vector3 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        movement = Vector3.zero;

        if (Keyboard.current.wKey.isPressed) movement += transform.forward;
        if (Keyboard.current.sKey.isPressed) movement -= transform.forward;
        if (Keyboard.current.aKey.isPressed) movement -= transform.right;
        if (Keyboard.current.dKey.isPressed) movement += transform.right;

        movement.y = 0;
        movement = movement.normalized;
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}