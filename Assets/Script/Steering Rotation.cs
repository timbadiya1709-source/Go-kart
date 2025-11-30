using UnityEngine;

public class SteeringRotation : MonoBehaviour
{
    [Header("Steering Components")]
    [SerializeField] private Transform steeringComponent1;
    [SerializeField] private Transform steeringComponent2;
    
    [Header("Steering Settings")]
    [SerializeField] private float maxSteerAngle = 30f;
    [SerializeField] private float steerSensitivity = 1f;
    [SerializeField] private float steerSmoothing = 5f;
    
    private float currentSteerInput = 0f;
    private float smoothedSteerInput = 0f;

    void Update()
    {
        // Get steering input
        GetSteeringInput();
        
        // Apply rotation to steering components
        ApplySteeringRotation();
    }

    void GetSteeringInput()
    {
        // Get horizontal input (A/D or Arrow Keys or Joystick)
        currentSteerInput = Input.GetAxis("Horizontal");
        
        // Smoothly interpolate steering input
        smoothedSteerInput = Mathf.Lerp(smoothedSteerInput, currentSteerInput, steerSmoothing * Time.deltaTime);
    }

    void ApplySteeringRotation()
    {
        // Calculate desired rotation angle based on input
        float targetAngle = smoothedSteerInput * maxSteerAngle;
        
        // Apply rotation to first steering component on X axis
        if (steeringComponent1 != null)
        {
            steeringComponent1.localRotation = Quaternion.Euler(-targetAngle, 0f, 0f);
        }
        
        // Apply rotation to second steering component on X axis
        if (steeringComponent2 != null)
        {
            steeringComponent2.localRotation = Quaternion.Euler(-targetAngle, 0f, 0f);
        }
    }
}
