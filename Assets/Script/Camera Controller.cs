using UnityEngine;

public class VehicleCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform vehicle;
    [SerializeField] private Rigidbody vehicleRb;
    
    [Header("Camera Position")]
    [SerializeField] private Vector3 forwardOffset = new Vector3(0f, 2.5f, -6f);
    [SerializeField] private Vector3 reverseOffset = new Vector3(0f, 2.5f, 6f);
    
    [Header("Smoothing")]
    [SerializeField] private float positionSmoothing = 15f;
    [SerializeField] private float rotationSmoothing = 15f;
    [SerializeField] private float flipTransitionSpeed = 8f;
    
    [Header("Transform Rotation")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    
    [Header("Turn Camera Movement")]
    [SerializeField] private float turnCameraSensitivity = 2f;
    [SerializeField] private float turnCameraDistance = 1.5f;
    [SerializeField] private float turnCameraSmoothing = 3f;
    
    [Header("Reverse Detection")]
    [SerializeField] private float reverseThreshold = -0.5f;
    
    private bool isReversing = false;
    private Vector3 currentOffset;
    private float flipProgress = 0f;
    private float turnInput = 0f;
    private float smoothedTurnInput = 0f;

    void Start()
    {
        if (vehicle == null)
        {
            Debug.LogError("Vehicle transform not assigned!");
            return;
        }
        
        if (vehicleRb == null)
        {
            vehicleRb = vehicle.GetComponent<Rigidbody>();
        }
        
        currentOffset = forwardOffset;
    }

    void LateUpdate()
    {
        if (vehicle == null) return;
        
        // Get turn input
        GetTurnInput();
        
        // Check if vehicle is moving in reverse
        CheckReverseMovement();
        
        // Smoothly transition between forward and reverse camera positions
        UpdateCameraOffset();
        
        // Calculate desired camera position with turn offset
        Vector3 cameraOffset = currentOffset;
        cameraOffset.x += smoothedTurnInput * turnCameraDistance;
        Vector3 desiredPosition = vehicle.position + vehicle.TransformDirection(cameraOffset);
        
        // Smoothly move camera to desired position (no shake from high speed)
        transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothing * Time.deltaTime);
        
        // Calculate look direction
        Vector3 lookDirection = vehicle.position - transform.position;
        
        if (lookDirection != Vector3.zero)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection);
            
            // Apply rotation offset
            Quaternion baseRotationOffset = Quaternion.Euler(rotationOffset);
            desiredRotation *= baseRotationOffset;
            
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothing * Time.deltaTime);
        }
    }

    void CheckReverseMovement()
    {
        if (vehicleRb == null) return;
        
        // Get vehicle's local velocity (forward/backward direction)
        Vector3 localVelocity = vehicle.InverseTransformDirection(vehicleRb.linearVelocity);
        
        // Check if moving backwards
        bool shouldReverse = localVelocity.z < reverseThreshold;
        
        if (shouldReverse != isReversing)
        {
            isReversing = shouldReverse;
            flipProgress = 0f; // Reset flip animation
        }
    }

    void UpdateCameraOffset()
    {
        // Smoothly transition between forward and reverse positions
        if (flipProgress < 1f)
        {
            flipProgress += flipTransitionSpeed * Time.deltaTime;
            flipProgress = Mathf.Clamp01(flipProgress);
        }
        
        Vector3 targetOffset = isReversing ? reverseOffset : forwardOffset;
        currentOffset = Vector3.Lerp(
            isReversing ? forwardOffset : reverseOffset,
            targetOffset,
            flipProgress
        );
    }

    void GetTurnInput()
    {
        // Get horizontal input (A/D or Arrow Keys or Joystick)
        turnInput = Input.GetAxis("Horizontal");
        
        // Smoothly interpolate turn input for camera movement
        smoothedTurnInput = Mathf.Lerp(smoothedTurnInput, turnInput * turnCameraSensitivity, turnCameraSmoothing * Time.deltaTime);
    }
}