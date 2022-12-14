using UnityEngine;

public class FormMovement : MonoBehaviour
{
    [Header("Run")]
    public float moveSpeed;
    public float acceleration;
    public float deceleration;
    public float velPower;
    public float frictionAmount;

    [Header("Jump")]
    public float jumpingPower;
    public float coyoteTime;
    public float jumpBufferTime;
    public float jumpHangTimeThreshold;
    public float jumpHangTimeMultiplier;

    [Header("Gravity")]
    public float fallGravityMultiplier;
    public float fastFallGravityMultiplier;
    public float jumpCutGravityMultiplier;
    public float maxFallSpeed;
    public float maxFastFallSpeed;
}
