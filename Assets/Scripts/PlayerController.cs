using UnityEngine;
using UnityEngine.InputSystem; // Required for New Input System

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float groundCheckDistance = 1.1f;

    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Check if Keyboard is present
        if (Keyboard.current == null) return;

        float moveHorizontal = 0f;
        float moveVertical = 0f;

        // Read Keyboard Input
        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            moveHorizontal = -1f;
        if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            moveHorizontal = 1f;

        if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed)
            moveVertical = 1f;
        if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
            moveVertical = -1f;

        // Create movement vector
        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

        // Apply movement
        // Using linearVelocity as requested by user (Unity 6+)
        rb.linearVelocity = new Vector3(movement.x * moveSpeed, rb.linearVelocity.y, movement.z * moveSpeed);

        // Movement Constraints (Screen bounds)
        Vector3 currentPos = transform.position;
        currentPos.x = Mathf.Clamp(currentPos.x, -50f, 50f);
        currentPos.z = Mathf.Clamp(currentPos.z, -50f, 50f);
        transform.position = currentPos;

        // Jump
        // Ground Check using Raycast
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);

        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}
