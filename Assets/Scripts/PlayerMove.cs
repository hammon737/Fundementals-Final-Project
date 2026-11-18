using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMove : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movementInput;
    private Vector2 lastMoveDirection;
    private bool movementPaused;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }
    public void OnMove(InputValue value)
    {
        if (movementPaused)
            return;

        movementInput = value.Get<Vector2>();
        if (movementInput.sqrMagnitude > 1)
        {
            movementInput.Normalize();
        }
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            animator.SetBool("IsWalkingDown", true);
        }
        else if (Keyboard.current.sKey.wasReleasedThisFrame)
        {
            animator.SetBool("IsWalkingDown", false);
            lastMoveDirection = Vector2.down;
        }
        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            animator.SetBool("IsWalkingLeft", true);
        }
        else if (Keyboard.current.aKey.wasReleasedThisFrame)
        {
            animator.SetBool("IsWalkingLeft", false);
            lastMoveDirection = Vector2.left;
        }
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            animator.SetBool("IsWalkingRight", true);
        }
        else if (Keyboard.current.dKey.wasReleasedThisFrame)
        {
            animator.SetBool("IsWalkingRight", false);
            lastMoveDirection = Vector2.right;
        }
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            animator.SetBool("IsWalkingUp", true);
        }
        else if (Keyboard.current.wKey.wasReleasedThisFrame)
        {
            animator.SetBool("IsWalkingUp", false);
            lastMoveDirection = Vector2.up;
        }

        animator.speed = movementInput.sqrMagnitude > 0 ? 1f : 0f;
    }

    public void SetMovementPaused(bool paused)
    {
        movementPaused = paused;
        if (!paused)
            return;

        movementInput = Vector2.zero;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        if (animator != null)
        {
            animator.SetBool("IsWalkingDown", false);
            animator.SetBool("IsWalkingLeft", false);
            animator.SetBool("IsWalkingRight", false);
            animator.SetBool("IsWalkingUp", false);
            animator.speed = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (movementPaused)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = movementInput * moveSpeed;
    }
}
