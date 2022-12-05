using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    #region Serialize Fields
    [Header("Run")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float deceleration;
    [SerializeField] private float velPower;
    [SerializeField] private float frictionAmount;

    [Header("Jump")]
    [SerializeField] private float jumpingPower;
    [SerializeField] private float coyoteTime;
    [SerializeField] private float jumpBufferTime;
    [SerializeField] private float jumpHangTimeThreshold;
    [SerializeField] private float jumpHangTimeMultiplier;

    [Header("Gravity")]
    [SerializeField] private float fallGravityMultiplier;
    [SerializeField] private float fastFallGravityMultiplier;
    [SerializeField] private float jumpCutGravityMultiplier;
    [SerializeField] private float maxFallSpeed;
    [SerializeField] private float maxFastFallSpeed;

    [Header("Dash")]
    [SerializeField] private float dashingPower;
    [SerializeField] private float dashingTime;
    [SerializeField] private float dashingCooldown;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;
    #endregion

    #region Private Variables
    private bool isFacingRight = true;
    private float horizontal;
    private float vertical;

    private bool canDash = true;
    private bool isDashing;

    private float lastGroundedTime;
    private float lastJumpTime;
    private bool isJumping = false;
    private bool isJumpFalling = false;
    private bool isJumpCut = false;

    private float defaultGravityScale;
    #endregion

    void Start()
    {
        defaultGravityScale = rb.gravityScale;
    }

    void Update()
    {
        #region Timers
        lastGroundedTime -= Time.deltaTime;
        lastJumpTime -= Time.deltaTime;
        #endregion

        #region Collision Checks
        if (IsGrounded() && !isJumping)
        {
            lastGroundedTime = coyoteTime;
        }
        #endregion

        #region Jump Checks
        if (isJumping && rb.velocity.y < -0.01f)
        {
            isJumping = false;
            isJumpFalling = true;
        }

        if (CanJump())
        {
            isJumpCut = false;

            if (!isJumping)
            {
                isJumpFalling = false;
            }
        }

        // Jump
        if (CanJump() && lastJumpTime > 0.01f)
        {
            isJumping = true;
            isJumpCut = false;
            isJumpFalling = false;
            Jump();
        }
        #endregion

        #region Dashing
        if (isDashing)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(Dash());
        }
        #endregion

        #region Gravity
        if (rb.velocity.y < -0.01f && vertical < -0.01f)
        {
            // Higher gravity if holding down
            rb.gravityScale = defaultGravityScale * fastFallGravityMultiplier;
            // Caps max fall speed, so when falling over large distances we don't accelerate to insanely high speeds
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, maxFastFallSpeed));
        }
        else if (isJumpCut)
        {
            // Higher gravity if jump button released (TODO: remove this if we want floaty falls)
            rb.gravityScale = defaultGravityScale * jumpCutGravityMultiplier;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -maxFallSpeed));
        }
        else if ((isJumping || isJumpFalling) && Mathf.Abs(rb.velocity.y) < jumpHangTimeThreshold)
        {
            rb.gravityScale = defaultGravityScale * jumpHangTimeMultiplier;
        }
        else if (rb.velocity.y < -0.01f)
		{
			// Higher gravity if falling
			rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
			// Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
			rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -maxFallSpeed));
		}
		else
		{
			//Default gravity if standing on a platform or moving upwards
			rb.gravityScale = defaultGravityScale;
		}
        #endregion

        Flip();
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            return;
        }

        MovePlayer();
        AddFriction();
    }

    // Ref: https://www.youtube.com/watch?v=KbtcEVCM7bw&list=LL&index=2
    private void MovePlayer()
    {
        float targetSpeed = horizontal * moveSpeed;
        float speedDiff = targetSpeed - rb.velocity.x;
        float accelRate = (Math.Abs(targetSpeed) > 0f) ? acceleration : deceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velPower) * Mathf.Sign(speedDiff);

        rb.AddForce(movement * Vector2.right);
    }

    // Ref: https://www.youtube.com/watch?v=KbtcEVCM7bw&list=LL&index=2
    private void AddFriction()
    {
        if (IsGrounded() && ShouldStop())
        {
            float amount = Mathf.Min(Mathf.Abs(rb.velocity.x), Mathf.Abs(frictionAmount));
            amount *= Mathf.Sign(rb.velocity.x);

            rb.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
        }
    }

    // Ref: https://www.youtube.com/watch?v=24-BkpFSZuI&list=LL&index=1
    public void OnMove(InputAction.CallbackContext context)
    {
        horizontal = context.ReadValue<Vector2>().x;
        vertical = context.ReadValue<Vector2>().y;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            lastJumpTime = jumpBufferTime;
        }

        if (context.canceled && rb.velocity.y > 0.01f)
        {
            if (CanJumpCut())
            {
                isJumpCut = true;
            }
        }
    }

    private void Jump()
    {
        lastJumpTime = 0f;
        lastGroundedTime = 0f;

        float force = jumpingPower;
        if (rb.velocity.y < -0.01f)
        {
            force -= rb.velocity.y;
        }

        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    private void Flip()
    {
        if (isFacingRight && horizontal < -0.01f || !isFacingRight && horizontal > 0.01f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private bool CanJump()
    {
        return lastGroundedTime > 0f && !isJumping;
    }

    private bool CanJumpCut()
    {
        return isJumping && rb.velocity.y > 0.01f;
    }

    private bool ShouldStop()
    {
        return Mathf.Abs(horizontal) < 0.01f;
    }

    private IEnumerator Dash()
    {
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
