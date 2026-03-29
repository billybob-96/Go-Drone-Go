using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DroneControllerRobust : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference moveAction;     // Vector2: x = strafe, y = forward
    public InputActionReference ascendAction;   // Float: -1..1
    public InputActionReference yawAction;      // Float: -1..1

    [Header("Optional Visual")]
    public Transform visualChild;

    [Header("Input Options")]
    [Tooltip("Turn this on if pushing forward currently makes the drone move backward.")]
    public bool invertForward = true;

    [Header("Hover / Vertical")]
    public float hoverHeight = 2f;
    public bool lockToHoverHeight = false;
    public float verticalAccel = 22f;
    public float verticalDamping = 8f;
    public float manualClimbAccel = 14f;
    public float maxVerticalSpeed = 6f;

    [Header("Planar Movement")]
    public float moveAcceleration = 16f;
    public float maxHorizontalSpeed = 12f;
    public float horizontalDamping = 4.5f;
    public float brakingDamping = 7f;

    [Header("Tilt Visual / Flight Feel")]
    public float maxTiltDegrees = 18f;
    public float uprightTorque = 18f;
    public float uprightDamping = 3.5f;
    public float yawTorque = 8f;
    public float yawDamping = 2.5f;

    [Header("Extra Stability")]
    public float maxTiltCorrectionTorque = 20f;
    public float maxYawTorque = 10f;
    public float airAngularDamping = 1.5f;

    private Rigidbody rb;

    private Vector2 moveInput;
    private float ascendInput;
    private float yawInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = true;

        // Good default runtime safety values if the inspector is still at rough defaults.
        rb.linearDamping = Mathf.Max(rb.linearDamping, 0.2f);
        rb.angularDamping = Mathf.Max(rb.angularDamping, airAngularDamping);
    }

    private void OnEnable()
    {
        moveAction?.action?.Enable();
        ascendAction?.action?.Enable();
        yawAction?.action?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.action?.Disable();
        ascendAction?.action?.Disable();
        yawAction?.action?.Disable();
    }

    private void Update()
    {
        moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        ascendInput = ascendAction != null ? ascendAction.action.ReadValue<float>() : 0f;
        yawInput = yawAction != null ? yawAction.action.ReadValue<float>() : 0f;

        if (invertForward)
            moveInput.y *= -1f;
    }

    private void FixedUpdate()
    {
        ApplyVerticalControl();
        ApplyPlanarMovement();
        ApplyYawControl();
        ApplyUprightStabilization();
        ClampSpeeds();
    }

    private void ApplyVerticalControl()
    {
        float gravityAccel = Physics.gravity.magnitude;

        // Always counter gravity first so the drone has a firm baseline.
        rb.AddForce(Vector3.up * gravityAccel, ForceMode.Acceleration);

        if (lockToHoverHeight)
        {
            float heightError = hoverHeight - transform.position.y;
            float verticalVel = rb.linearVelocity.y;

            float accel = (heightError * verticalAccel) - (verticalVel * verticalDamping);
            rb.AddForce(Vector3.up * accel, ForceMode.Acceleration);
        }
        else
        {
            // Manual climb / descend with vertical damping so it feels less floaty.
            float currentVerticalVel = rb.linearVelocity.y;
            float accel = (ascendInput * manualClimbAccel) - (currentVerticalVel * (verticalDamping * 0.5f));
            rb.AddForce(Vector3.up * accel, ForceMode.Acceleration);
        }
    }

    private void ApplyPlanarMovement()
    {
        // Input relative to drone yaw direction, but movement stays in world-horizontal plane.
        Vector3 forwardFlat = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 rightFlat = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

        Vector3 desiredMove = (forwardFlat * moveInput.y) + (rightFlat * moveInput.x);
        if (desiredMove.sqrMagnitude > 1f)
            desiredMove.Normalize();

        // Horizontal velocity only.
        Vector3 horizontalVel = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);

        // Accelerate toward desired horizontal movement.
        rb.AddForce(desiredMove * moveAcceleration, ForceMode.Acceleration);

        // Add damping so it stops drifting and feels more solid.
        float damping = desiredMove.sqrMagnitude > 0.01f ? horizontalDamping : brakingDamping;
        rb.AddForce(-horizontalVel * damping, ForceMode.Acceleration);
    }

    private void ApplyYawControl()
    {
        float yawRate = Vector3.Dot(rb.angularVelocity, transform.up); // rad/s around local up
        float torque = (yawInput * yawTorque) - (yawRate * yawDamping);
        torque = Mathf.Clamp(torque, -maxYawTorque, maxYawTorque);

        rb.AddTorque(transform.up * torque, ForceMode.Acceleration);
    }

    private void ApplyUprightStabilization()
    {
        // Desired orientation: keep the drone's up aligned with world up,
        // but preserve its current yaw by flattening forward onto the horizontal plane.
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

        // If we're too vertical / upside-down and forward projection gets tiny,
        // fall back to right vector so we can still build a valid upright target.
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = Vector3.ProjectOnPlane(transform.right, Vector3.up);

        flatForward.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(flatForward, Vector3.up);

        // Rotation needed to get from current to target
        Quaternion error = targetRotation * Quaternion.Inverse(rb.rotation);

        // Convert quaternion error to axis-angle
        error.ToAngleAxis(out float angleDeg, out Vector3 axis);

        if (angleDeg > 180f)
            angleDeg -= 360f;

        // Safety for NaN / degenerate axis
        if (float.IsNaN(axis.x) || axis.sqrMagnitude < 0.0001f)
            return;

        axis.Normalize();

        // Angular velocity in world space
        Vector3 angVel = rb.angularVelocity;

        // Remove yaw damping from upright correction so yaw remains independently controllable.
        Vector3 yawComponent = Vector3.Project(angVel, transform.up);
        Vector3 tiltAngVel = angVel - yawComponent;

        Vector3 correctiveTorque =
         axis * (angleDeg * Mathf.Deg2Rad * uprightTorque)
            - tiltAngVel * uprightDamping;

        correctiveTorque = Vector3.ClampMagnitude(correctiveTorque, maxTiltCorrectionTorque);

        rb.AddTorque(correctiveTorque, ForceMode.Acceleration);
    }

    private void ClampSpeeds()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = Vector3.ProjectOnPlane(velocity, Vector3.up);
        float vertical = velocity.y;

        if (horizontal.magnitude > maxHorizontalSpeed)
            horizontal = horizontal.normalized * maxHorizontalSpeed;

        vertical = Mathf.Clamp(vertical, -maxVerticalSpeed, maxVerticalSpeed);

        rb.linearVelocity = horizontal + Vector3.up * vertical;
    }
}