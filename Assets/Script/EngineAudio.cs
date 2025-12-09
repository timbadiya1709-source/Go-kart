using UnityEngine;

public class EngineAudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource idleSource;
    [SerializeField] private AudioSource lowSource;
    [SerializeField] private AudioSource midSource;
    [SerializeField] private AudioSource highSource;

    [Header("RPM Settings")]
    [SerializeField] private float minRPM = 500f;
    [SerializeField] private float maxRPM = 8000f;

    [Header("Gear-Based Blending")]
    [SerializeField] private float gearBlendWidth = 0.20f;

    [Header("Smoothing")]
    [SerializeField] private float rpmSmoothing = 6f;
    [SerializeField] private float volumeSmoothing = 8f;
    [SerializeField] private float gearTransitionSmoothing = 5f;
    [SerializeField] private float masterVolume = 1f;

    [Header("References")]
    [SerializeField] private KartController kart;

    private float rpmFiltered;
    private float targetIdleVol, targetLowVol, targetMidVol, targetHighVol;
    private int previousGear = -1;
    private float gearShiftAlpha = 1f;

    private void Start()
    {
        if (!idleSource || !lowSource || !midSource || !highSource || !kart)
        {
            Debug.LogWarning("EngineAudioController: Assign all AudioSources and KartController.");
            enabled = false;
            return;
        }

        idleSource.loop = true;
        lowSource.loop = true;
        midSource.loop = true;
        highSource.loop = true;

        idleSource.pitch = 1f;
        lowSource.pitch = 1f;
        midSource.pitch = 1f;
        highSource.pitch = 1f;

        idleSource.Play();
        lowSource.Play();
        midSource.Play();
        highSource.Play();

        rpmFiltered = minRPM;
        previousGear = GetCurrentGear();
    }

    private void Update()
    {
        int currentGear = GetCurrentGear();

        // Detect gear shift
        if (currentGear != previousGear)
        {
            gearShiftAlpha = 0f;
            previousGear = currentGear;
            Debug.Log("Gear shifted to: " + currentGear);
        }

        // Smooth gear transition
        gearShiftAlpha = Mathf.Lerp(gearShiftAlpha, 1f, Time.deltaTime * gearTransitionSmoothing);

        // Calculate RPM
        float motorInput = Mathf.Clamp01(Mathf.Abs(kart.GetSmoothedMotorInput()));
        float pseudoRPM = Mathf.Lerp(minRPM, maxRPM, Mathf.Clamp01(motorInput * kart.gearRatio));

        rpmFiltered = Mathf.Lerp(rpmFiltered, pseudoRPM, Time.deltaTime * rpmSmoothing);

        float rNormalized = Mathf.InverseLerp(minRPM, maxRPM, rpmFiltered);

        // Calculate volumes based on gear and RPM
        CalculateGearVolumes(rNormalized, currentGear);

        // Normalize and apply master volume
        float sum = targetIdleVol + targetLowVol + targetMidVol + targetHighVol;
        float norm = sum > 1e-3f ? (masterVolume / sum) : 0f;

        // Apply volumes with smoothing
        idleSource.volume = Mathf.Lerp(idleSource.volume, targetIdleVol * norm, Time.deltaTime * volumeSmoothing);
        lowSource.volume = Mathf.Lerp(lowSource.volume, targetLowVol * norm, Time.deltaTime * volumeSmoothing);
        midSource.volume = Mathf.Lerp(midSource.volume, targetMidVol * norm, Time.deltaTime * volumeSmoothing);
        highSource.volume = Mathf.Lerp(highSource.volume, targetHighVol * norm, Time.deltaTime * volumeSmoothing);
    }

    private void CalculateGearVolumes(float rpmNorm, int gear)
    {
        // Define audio layer strength for each gear
        float idleStrength, lowStrength, midStrength, highStrength;

        switch (gear)
        {
            case 1: // 0-20 km/h
                idleStrength = Bell(rpmNorm, 0.15f, gearBlendWidth) + 0.2f;
                lowStrength = Bell(rpmNorm, 0.30f, gearBlendWidth) + 0.3f;
                midStrength = Bell(rpmNorm, 0.50f, gearBlendWidth) * 0.5f;
                highStrength = 0.1f;
                break;

            case 2: // 20-50 km/h
                idleStrength = Bell(rpmNorm, 0.15f, gearBlendWidth) * 0.3f + 0.1f;
                lowStrength = Bell(rpmNorm, 0.35f, gearBlendWidth) + 0.2f;
                midStrength = Bell(rpmNorm, 0.55f, gearBlendWidth) + 0.2f;
                highStrength = Bell(rpmNorm, 0.70f, gearBlendWidth) * 0.4f + 0.1f;
                break;

            case 3: // 50-90 km/h
                idleStrength = 0.05f;
                lowStrength = Bell(rpmNorm, 0.35f, gearBlendWidth) * 0.3f + 0.05f;
                midStrength = Bell(rpmNorm, 0.60f, gearBlendWidth) + 0.2f;
                highStrength = Bell(rpmNorm, 0.75f, gearBlendWidth) + 0.2f;
                break;

            case 4: // 90-140 km/h
                idleStrength = 0.05f;
                lowStrength = 0.05f;
                midStrength = Bell(rpmNorm, 0.55f, gearBlendWidth) * 0.3f + 0.05f;
                highStrength = Bell(rpmNorm, 0.80f, gearBlendWidth) + 0.2f;
                break;

            case 5: // 140+ km/h
                idleStrength = 0.05f;
                lowStrength = 0.05f;
                midStrength = Bell(rpmNorm, 0.60f, gearBlendWidth) * 0.2f + 0.05f;
                highStrength = Bell(rpmNorm, 0.85f, gearBlendWidth) + 0.2f;
                break;

            default:
                idleStrength = lowStrength = midStrength = highStrength = 0.1f;
                break;
        }

        // Clamp to max 1.0
        idleStrength = Mathf.Clamp01(idleStrength);
        lowStrength = Mathf.Clamp01(lowStrength);
        midStrength = Mathf.Clamp01(midStrength);
        highStrength = Mathf.Clamp01(highStrength);

        // Apply smooth transition during gear shifts
        targetIdleVol = Mathf.Lerp(targetIdleVol, idleStrength, gearShiftAlpha * 0.15f);
        targetLowVol = Mathf.Lerp(targetLowVol, lowStrength, gearShiftAlpha * 0.15f);
        targetMidVol = Mathf.Lerp(targetMidVol, midStrength, gearShiftAlpha * 0.15f);
        targetHighVol = Mathf.Lerp(targetHighVol, highStrength, gearShiftAlpha * 0.15f);
    }

    private int GetCurrentGear()
    {
        float speed = kart.currentSpeedKmh;

        if (speed < 20f) return 1;
        else if (speed < 50f) return 2;
        else if (speed < 90f) return 3;
        else if (speed < 140f) return 4;
        else return 5;
    }

    private float Bell(float x, float center, float width)
    {
        float sigma = Mathf.Max(0.0001f, width * 0.35f);
        float dx = (x - center);
        return Mathf.Clamp01(Mathf.Exp(-(dx * dx) / (2f * sigma * sigma)));
    }
}