using UnityEngine;


/// <summary>
/// Component to orbit a transform around a Vector3.
/// </summary>
public class TransformOrbiter : MonoBehaviour
{
    public float defaultDampTime = 0.1f;
    public float distanceDampTime = 0.1f;

    public Vector3 orbitPoint = Vector3.zero;
    public Vector3 offset = Vector3.zero;

    public float distance = 2;

    public float Rotation
    {
        get { return rotation; }
        set
        {
            if(value == rotation) return;

            // Set the closest value for Rotation in the correct direction
            float diff = Mathf.Abs(value - rotation);
            if(diff > 180)
            {
                int change = 360 * Mathf.RoundToInt(diff / 360) + 1;

                value = value > rotation ? value - change : value + change;
            }

            rotation = value;
            MaintainRotationBounds();
        }
    }

    public float Pitch
    {
        get { return pitch; }
        set
        {
            pitch = Mathf.Clamp(value, -89, 89);
        }
    }

    public Vector3 CurrentOrbitPoint { get; private set; }
    public Vector3 CurrentOffset { get; private set; }
    public float CurrentDistance { get; private set; }
    public float CurrentRotation { get; private set; }
    public float CurrentPitch { get; private set; }

    // Velocity variables for smooth damping
    private Vector3 currentOrbitPointVelocity;
    private Vector3 currentOffsetVel;
    private float currentDistanceVel;
    private float currentRotationVel;
    private float currentPitchVel;

    private float rotation = 0;
    private float pitch = 40;


    /// <summary>
    /// Force the target values instantly without any smoothing.
    /// </summary>
    public void ForceTargetValues()
    {
        CurrentOrbitPoint = orbitPoint;
        CurrentDistance = distance;
        CurrentRotation = Rotation;
        CurrentPitch = Pitch;
        CurrentOffset = offset;
    }

    private void Update()
    {
        PerformSmoothDampOnAllValues();

        SetOrbitingTransformation();
    }

    /// <summary>
    /// Perform the smooth damp of all current values to target vale
    /// </summary>
    private void PerformSmoothDampOnAllValues()
    {
        CurrentOrbitPoint = SmoothDamp(CurrentOrbitPoint, orbitPoint, ref currentOrbitPointVelocity, defaultDampTime);
        CurrentDistance = SmoothDamp(CurrentDistance, distance, ref currentDistanceVel, distanceDampTime);
        CurrentRotation = SmoothDamp(CurrentRotation, rotation, ref currentRotationVel, defaultDampTime);
        CurrentPitch = SmoothDamp(CurrentPitch, pitch, ref currentPitchVel, defaultDampTime);
        CurrentOffset = SmoothDamp(CurrentOffset, offset, ref currentOffsetVel, defaultDampTime);
    }

    /// <summary>
    /// Performs the calculation to set the object at the correct position and Rotation based on 'current' values.
    /// </summary>
    private void SetOrbitingTransformation()
    {
        // Set camera distance
        var pos = new Vector3(0, 0, -CurrentDistance);

        // Rotate with 'current' values
        pos = Quaternion.Euler(CurrentPitch, CurrentRotation, 0) * pos;

        // Set position
        pos += CurrentOrbitPoint;
        transform.position = pos;

        // Set Rotation
        transform.LookAt(CurrentOrbitPoint);

        // Fix offset's values based on Rotation
        Vector3 rotatedOffset = Quaternion.AngleAxis(CurrentRotation, Vector3.up) * CurrentOffset;

        // Add to final position
        transform.position += rotatedOffset;
    }

    private void MaintainRotationBounds()
    {
        if(rotation < 540 && rotation > -540) return;

        int div = Mathf.FloorToInt(rotation / 360);

        int n = div * 360;
        rotation -= n;
        CurrentRotation -= n;
    }

    private float SmoothDamp(float current, float target, ref float velocity, float damp)
    {
        return Mathf.SmoothDamp(current, target, ref velocity, damp, 50000, Time.unscaledDeltaTime);
    }

    private Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 velocity, float damp)
    {
        return Vector3.SmoothDamp(current, target, ref velocity, damp, 50000, Time.unscaledDeltaTime);
    }
}