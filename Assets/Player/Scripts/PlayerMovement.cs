using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    #region Serialize Fields
    [Header("Dash")]
    [SerializeField] private float dashingPower;
    [SerializeField] private float dashingTime;
    [SerializeField] private float dashingCooldown;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private TrailRenderer tr;
    [SerializeField] private PlayerFormController playerFormController;
    [SerializeField] private HumanMovement humanMovement;
    [SerializeField] private FoxMovement foxMovement;
    #endregion

    #region Private Variables
    // Form (Human/Fox/etc)
    private FormMovement _formMovement;

    // Run
    private float moveSpeed;
    private float acceleration;
    private float deceleration;
    private float velPower;
    private float frictionAmount;

    // Jump
    private float jumpingPower;
    private float coyoteTime;
    private float jumpBufferTime;
    private float jumpHangTimeThreshold;
    private float jumpHangTimeMultiplier;

    // Gravity
    private float fallGravityMultiplier;
    private float fastFallGravityMultiplier;
    private float jumpCutGravityMultiplier;
    private float maxFallSpeed;
    private float maxFastFallSpeed;

    private bool isFacingRight = true;
    private float _horizontalInput;
    private float _verticalInput;
    private DirectionX _moveInputX;
    private DirectionY _moveInputY;

    private bool canDash = true;
    private bool isDashing;

    private float lastGroundedTime;
    private float lastJumpTime;
    private bool isJumping = false;
    private bool isJumpFalling = false;
    private bool isJumpCut = false;

    private float _defaultGravityScale;

    private const float ZERO_THRESHOLD = 0.01f;
    #endregion

    private enum DirectionX
    {
        LEFT,
        RIGHT,
        NONE
    };

    private enum DirectionY
    {
        UP,
        DOWN,
        NONE
    }

    void Start()
    {
        ChangeForm();
        SetInitialGravityScale(rb.gravityScale);
        SetMoveInputs();
    }

    void Update()
    {
        ChangeForm(); // TODO: Call this only when we switch forms, not every update
        UpdateTimers();
        SetJumpChecks();
        SetDashingChecks();
        SetGravityChecks();
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

    private void ChangeForm()
    {
        SetFormMovement();
        SetMovementConstants();
    }

    private void SetFormMovement()
    {
        switch (playerFormController._playerForm)
        {
            case PlayerFormController.AnimalForm.HUMAN:
                _formMovement = humanMovement;
                return;
            case PlayerFormController.AnimalForm.FOX:
                _formMovement = foxMovement;
                return;
            default:
                _formMovement = humanMovement;
                return;
        }
    }

    private void SetMovementConstants()
    {        
        moveSpeed = _formMovement.moveSpeed;
        acceleration = _formMovement.acceleration;
        deceleration = _formMovement.deceleration;
        velPower = _formMovement.velPower;
        frictionAmount = _formMovement.frictionAmount;

        jumpingPower = _formMovement.jumpingPower;
        coyoteTime = _formMovement.coyoteTime;
        jumpBufferTime = _formMovement.jumpBufferTime;
        jumpHangTimeThreshold = _formMovement.jumpHangTimeThreshold;
        jumpHangTimeMultiplier = _formMovement.jumpHangTimeMultiplier;

        fallGravityMultiplier = _formMovement.fallGravityMultiplier;
        fastFallGravityMultiplier = _formMovement.fastFallGravityMultiplier;
        jumpCutGravityMultiplier = _formMovement.jumpCutGravityMultiplier;
        maxFallSpeed = _formMovement.maxFallSpeed;
        maxFastFallSpeed = _formMovement.maxFastFallSpeed;
    }
    
    private void SetInitialGravityScale(float gravityScale)
    {
        _defaultGravityScale = gravityScale;
    }

    void UpdateTimers()
    {
        lastGroundedTime -= Time.deltaTime;
        lastJumpTime -= Time.deltaTime;

        if (IsGrounded() && !isJumping)
        {
            lastGroundedTime = coyoteTime;
        }
    }

    private void SetMoveInputs()
    {
        if (_horizontalInput < -ZERO_THRESHOLD) _moveInputX = DirectionX.LEFT;
        else if (_horizontalInput > ZERO_THRESHOLD) _moveInputX = DirectionX.RIGHT;
        else _moveInputX = DirectionX.NONE;

        if (_verticalInput < -ZERO_THRESHOLD) _moveInputY = DirectionY.DOWN;
        else if (_verticalInput > ZERO_THRESHOLD) _moveInputY = DirectionY.UP;
        else _moveInputY = DirectionY.NONE;
    }


    void SetJumpChecks()
    {
        if (isJumping && rb.velocity.y < -ZERO_THRESHOLD)
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
        if (CanJump() && lastJumpTime > ZERO_THRESHOLD)
        {
            isJumping = true;
            isJumpCut = false;
            isJumpFalling = false;
            Jump();
        }
    }

    void SetDashingChecks()
    {
        if (isDashing)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(Dash());
        }
    }

    void SetGravityChecks()
    {
        if (rb.velocity.y < -ZERO_THRESHOLD && _moveInputY == DirectionY.DOWN)
        {
            // Higher gravity if holding down
            rb.gravityScale = _defaultGravityScale * fastFallGravityMultiplier;
            // Caps max fall speed, so when falling over large distances we don't accelerate to insanely high speeds
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -maxFastFallSpeed));
        }
        else if (isJumpCut)
        {
            // Higher gravity if jump button released (TODO: remove this if we want floaty falls)
            rb.gravityScale = _defaultGravityScale * jumpCutGravityMultiplier;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -maxFallSpeed));
        }
        else if ((isJumping || isJumpFalling) && Mathf.Abs(rb.velocity.y) < jumpHangTimeThreshold)
        {
            rb.gravityScale = _defaultGravityScale * jumpHangTimeMultiplier;
        }
        else if (rb.velocity.y < -ZERO_THRESHOLD)
        {
            // Higher gravity if falling
            rb.gravityScale = _defaultGravityScale * fallGravityMultiplier;
            // Caps maximum fall speed, so when falling over large distances we don't accelerate to insanely high speeds
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -maxFallSpeed));
        }
        else
        {
            //Default gravity if standing on a platform or moving upwards
            rb.gravityScale = _defaultGravityScale;
        }
    }

    private void Flip()
    {
        if (isFacingRight && _moveInputX == DirectionX.LEFT || !isFacingRight && _moveInputX == DirectionX.RIGHT)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    // Ref: https://www.youtube.com/watch?v=KbtcEVCM7bw&list=LL&index=2
    private void MovePlayer()
    {
        float targetSpeed = _horizontalInput * moveSpeed;
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
        _horizontalInput = context.ReadValue<Vector2>().x;
        _verticalInput = context.ReadValue<Vector2>().y;

        SetMoveInputs();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            lastJumpTime = jumpBufferTime;
        }

        if (context.canceled && rb.velocity.y > ZERO_THRESHOLD)
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
        if (rb.velocity.y < -ZERO_THRESHOLD)
        {
            force -= rb.velocity.y;
        }

        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
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
        return isJumping && rb.velocity.y > ZERO_THRESHOLD;
    }

    private bool ShouldStop()
    {
        return _moveInputX == DirectionX.NONE;
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
