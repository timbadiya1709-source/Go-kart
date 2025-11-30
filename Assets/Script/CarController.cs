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
    [SerializeField] private float motorForce = 1500f;
    [SerializeField] private float brakeForce = 3000f;
    [SerializeField] private float maxSteerAngle = 30f;

    [Header("Input")]
    private float currentSteerAngle;
    private float currentBrakeForce;
    private bool isBraking;
    // Using Unity's legacy Input axes instead of Input System

    private Rigidbody rb;

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
        HandleMotor();
        HandleSteering();
        UpdateWheelVisuals();
    }


    private void HandleMotor()
    {
        float verticalInput = Input.GetAxis("Vertical");
        // Brake when Space is held
        isBraking = Input.GetKey(KeyCode.Space);
        
        rearLeftCollider.motorTorque = verticalInput * motorForce;
        rearRightCollider.motorTorque = verticalInput * motorForce;

        currentBrakeForce = isBraking ? brakeForce : 0f;
        
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
        wheelTransform.rotation = rotation * Quaternion.Euler(-90, -90, 0);

    }
}
