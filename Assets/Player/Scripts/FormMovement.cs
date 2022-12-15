using UnityEngine;

public class FormMovement : MonoBehaviour
{
    [Header("Run")]
    public float moveSpeed;
    public float runAccel;
    public float runDecel;
    public float velPower;
    public float frictionAmount;

    [Header("Wall Jump")]
    public float wallJumpRunLerp;
    public float accelInAir;
    public float decelInAir;

    [Header("Jump")]
    public float jumpingPower;
    public float coyoteTime;
    public float jumpBufferTime;
    public float jumpHangTimeThreshold;
    public float jumpHangTimeMultiplier;
    public float jumpHangAccelMult;
    public float jumpHangMaxSpeedMult;

    [Header("Gravity")]
    public float fallGravityMultiplier;
    public float fastFallGravityMultiplier;
    public float jumpCutGravityMultiplier;
    public float maxFallSpeed;
    public float maxFastFallSpeed;
}
