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

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }
    public void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
        if (movementInput.sqrMagnitude > 1)
        {
            movementInput.Normalize();
        }
    }

    private void FixedUpdate()
    {

        rb.linearVelocity = movementInput * moveSpeed;
        if (movementInput.sqrMagnitude > 1)
        {
            lastMoveDirection = movementInput.normalized;
        }
    }
}
