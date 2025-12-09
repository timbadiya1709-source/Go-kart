using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    [SerializeField] private WheelCollider frontLeftCollider;
    [SerializeField] private WheelCollider frontRightCollider;
    [SerializeField] private WheelCollider rearLeftCollider;
    [SerializeField] private WheelCollider rearRightCollider;

    [Header("Wheel Meshes")]
    [SerializeField] private Transform frontLeftWheel;
    [SerializeField] private Transform frontRightWheel;
    [SerializeField] private Transform rearLeftWheel;
    [SerializeField] private Transform rearRightWheel;

    [Header("Motor Settings")]
    private float motorForce =10000f;
    private float brakeForce = 80000f;
    private float maxSteerAngle = 30f;
    private float maxSpeed = 100f;
    private float engineResponseTime = 0.25f;
    private float brakingDecelerationTime = 2.5f;
    [Header("Traction Control")]
    private float maxWheelRPM = 8000f;   // limit wheel spin
    private float tractionControlFactor = 0.7f; // reduce torque when slipping


    private float currentSteerAngle;
    private float currentBrakeForce;
    private bool isBraking;
    private float targetMotorInput = 0f;
    private float smoothedMotorInput = 0f;

    private Rigidbody rb;
    public float currentSpeedKmh;
    public     float gearRatio = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
    }

    private void FixedUpdate()
    {
        CalculateSpeed();
        HandleMotor();
        HandleSteering();
        UpdateWheelVisuals();
    }

    private void CalculateSpeed()
    {
        currentSpeedKmh = rb.linearVelocity.magnitude * 3.6f;
    }

private void HandleMotor()
{
    float verticalInput = Input.GetAxis("Vertical");
    isBraking = Input.GetKey(KeyCode.Space);

    // --- Smooth transition between forward and reverse ---
    targetMotorInput = verticalInput;

    float lerpSpeed = (Mathf.Sign(verticalInput) != Mathf.Sign(smoothedMotorInput))
        ? (Time.fixedDeltaTime / (engineResponseTime * 2f))   // slower when switching direction
        : (Time.fixedDeltaTime / engineResponseTime);

    smoothedMotorInput = Mathf.Lerp(smoothedMotorInput, targetMotorInput, lerpSpeed);

    // --- Gearbox system ---
    if (currentSpeedKmh < 20f)        gearRatio = 3.5f;   // 1st gear: strong acceleration
    else if (currentSpeedKmh < 50f)   gearRatio = 2.5f;   // 2nd gear
    else if (currentSpeedKmh < 90f)   gearRatio = 1.8f;   // 3rd gear
    else if (currentSpeedKmh < 140f)  gearRatio = 1.2f;   // 4th gear
    else                              gearRatio = 0.8f;   // 5th gear: weak torque at high speed

    // --- Motor force scaling with speed (FIXED) ---
    // Use a gentler curve instead of linear reduction
    // Car maintains good power up to ~80% of max speed, then gradually drops off
    float speedRatio = currentSpeedKmh / maxSpeed;
    float speedFactor = Mathf.Max(0.1f, 1f - Mathf.Pow(speedRatio, 2f));
    
    float appliedMotorForce = motorForce * gearRatio * speedFactor;

    // --- Zero torque when stopped ---
    if (currentSpeedKmh < 0.5f && Mathf.Approximately(verticalInput, 0f))
    {
        appliedMotorForce = 0f;
    }

    // --- Basic traction control ---
    if (Mathf.Abs(rearLeftCollider.rpm) > maxWheelRPM || Mathf.Abs(rearRightCollider.rpm) > maxWheelRPM)
    {
        appliedMotorForce *= tractionControlFactor;
    }

    // --- Apply torque to rear wheels ---
    rearLeftCollider.motorTorque = smoothedMotorInput * appliedMotorForce;
    rearRightCollider.motorTorque = smoothedMotorInput * appliedMotorForce;

    // --- Braking logic ---
    if (isBraking)
    {
        currentBrakeForce = brakeForce;
    }
    else if (smoothedMotorInput == 0f && currentSpeedKmh > 1f)
    {
        float brakingForce = (rb.mass * currentSpeedKmh) / (3.6f * brakingDecelerationTime);
        currentBrakeForce = brakingForce;
    }
    else
    {
        currentBrakeForce = 0f;
    }

    // --- Apply brake torque to all wheels ---
    frontLeftCollider.brakeTorque = currentBrakeForce;
    frontRightCollider.brakeTorque = currentBrakeForce;
    rearLeftCollider.brakeTorque = currentBrakeForce;
    rearRightCollider.brakeTorque = currentBrakeForce;
}
    private void HandleSteering()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        currentSteerAngle = maxSteerAngle * horizontalInput;
        frontLeftCollider.steerAngle = currentSteerAngle;
        frontRightCollider.steerAngle = currentSteerAngle;
    }

    private void UpdateWheelVisuals()
    {
        UpdateSingleWheel(frontLeftCollider, frontLeftWheel);
        UpdateSingleWheel(frontRightCollider, frontRightWheel);
        UpdateSingleWheel(rearLeftCollider, rearLeftWheel);
        UpdateSingleWheel(rearRightCollider, rearRightWheel);
    }

    private void UpdateSingleWheel(WheelCollider collider, Transform wheelTransform)
    {
        collider.GetWorldPose(out Vector3 position, out Quaternion rotation);
        wheelTransform.position = position;
        wheelTransform.rotation = rotation * Quaternion.Euler(-90, 0, -90);
    }

    public float GetSpeedKmh()
    {
        return currentSpeedKmh;
    }
    public float GetSmoothedMotorInput()
{
    return smoothedMotorInput;
}
}