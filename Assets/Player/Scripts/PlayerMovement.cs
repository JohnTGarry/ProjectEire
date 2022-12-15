using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public PlayerData Data;

    #region Serialize Fields
    [Header("Dash")]
    [SerializeField] private float dashingPower;
    [SerializeField] private float dashingTime;
    [SerializeField] private float dashingCooldown;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform _frontWallCheckPoint;
    [SerializeField] private Transform _backWallCheckPoint;
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
    private float runAccel;
    private float runDecel;
    private float velPower;
    private float frictionAmount;

    // Jump
    private float jumpingPower;
    private float coyoteTime;
    private float jumpBufferTime;
    private float jumpHangTimeThreshold;
    private float jumpHangTimeMultiplier;
    private float jumpHangAccelMult;
    private float jumpHangMaxSpeedMult;

    //Wall Jump
    private bool _isWallJumping;
    private float _wallJumpStartTime;
    private float _wallJumpTime;
    private int _lastWallJumpDir;
    private float _lastOnWallTime;
    private float _lastOnWallRightTime;
    private float _lastOnWallLeftTime;
    private float _wallJumpRunLerp;
    private float _accelInAir;
    private float _decelInAir;

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
        CheckCollisions();
        UpdateLastOnWallTime();
        SetJumpChecks();
        SetDashingChecks();
        SetGravityChecks();
        Turn();
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            return;
        }

        if (_isWallJumping)
            Run(_wallJumpRunLerp);
        else
            Run(1f);

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
        runAccel = _formMovement.runAccel;
        runDecel = _formMovement.runDecel;
        velPower = _formMovement.velPower;
        frictionAmount = _formMovement.frictionAmount;

        _wallJumpRunLerp = _formMovement.wallJumpRunLerp;
        _accelInAir = _formMovement.accelInAir;
        _decelInAir = _formMovement.decelInAir;

        jumpingPower = _formMovement.jumpingPower;
        coyoteTime = _formMovement.coyoteTime;
        jumpBufferTime = _formMovement.jumpBufferTime;
        jumpHangTimeThreshold = _formMovement.jumpHangTimeThreshold;
        jumpHangTimeMultiplier = _formMovement.jumpHangTimeMultiplier;
        jumpHangAccelMult = _formMovement.jumpHangAccelMult;
        jumpHangMaxSpeedMult = _formMovement.jumpHangMaxSpeedMult;

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
        _lastOnWallTime -= Time.deltaTime;
        _lastOnWallLeftTime -= Time.deltaTime;
        _lastOnWallRightTime -= Time.deltaTime;
    }

    private void CheckCollisions()
    {
        if (isJumping) return;

        if (IsGrounded() && !isJumping)
        {
            lastGroundedTime = coyoteTime;
        }

        if (_isWallJumping) return;

        if ((IsRightSideOnWall() && isFacingRight) || (IsLeftSideOnWall() && !isFacingRight))
        {
            _lastOnWallRightTime = coyoteTime;
        }

        if ((IsRightSideOnWall() && !isFacingRight) || (IsLeftSideOnWall() && isFacingRight))
        {
            _lastOnWallLeftTime = coyoteTime;
        }
    }

    private void UpdateLastOnWallTime()
    {
        _lastOnWallTime = Mathf.Max(_lastOnWallLeftTime, _lastOnWallRightTime);
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

            if (!_isWallJumping)
            {
                isJumpFalling = true;
            }
        }

        if (_isWallJumping && Time.time - _wallJumpStartTime > _wallJumpTime)
        {
            _isWallJumping = false;
        }

        if (CanJump() && !_isWallJumping)
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
            _isWallJumping = false;
            isJumpCut = false;
            isJumpFalling = false;
            Jump();
        }
        // Wall Jump
        else if (CanWallJump() && lastJumpTime > ZERO_THRESHOLD)
        {
            _isWallJumping = true;
            isJumping = false;
            isJumpCut = false;
            isJumpFalling = false;
            _wallJumpStartTime = Time.time;
            _lastWallJumpDir = (_lastOnWallRightTime > ZERO_THRESHOLD) ? -1 : 1;
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
        else if ((isJumping || _isWallJumping || isJumpFalling) && Mathf.Abs(rb.velocity.y) < jumpHangTimeThreshold)
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

    private void Turn()
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
    private void Run(float lerpAmount)
    {
        float targetSpeed = _horizontalInput * moveSpeed;
        targetSpeed = Mathf.Lerp(rb.velocity.x, targetSpeed, lerpAmount);

        float accelRate;

        if (lastGroundedTime > ZERO_THRESHOLD)
            accelRate = (Mathf.Abs(targetSpeed) > ZERO_THRESHOLD) ? runAccel : runDecel;
        else
            accelRate = (Mathf.Abs(targetSpeed) > ZERO_THRESHOLD) ? runAccel * _accelInAir : runDecel * _decelInAir;

        //Increase are acceleration and maxSpeed when at the apex of their jump, makes the jump feel a bit more bouncy, responsive and natural
        if ((isJumping || _isWallJumping || isJumpFalling) && Mathf.Abs(rb.velocity.y) < jumpHangTimeThreshold)
        {
            accelRate *= jumpHangTimeMultiplier;
            targetSpeed *= jumpHangMaxSpeedMult;
        }

        // Conserve Momentum
        bool shouldConserveMomentum = true;
        if (shouldConserveMomentum && IsFasterThanSpeedX(targetSpeed) && IsMovingAndInSameDirectionX(targetSpeed) && lastGroundedTime < -ZERO_THRESHOLD)
        {
            // Prevent any deceleration from happening, or in other words conserve are current momentum
            // You could experiment with allowing for the player to slightly increae their speed whilst in this "state"
            accelRate = 0;
        }

        float speedDiff = targetSpeed - rb.velocity.x;
        float movement = speedDiff * accelRate;

        rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
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

    private bool IsFasterThanSpeedX(float speed)
    {
        return Mathf.Abs(rb.velocity.x) > Mathf.Abs(speed);
    }

    private bool IsMovingAndInSameDirectionX(float speed)
    {
        return Mathf.Sign(rb.velocity.x) == Mathf.Sign(speed) && Mathf.Abs(speed) > ZERO_THRESHOLD;
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

    private void WallJump(int direction)
    {
        //Ensures we can't call Wall Jump multiple times from one press
        lastJumpTime = 0;
        lastGroundedTime = 0;
        _lastOnWallRightTime = 0;
        _lastOnWallLeftTime = 0;

        Vector2 force = new Vector2(Data.wallJumpForce.x, Data.wallJumpForce.y);
        force.x *= direction; //apply force in opposite direction of wall

        if (Mathf.Sign(rb.velocity.x) != Mathf.Sign(force.x))
            force.x -= rb.velocity.x;

        if (rb.velocity.y < 0) //checks whether player is falling, if so we subtract the velocity.y (counteracting force of gravity). This ensures the player always reaches our desired jump force or greater
            force.y -= rb.velocity.y;

        //Unlike in the run we want to use the Impulse mode.
        //The default mode will apply are force instantly ignoring masss
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private bool IsRightSideOnWall()
    {
        return Physics2D.OverlapCircle(_frontWallCheckPoint.position, 0.2f, 0, groundLayer);
    }

    private bool IsLeftSideOnWall()
    {
        return Physics2D.OverlapCircle(_backWallCheckPoint.position, 0.2f, 0, groundLayer);
    }

    private bool CanJump()
    {
        return lastGroundedTime > 0f && !isJumping;
    }

    private bool CanWallJump()
    {
        return lastJumpTime > 0 && _lastOnWallTime > 0 && lastGroundedTime <= 0 &&
            (!_isWallJumping || 
                (_lastOnWallRightTime > 0 && _lastWallJumpDir == 1) || 
                (_lastOnWallLeftTime > 0 && _lastWallJumpDir == -1));
    }

    private bool CanJumpCut()
    {
        return isJumping && rb.velocity.y > ZERO_THRESHOLD;
    }

    private bool CanWallJumpCut()
    {
        return _isWallJumping && rb.velocity.y > ZERO_THRESHOLD;
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
