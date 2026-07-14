using UnityEngine;

public class MovingBoxSpline : MonoBehaviour
{
    public Transform[] points;
    public float speed = 2f;

    [Header("Rail Alignment")]
    [Tooltip("Layer the rail's box colliders are on. The cart snaps to the top surface of whatever this hits.")]
    public LayerMask railLayer;
    [Tooltip("Distance from the cart's pivot down to the bottom of its wheels. Keeps the wheels flush on the rail surface instead of the pivot itself.")]
    public float wheelBottomOffset = 0f;
    [Tooltip("How far up/down from the path to search for the rail surface.")]
    public float railCheckDistance = 5f;
    [Tooltip("Manual fine-tune added on top of the raycast result, in case the rail surface isn't perfectly flat.")]
    public float railSurfaceOffset = 0f;
    [Tooltip("Radius of the probe used to find the rail surface. Wider than a thin raycast so small seams between rail segments don't get missed.")]
    public float railProbeRadius = 0.15f;

    private bool hasLastRailHeight;
    private float lastRailHeight;

    [Header("Wheels")]
    public Transform[] wheels;
    public float wheelRadius = 0.3f;
    public Vector3 wheelRotationAxis = Vector3.right;

    [Header("Slope Matching")]
    [Tooltip("How far ahead along the path to sample when measuring the rail's slope.")]
    public float pitchSampleDistance = 0.05f;

    public float CurrentPitch { get; private set; }

    private float t;

    void Update()
    {
        if (points.Length < 4)
            return;

        t += speed * Time.deltaTime;

        // Loop path
        float curveTime = t % (points.Length - 3);

        int segment = Mathf.FloorToInt(curveTime);

        float localT = curveTime - segment;

        Vector3 p0 = points[segment].position;
        Vector3 p1 = points[segment + 1].position;
        Vector3 p2 = points[segment + 2].position;
        Vector3 p3 = points[segment + 3].position;

        Vector3 newPosition = CatmullRom(p0, p1, p2, p3, localT);
        newPosition.y = SnapToRailSurface(newPosition);

        float aheadT = Mathf.Clamp01(localT + pitchSampleDistance);
        Vector3 aheadPosition = CatmullRom(p0, p1, p2, p3, aheadT);
        aheadPosition.y = SnapToRailSurface(aheadPosition);

        UpdatePitch(newPosition, aheadPosition);

        float distanceTraveled = (newPosition - transform.position).magnitude;

        transform.position = newPosition;

        RotateWheels(distanceTraveled);
    }

    void UpdatePitch(Vector3 current, Vector3 ahead)
    {
        Vector3 toAhead = ahead - current;
        Vector3 flatDirection = new Vector3(toAhead.x, 0f, toAhead.z);

        if (flatDirection.sqrMagnitude < 0.0001f)
            return;

        CurrentPitch = Mathf.Atan2(toAhead.y, flatDirection.magnitude) * Mathf.Rad2Deg;
    }

    [ContextMenu("Auto-Calculate Wheel Bottom Offset")]
    void AutoCalculateWheelBottomOffset()
    {
        if (wheels == null || wheels.Length == 0)
        {
            Debug.LogWarning("Assign the wheels array before calculating.");
            return;
        }

        float lowestWheelBottom = float.MaxValue;
        bool foundRenderer = false;

        foreach (Transform wheel in wheels)
        {
            if (wheel == null)
                continue;

            Renderer wheelRenderer = wheel.GetComponentInChildren<Renderer>();

            if (wheelRenderer == null)
                continue;

            // Measure the wheel's actual mesh bounds rather than assuming its pivot
            // sits at the wheel's center, since that assumption may not hold.
            foundRenderer = true;
            lowestWheelBottom = Mathf.Min(lowestWheelBottom, wheelRenderer.bounds.min.y);
        }

        if (!foundRenderer)
        {
            Debug.LogWarning("No Renderer found on the assigned wheels; can't measure their geometry.");
            return;
        }

        wheelBottomOffset = transform.position.y - lowestWheelBottom;

        Debug.Log($"Wheel Bottom Offset set to {wheelBottomOffset}", this);
    }

    float SnapToRailSurface(Vector3 pathPosition)
    {
        Vector3 castOrigin = pathPosition + Vector3.up * (railCheckDistance * 0.5f);

        // SphereCast instead of a thin ray so small seams between adjacent rail
        // segments don't get missed and cause the cart to snap between heights.
        if (Physics.SphereCast(castOrigin, railProbeRadius, Vector3.down, out RaycastHit hit, railCheckDistance, railLayer))
        {
            float height = hit.point.y + wheelBottomOffset + railSurfaceOffset;
            lastRailHeight = height;
            hasLastRailHeight = true;
            return height;
        }

        // A momentary miss falls back to the last known good height rather than
        // the raw path height, which could be a completely different value.
        return hasLastRailHeight ? lastRailHeight : pathPosition.y;
    }

    void RotateWheels(float distanceTraveled)
    {
        if (wheels == null || wheels.Length == 0 || wheelRadius <= 0f)
            return;

        float angle = (distanceTraveled / wheelRadius) * Mathf.Rad2Deg;

        foreach (Transform wheel in wheels)
        {
            if (wheel != null)
                wheel.Rotate(wheelRotationAxis, angle, Space.Self);
        }
    }


    Vector3 CatmullRom(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        float t)
    {
        return 0.5f *
        (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f*p0 - 5f*p1 + 4f*p2 - p3) * t*t +
            (-p0 + 3f*p1 - 3f*p2 + p3) * t*t*t
        );
    }
}