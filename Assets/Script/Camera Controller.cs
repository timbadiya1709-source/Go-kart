using UnityEngine;

public class VehicleCamera : MonoBehaviour
{
    [Header("Camera Tilt Settings")]
    [SerializeField] private float tiltSensitivity = 15f; // How much to rotate on Y axis when turning
    [SerializeField] private float tiltSmoothing = 3f; // How smoothly to apply the tilt
    [SerializeField] private float maxTiltAngle = 8f; // Maximum tilt angle
    
    private float currentTiltAngle = 0f;
    private float targetTiltAngle = 0f;

    void LateUpdate()
    {
        // Get turn input
        float turnInput = Input.GetAxis("Horizontal");
        
        // Calculate target tilt angle based on turning
        targetTiltAngle = Mathf.Clamp(turnInput * tiltSensitivity, -maxTiltAngle, maxTiltAngle);
        
        // Smoothly apply the tilt
        float lerpFactor = Time.deltaTime * tiltSmoothing;
        currentTiltAngle = Mathf.Lerp(currentTiltAngle, targetTiltAngle, lerpFactor);
        
        // Apply rotation to camera around Y axis
        transform.localRotation = Quaternion.Euler(0f, currentTiltAngle, 0f);
    }
}