using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Unity uses liear drag. For realism i needed quadratic drag.
/// This script is for that.
/// </summary>
public class DroneAerodynamics : MonoBehaviour
{
    [Header("Drag Coefficients")]
    [Tooltip("Drag coefficient for forward/backward motion (local Z).")]
    [SerializeField] private float forwardDragCoeff = 0.15f;

    [Tooltip("Drag coefficient for left/right motion (local X).")]
    [SerializeField] private float strafeDragCoeff = 0.20f;

    [Tooltip("Drag coefficient for upward/downward motion (local Y).")]
    [SerializeField] private float verticalDragCoeff = 0.25f;

    [Tooltip("Maximum drag force magnitude, in Newtons. Prevents unrealistic forces at extreme speeds.")]
    [SerializeField] private float maxDragForce = 100f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.drag = 0f; // Disable Unity's built-in linear drag.
    }

    private void FixedUpdate()
    {
        if (rb == null || rb.isKinematic)
            return;

        // Convert world velocity into the drone's local coordinate frame.
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

        // Quadratic drag: F = -k * v * |v|
        float dragX = -strafeDragCoeff * localVelocity.x * Mathf.Abs(localVelocity.x);
        float dragY = -verticalDragCoeff * localVelocity.y * Mathf.Abs(localVelocity.y);
        float dragZ = -forwardDragCoeff * localVelocity.z * Mathf.Abs(localVelocity.z);

        Vector3 localDragForce = new Vector3(dragX, dragY, dragZ);

        // Optional safety clamp.
        if (localDragForce.sqrMagnitude > maxDragForce * maxDragForce)
            localDragForce = localDragForce.normalized * maxDragForce;

        // Convert local drag force back to world space and apply it.
        Vector3 worldDragForce = transform.TransformDirection(localDragForce);
        rb.AddForce(worldDragForce, ForceMode.Force);
    }
}
