using System.Collections.Generic;
using UnityEngine;

public class DroneAnimator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Usually the Rigidbody root of the drone.")]
    public Rigidbody droneRb;

    [Tooltip("Optional explicit root to search for propellers under. If left empty, all children under this object are searched.")]
    public Transform propSearchRoot;

    [Tooltip("Optional visual mesh root. If left empty, this transform is animated directly.")]
    public Transform animatedBody;

    [Header("Model Correction")]
    [Tooltip("Use this if the visible mesh itself needs a fixed rotation offset.")]
    public Vector3 modelRotationOffset;

    [Header("Propellers")]
    public float idlePropSpeed = 1200f;
    public float maxPropSpeed = 2400f;
    public float propSpinSmoothing = 8f;

    [Tooltip("Extra prop speed from horizontal movement.")]
    public float propSpeedFromMovement = 35f;

    [Tooltip("Extra prop speed from climb / descend speed.")]
    public float propSpeedFromVertical = 80f;

    public Material blurMaterial;
    public float blurStartSpeed = 1000f;
    public float blurFullSpeed = 2200f;

    [Header("Idle Motion")]
    public float bobHeight = 0.03f;
    public float bobSpeed = 2.2f;
    public float wobbleAmount = 0.8f;
    public float wobbleSpeed = 1.8f;

    [Header("Directional Tilt")]
    [Tooltip("Forward/back visual tilt in degrees.")]
    public float maxPitchTilt = 14f;

    [Tooltip("Left/right visual bank in degrees.")]
    public float maxRollTilt = 16f;

    [Tooltip("How much local velocity affects visual tilt.")]
    public float tiltResponsiveness = 1.6f;

    [Tooltip("How quickly the mesh reaches the target tilt.")]
    public float tiltSmooth = 8f;

    [Header("Yaw Visuals")]
    [Tooltip("Subtle visual yaw lean/twist while rotating.")]
    public float yawVisualTilt = 4f;

    [Tooltip("How much angular velocity influences yaw visual tilt.")]
    public float yawTiltResponsiveness = 20f;

    [Header("Movement Punch")]
    [Tooltip("Extra tilt from acceleration, makes controls feel snappier.")]
    public float accelTiltAmount = 10f;

    [Tooltip("How quickly acceleration influence settles.")]
    public float accelTiltSmooth = 10f;

    [Header("Smoothing")]
    public float positionSmooth = 8f;

    private Transform[] props;
    private Vector3 baseLocalPos;
    private Quaternion baseLocalRot;
    private Quaternion modelOffsetRot;

    private float currentPropSpeed;
    private Quaternion currentTilt = Quaternion.identity;
    private Vector3 currentLocalPos;
    private Vector3 smoothedLocalVelocity;
    private Vector3 lastWorldVelocity;

    private void Start()
    {
        if (animatedBody == null)
            animatedBody = transform;

        if (droneRb == null)
            droneRb = GetComponentInParent<Rigidbody>();

        if (droneRb == null)
        {
            Debug.LogError("DroneAnimator: No Rigidbody found in parent hierarchy.");
            enabled = false;
            return;
        }

        if (propSearchRoot == null)
            propSearchRoot = transform;

        baseLocalPos = animatedBody.localPosition;
        baseLocalRot = animatedBody.localRotation;
        modelOffsetRot = Quaternion.Euler(modelRotationOffset);
        currentLocalPos = baseLocalPos;

        FindPropellers();

        currentPropSpeed = idlePropSpeed;
        lastWorldVelocity = droneRb.linearVelocity;
    }

    private void Update()
    {
        AnimateProps();
        AnimateBody();
    }

    private void FindPropellers()
    {
        List<Transform> list = new List<Transform>();
        Transform[] allChildren = propSearchRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in allChildren)
        {
            string lowerName = t.name.ToLower();

            // Looks for "motor" objects with a visible prop child,
            // or direct "prop" named transforms.
            if (lowerName.Contains("motor") && t.childCount > 0)
            {
                list.Add(t.GetChild(0));
            }
            else if (lowerName.Contains("prop"))
            {
                list.Add(t);
            }
        }

        props = list.ToArray();

        if (props.Length == 0)
            Debug.LogWarning("DroneAnimator: No propellers found under propSearchRoot.");
    }

    private void AnimateProps()
    {
        Vector3 velocity = droneRb.linearVelocity;
        Vector3 horizontalVel = Vector3.ProjectOnPlane(velocity, Vector3.up);
        float verticalSpeed = Mathf.Abs(velocity.y);

        float targetPropSpeed =
            idlePropSpeed +
            horizontalVel.magnitude * propSpeedFromMovement +
            verticalSpeed * propSpeedFromVertical;

        targetPropSpeed = Mathf.Clamp(targetPropSpeed, idlePropSpeed, maxPropSpeed);
        currentPropSpeed = Mathf.Lerp(currentPropSpeed, targetPropSpeed, Time.deltaTime * propSpinSmoothing);

        foreach (Transform p in props)
        {
            // Assumes the propeller spins around its local Z axis.
            p.Rotate(0f, 0f, currentPropSpeed * Time.deltaTime, Space.Self);
        }

        if (blurMaterial != null)
        {
            float blur = Mathf.InverseLerp(blurStartSpeed, blurFullSpeed, currentPropSpeed);
            blurMaterial.SetFloat("_BlurStrength", blur);
        }
    }

    private void AnimateBody()
    {
        Vector3 worldVelocity = droneRb.linearVelocity;
        Vector3 localVelocity = droneRb.transform.InverseTransformDirection(worldVelocity);

        smoothedLocalVelocity = Vector3.Lerp(
            smoothedLocalVelocity,
            localVelocity,
            Time.deltaTime * tiltSmooth
        );

        // Calculate acceleration for extra punch.
        Vector3 worldAcceleration = (worldVelocity - lastWorldVelocity) / Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 localAcceleration = droneRb.transform.InverseTransformDirection(worldAcceleration);
        lastWorldVelocity = worldVelocity;

        // Core directional tilt:
        // forward movement -> nose down (negative X)
        float velPitch = Mathf.Clamp(
            -smoothedLocalVelocity.z * tiltResponsiveness,
            -maxPitchTilt,
            maxPitchTilt
        );

        // right movement -> roll right (negative Z)
        float velRoll = Mathf.Clamp(
            -smoothedLocalVelocity.x * tiltResponsiveness,
            -maxRollTilt,
            maxRollTilt
        );

        // Extra tilt from acceleration for snappier arcade feel.
        float accelPitch = Mathf.Clamp(
            -localAcceleration.z * accelTiltAmount * 0.01f,
            -maxPitchTilt * 0.5f,
            maxPitchTilt * 0.5f
        );

        float accelRoll = Mathf.Clamp(
            -localAcceleration.x * accelTiltAmount * 0.01f,
            -maxRollTilt * 0.5f,
            maxRollTilt * 0.5f
        );

        // Subtle yaw visual tilt based on yaw angular velocity.
        float localYawRate = Vector3.Dot(droneRb.angularVelocity, droneRb.transform.up);
        float yawVisualZ = Mathf.Clamp(
            -localYawRate * yawTiltResponsiveness,
            -yawVisualTilt,
            yawVisualTilt
        );

        // Idle wobble.
        float idleX = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount * 0.25f;
        float idleZ = Mathf.Cos(Time.time * wobbleSpeed * 0.9f) * wobbleAmount * 0.25f;

        // Bobbing.
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        float finalPitch = velPitch + accelPitch + idleX;
        float finalRoll = velRoll + accelRoll + yawVisualZ + idleZ;

        Quaternion targetTilt = Quaternion.Euler(finalPitch, 0f, finalRoll);

        currentTilt = Quaternion.Slerp(
            currentTilt,
            targetTilt,
            Time.deltaTime * tiltSmooth
        );

        Vector3 targetLocalPos = baseLocalPos + new Vector3(0f, bob, 0f);
        currentLocalPos = Vector3.Lerp(currentLocalPos, targetLocalPos, Time.deltaTime * positionSmooth);

        animatedBody.localRotation = baseLocalRot * modelOffsetRot * currentTilt;
        animatedBody.localPosition = currentLocalPos;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        idlePropSpeed = Mathf.Max(0f, idlePropSpeed);
        maxPropSpeed = Mathf.Max(idlePropSpeed, maxPropSpeed);
        propSpinSmoothing = Mathf.Max(0f, propSpinSmoothing);

        bobHeight = Mathf.Max(0f, bobHeight);
        bobSpeed = Mathf.Max(0f, bobSpeed);
        wobbleAmount = Mathf.Max(0f, wobbleAmount);
        wobbleSpeed = Mathf.Max(0f, wobbleSpeed);

        maxPitchTilt = Mathf.Max(0f, maxPitchTilt);
        maxRollTilt = Mathf.Max(0f, maxRollTilt);
        tiltResponsiveness = Mathf.Max(0f, tiltResponsiveness);
        tiltSmooth = Mathf.Max(0f, tiltSmooth);

        yawVisualTilt = Mathf.Max(0f, yawVisualTilt);
        yawTiltResponsiveness = Mathf.Max(0f, yawTiltResponsiveness);

        accelTiltAmount = Mathf.Max(0f, accelTiltAmount);
        accelTiltSmooth = Mathf.Max(0f, accelTiltSmooth);
        positionSmooth = Mathf.Max(0f, positionSmooth);
    }
#endif
}