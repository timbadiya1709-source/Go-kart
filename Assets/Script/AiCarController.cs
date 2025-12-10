using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AIKartController : MonoBehaviour
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
    private float motorForce = 10000f;
    private float brakeForce = 80000f;
    private float maxSteerAngle = 30f;
    private float maxSpeed = 100f;
    private float engineResponseTime = 0.25f;
    private float brakingDecelerationTime = 2.5f;

    [Header("Traction Control")]
    private float maxWheelRPM = 8000f;
    private float tractionControlFactor = 0.7f;

    [Header("AI Settings")]
    [SerializeField] private float waypointDetectionRadius = 5f;
    [SerializeField] private float breakpointBrakingDistance = 15f;
    [SerializeField] private float breakpointMinSpeed = 15f; // Minimum speed when braking (prevents full stop)
    [SerializeField] private float steeringSpeed = 3f;
    [SerializeField] private string waypointTag = "Waypoint";
    [SerializeField] private string breakpointTag = "Breakpoint";
    [SerializeField] private string startFinishWaypointName = "Waypoint1"; // Name of start/finish waypoint
    [SerializeField] private int totalLaps = 6;
    [SerializeField] private bool debugMode = true;

    // AI State
    private Transform currentWaypoint;
    private Transform activeBreakpoint;
    private int currentLap = 0;
    private bool raceFinished = false;
    private bool hasPassedStartLine = false;
    
    private float currentSteerAngle;
    private float currentBrakeForce;
    private float targetMotorInput = 0f;
    private float smoothedMotorInput = 0f;
    private float aiThrottle = 1f;
    
    private Rigidbody rb;
    public float currentSpeedKmh;
    public float gearRatio = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        FindNextWaypoint();
        currentLap = 1; // Start at lap 1
        Debug.Log("AI Race Started - Target: " + totalLaps + " laps");
    }

    private void FixedUpdate()
    {
        if (raceFinished)
        {
            StopKart();
            return;
        }

        CalculateSpeed();
        CheckLapCompletion();
        AILogic();
        HandleMotor();
        HandleSteering();
        UpdateWheelVisuals();
    }

    private void CalculateSpeed()
    {
        currentSpeedKmh = rb.linearVelocity.magnitude * 3.6f;
    }

    private void CheckLapCompletion()
    {
        if (currentWaypoint == null) return;

        // Check if we reached the start/finish waypoint
        if (currentWaypoint.name == startFinishWaypointName)
        {
            float distanceToFinish = Vector3.Distance(transform.position, currentWaypoint.position);
            
            if (distanceToFinish <= waypointDetectionRadius && !hasPassedStartLine)
            {
                hasPassedStartLine = true;
                currentLap++;
                
                if (debugMode) Debug.Log("LAP COMPLETED! Now on lap: " + currentLap + "/" + totalLaps);
                
                if (currentLap > totalLaps)
                {
                    raceFinished = true;
                    Debug.Log("RACE FINISHED! Completed " + totalLaps + " laps!");
                }
            }
        }
        else
        {
            // Reset the start line flag when away from it
            hasPassedStartLine = false;
        }
    }

    private void AILogic()
    {
        // Check if reached current waypoint
        if (currentWaypoint != null)
        {
            float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint.position);
            
            if (distanceToWaypoint <= waypointDetectionRadius)
            {
                if (debugMode) Debug.Log("Reached waypoint: " + currentWaypoint.name);
                FindNextWaypoint();
            }
        }

        // Handle breakpoint detection and braking
        HandleBreakpointLogic();
    }

    private void HandleBreakpointLogic()
    {
        GameObject[] breakpoints = GameObject.FindGameObjectsWithTag(breakpointTag);
        
        Transform nearestBreakpointAhead = null;
        float closestDistance = Mathf.Infinity;

        // Find nearest breakpoint AHEAD of the kart
        foreach (GameObject breakpoint in breakpoints)
        {
            Vector3 directionToBreakpoint = breakpoint.transform.position - transform.position;
            float distanceToBreakpoint = directionToBreakpoint.magnitude;
            
            // Check if breakpoint is in front (dot product check)
            float dotProduct = Vector3.Dot(transform.forward, directionToBreakpoint.normalized);

            // Only consider breakpoints in front of us
            if (dotProduct > 0.5f && distanceToBreakpoint < closestDistance)
            {
                closestDistance = distanceToBreakpoint;
                nearestBreakpointAhead = breakpoint.transform;
            }
        }

        // DECISION LOGIC: Should we brake or accelerate?
        if (nearestBreakpointAhead != null && closestDistance <= breakpointBrakingDistance)
        {
            // Breakpoint detected ahead - APPLY BRAKES
            activeBreakpoint = nearestBreakpointAhead;
            
            // Reduce throttle based on distance (closer = less throttle)
            float brakingIntensity = 1f - (closestDistance / breakpointBrakingDistance);
            
            // Apply partial throttle to maintain minimum speed
            if (currentSpeedKmh > breakpointMinSpeed)
            {
                aiThrottle = 0f; // Full brake
                if (debugMode && activeBreakpoint != null) 
                    Debug.Log("Braking for: " + activeBreakpoint.name + " | Distance: " + closestDistance.ToString("F1") + "m | Speed: " + currentSpeedKmh.ToString("F1"));
            }
            else
            {
                aiThrottle = 0.3f; // Light throttle to prevent stopping
                if (debugMode) Debug.Log("Maintaining minimum speed at breakpoint: " + currentSpeedKmh.ToString("F1"));
            }
        }
        else
        {
            // NO breakpoint ahead OR passed it - FULL ACCELERATION
            if (activeBreakpoint != null && debugMode)
            {
                Debug.Log("Passed breakpoint: " + activeBreakpoint.name + " - RESUMING ACCELERATION");
            }
            
            activeBreakpoint = null;
            aiThrottle = 1f; // Full throttle
        }
    }

    private void FindNextWaypoint()
    {
        GameObject[] waypoints = GameObject.FindGameObjectsWithTag(waypointTag);
        
        if (waypoints.Length == 0)
        {
            Debug.LogWarning("No waypoints found with tag: " + waypointTag);
            return;
        }

        Transform nearestWaypoint = null;
        float shortestDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        // Find nearest waypoint ahead
        foreach (GameObject waypoint in waypoints)
        {
            // Skip current waypoint
            if (currentWaypoint != null && waypoint.transform == currentWaypoint)
                continue;

            Vector3 directionToWaypoint = waypoint.transform.position - currentPosition;
            float distanceToWaypoint = directionToWaypoint.magnitude;
            float dotProduct = Vector3.Dot(transform.forward, directionToWaypoint.normalized);

            // Prioritize waypoints ahead
            if (dotProduct > 0.3f && distanceToWaypoint < shortestDistance)
            {
                shortestDistance = distanceToWaypoint;
                nearestWaypoint = waypoint.transform;
            }
        }

        // Fallback: if no waypoint ahead, find absolute nearest (for loop completion)
        if (nearestWaypoint == null)
        {
            shortestDistance = Mathf.Infinity;
            foreach (GameObject waypoint in waypoints)
            {
                float distance = Vector3.Distance(currentPosition, waypoint.transform.position);
                if (distance < shortestDistance && waypoint.transform != currentWaypoint)
                {
                    shortestDistance = distance;
                    nearestWaypoint = waypoint.transform;
                }
            }
        }

        currentWaypoint = nearestWaypoint;
        if (debugMode && currentWaypoint != null) 
            Debug.Log("New target waypoint: " + currentWaypoint.name + " | Distance: " + shortestDistance.ToString("F1") + "m");
    }

    private void HandleMotor()
    {
        targetMotorInput = aiThrottle;

        float lerpSpeed = Time.fixedDeltaTime / engineResponseTime;
        smoothedMotorInput = Mathf.Lerp(smoothedMotorInput, targetMotorInput, lerpSpeed);

        // Gearbox system
        if (currentSpeedKmh < 20f) gearRatio = 3.5f;
        else if (currentSpeedKmh < 50f) gearRatio = 2.5f;
        else if (currentSpeedKmh < 90f) gearRatio = 1.8f;
        else if (currentSpeedKmh < 140f) gearRatio = 1.2f;
        else gearRatio = 0.8f;

        // Motor force scaling
        float speedRatio = currentSpeedKmh / maxSpeed;
        float speedFactor = Mathf.Max(0.1f, 1f - Mathf.Pow(speedRatio, 2f));
        float appliedMotorForce = motorForce * gearRatio * speedFactor;

        // Don't apply zero torque at low speeds if we want to accelerate
        if (currentSpeedKmh < 0.5f && aiThrottle > 0f)
        {
            appliedMotorForce = motorForce * 3.5f; // Boost from standstill
        }

        // Traction control
        if (Mathf.Abs(rearLeftCollider.rpm) > maxWheelRPM || Mathf.Abs(rearRightCollider.rpm) > maxWheelRPM)
        {
            appliedMotorForce *= tractionControlFactor;
        }

        // Apply motor torque
        rearLeftCollider.motorTorque = smoothedMotorInput * appliedMotorForce;
        rearRightCollider.motorTorque = smoothedMotorInput * appliedMotorForce;

        // Braking logic - only brake hard if speed is above minimum
        if (aiThrottle < 0.5f && currentSpeedKmh > breakpointMinSpeed)
        {
            currentBrakeForce = brakeForce;
        }
        else if (aiThrottle == 0f && currentSpeedKmh > 1f)
        {
            float brakingForce = (rb.mass * currentSpeedKmh) / (3.6f * brakingDecelerationTime);
            currentBrakeForce = brakingForce;
        }
        else
        {
            currentBrakeForce = 0f;
        }

        // Apply brakes
        frontLeftCollider.brakeTorque = currentBrakeForce;
        frontRightCollider.brakeTorque = currentBrakeForce;
        rearLeftCollider.brakeTorque = currentBrakeForce;
        rearRightCollider.brakeTorque = currentBrakeForce;
    }

    private void HandleSteering()
    {
        if (currentWaypoint == null) return;

        Vector3 relativeVector = transform.InverseTransformPoint(currentWaypoint.position);
        float newSteerAngle = (relativeVector.x / relativeVector.magnitude) * maxSteerAngle;

        currentSteerAngle = Mathf.Lerp(currentSteerAngle, newSteerAngle, Time.fixedDeltaTime * steeringSpeed);

        frontLeftCollider.steerAngle = currentSteerAngle;
        frontRightCollider.steerAngle = currentSteerAngle;
    }

    private void StopKart()
    {
        // Gradually stop the kart
        aiThrottle = 0f;
        currentBrakeForce = brakeForce * 2f;
        
        frontLeftCollider.brakeTorque = currentBrakeForce;
        frontRightCollider.brakeTorque = currentBrakeForce;
        rearLeftCollider.brakeTorque = currentBrakeForce;
        rearRightCollider.brakeTorque = currentBrakeForce;
        
        rearLeftCollider.motorTorque = 0f;
        rearRightCollider.motorTorque = 0f;
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

    private void OnDrawGizmos()
    {
        if (!debugMode) return;

        // Current waypoint
        if (currentWaypoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentWaypoint.position);
            Gizmos.DrawWireSphere(currentWaypoint.position, waypointDetectionRadius);
        }

        // Active breakpoint
        if (activeBreakpoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, activeBreakpoint.position);
            Gizmos.DrawWireSphere(activeBreakpoint.position, 2f);
        }

        // Brake indicator
        if (aiThrottle < 0.5f)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 3f);
        }

        // Race finished indicator
        if (raceFinished)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 5f, Vector3.one * 2f);
        }
    }

    public float GetSpeedKmh() => currentSpeedKmh;
    public float GetSmoothedMotorInput() => smoothedMotorInput;
    public int GetCurrentLap() => currentLap;
    public bool IsRaceFinished() => raceFinished;
}