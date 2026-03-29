using UnityEngine;
using UnityEngine.InputSystem;

public class DroneCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform target;          // Drone root
    public Transform pivot;           // Pitch pivot
    public Camera cam;                // Main camera
    public Rigidbody targetRb;        // Optional, for look-ahead / FOV

    [Header("Follow")]
    public Vector3 followOffset = new Vector3(0f, 1.5f, 0f);
    public float followSmoothTime = 0.12f;
    public float rotationSmooth = 10f;

    [Header("Orbit")]
    public float mouseSensitivity = 0.12f;
    public float minPitch = -20f;
    public float maxPitch = 55f;
    public float startPitch = 18f;

    [Header("Auto Recenter")]
    public bool autoRecenterBehindDrone = true;
    public float recenterDelay = 1.2f;
    public float recenterSpeed = 2.5f;

    [Header("Camera Distance")]
    public float distance = 5.5f;
    public float sideOffset = 0f;
    public float heightOffset = 1.8f;

    [Header("Look Ahead")]
    public bool useLookAhead = true;
    public float lookAheadAmount = 1.2f;
    public float lookAheadSmooth = 4f;

    [Header("FOV")]
    public bool useDynamicFov = true;
    public float baseFov = 65f;
    public float maxFov = 78f;
    public float speedForMaxFov = 18f;
    public float fovSmooth = 4f;

    [Header("Cursor")]
    public bool lockCursor = true;

    private float desiredYaw;
    private float currentYaw;
    private float desiredPitch;
    private float currentPitch;

    private Vector3 followVelocity;
    private Vector3 currentLookAhead;
    private float lastManualLookTime;

    private void Start()
    {
        if (target == null || pivot == null || cam == null)
        {
            Debug.LogError("DroneCameraController: Missing target, pivot, or camera reference.");
            enabled = false;
            return;
        }

        if (targetRb == null)
            targetRb = target.GetComponent<Rigidbody>();

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        desiredYaw = target.eulerAngles.y;
        currentYaw = desiredYaw;

        desiredPitch = startPitch;
        currentPitch = desiredPitch;

        transform.position = target.position + followOffset;
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        pivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);

        cam.transform.localPosition = new Vector3(sideOffset, heightOffset, -distance);
        cam.transform.localRotation = Quaternion.identity;

        if (cam != null)
            cam.fieldOfView = baseFov;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        HandleInput();
        HandleAutoRecenter();
        HandleFollow();
        HandleRotation();
        HandleCameraLocalPose();
        HandleDynamicFov();
    }

    private void HandleInput()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        if (mouseDelta.sqrMagnitude > 0.0001f)
            lastManualLookTime = Time.time;

        desiredYaw += mouseDelta.x * mouseSensitivity;
        desiredPitch -= mouseDelta.y * mouseSensitivity;
        desiredPitch = Mathf.Clamp(desiredPitch, minPitch, maxPitch);
    }

    private void HandleAutoRecenter()
    {
        if (!autoRecenterBehindDrone)
            return;

        if (Time.time - lastManualLookTime < recenterDelay)
            return;

        float droneYaw = target.eulerAngles.y;
        desiredYaw = Mathf.LerpAngle(desiredYaw, droneYaw, recenterSpeed * Time.deltaTime);
    }

    private void HandleFollow()
    {
        Vector3 targetPos = target.position + followOffset;

        if (useLookAhead && targetRb != null)
        {
            Vector3 horizontalVel = Vector3.ProjectOnPlane(targetRb.linearVelocity, Vector3.up);
            Vector3 desiredLookAhead = Vector3.zero;

            if (horizontalVel.sqrMagnitude > 0.01f)
                desiredLookAhead = horizontalVel.normalized * Mathf.Min(horizontalVel.magnitude * 0.08f, lookAheadAmount);

            currentLookAhead = Vector3.Lerp(
                currentLookAhead,
                desiredLookAhead,
                lookAheadSmooth * Time.deltaTime
            );

            targetPos += currentLookAhead;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref followVelocity,
            followSmoothTime
        );
    }

    private void HandleRotation()
    {
        currentYaw = Mathf.LerpAngle(currentYaw, desiredYaw, rotationSmooth * Time.deltaTime);
        currentPitch = Mathf.Lerp(currentPitch, desiredPitch, rotationSmooth * Time.deltaTime);

        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        pivot.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    private void HandleCameraLocalPose()
    {
        Vector3 desiredLocalPos = new Vector3(sideOffset, heightOffset, -distance);

        cam.transform.localPosition = Vector3.Lerp(
            cam.transform.localPosition,
            desiredLocalPos,
            rotationSmooth * Time.deltaTime
        );

        cam.transform.localRotation = Quaternion.identity;
    }

    private void HandleDynamicFov()
    {
        if (!useDynamicFov || cam == null)
            return;

        float speed = 0f;
        if (targetRb != null)
            speed = targetRb.linearVelocity.magnitude;

        float t = Mathf.Clamp01(speed / Mathf.Max(0.01f, speedForMaxFov));
        float targetFov = Mathf.Lerp(baseFov, maxFov, t);

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, fovSmooth * Time.deltaTime);
    }
}