using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Run")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float deceleration;
    [SerializeField] private float velPower;
    [SerializeField] private float frictionAmount;

    [Header("Jump")]
    [SerializeField] private float jumpingPower;
    [SerializeField] private float coyoteTime;

    [Header("Dash")]
    [SerializeField] private float dashingPower;
    [SerializeField] private float dashingTime;
    [SerializeField] private float dashingCooldown;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;

    private bool isFacingRight = true;
    private float horizontal;

    private bool canDash = true;
    private bool isDashing;

    private float lastGroundedTime;
    private float lastJumpTime;
    private bool isJumping = false;

    void Start() {
        lastGroundedTime = 0f;
        lastJumpTime = 0f;
    }

    void Update() {
        lastGroundedTime += Time.deltaTime;
        lastJumpTime += Time.deltaTime;

        if (IsGrounded()) {
            lastGroundedTime = 0f;
        }

        if (isJumping && rb.velocity.y < 0f) {
            isJumping = false;
        }

        if (isDashing) {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash) {
            StartCoroutine(Dash());
        }

        Flip();
    }

    private void FixedUpdate() {
        if (isDashing) {
            return;
        }
        
        MovePlayer();
        AddFriction();
    }

    // Ref: https://www.youtube.com/watch?v=KbtcEVCM7bw&list=LL&index=2
    private void MovePlayer() {
        float targetSpeed = horizontal * moveSpeed;
        float speedDiff = targetSpeed - rb.velocity.x;
        float accelRate = (Math.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velPower) * Mathf.Sign(speedDiff);
        
        rb.AddForce(movement * Vector2.right);
    }

    // Ref: https://www.youtube.com/watch?v=KbtcEVCM7bw&list=LL&index=2
    private void AddFriction() {
        if (IsGrounded() && ShouldStop()) {
            float amount = Mathf.Min(Mathf.Abs(rb.velocity.x), Mathf.Abs(frictionAmount));
            amount *= Mathf.Sign(rb.velocity.x);

            rb.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
        }
    }

    // Ref: https://www.youtube.com/watch?v=24-BkpFSZuI&list=LL&index=1
    public void Move(InputAction.CallbackContext context) {
        horizontal = context.ReadValue<Vector2>().x;
    }

    public void Jump(InputAction.CallbackContext context) {
        if (context.performed && lastGroundedTime < coyoteTime && lastJumpTime < coyoteTime && !isJumping) {
            rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
            lastJumpTime = 0f;
            isJumping = true;
        }

        if (context.canceled && rb.velocity.y > 0f) {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }
    }

    private void Flip() {
        if (isFacingRight && horizontal < 0f || !isFacingRight && horizontal > 0f) {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private bool IsGrounded() {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private bool CanJump() {
        return lastGroundedTime < coyoteTime && !isJumping;
    }

    private bool ShouldStop() {
        return Mathf.Abs(horizontal) < 0.01f;
    }

    private IEnumerator Dash() {
        canDash = false;
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(transform.localScale.x * dashingPower, 0f);
        tr.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        tr.emitting = false;
        rb.gravityScale = originalGravity;
        isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;
    }
}
