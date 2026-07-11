using UnityEngine;

public class TrunkClimb : MonoBehaviour
{
    [Header("References")]
    public Transform trunkTip;          // end of trunk for detection
    public Transform trunkBase;         // MarcH-ElephantTrunk (where trunk starts on the body)
    public PlayerController playerController;

    [Header("Grab")]
    public float grabDistance = 3f;
    public KeyCode grabKey = KeyCode.E;

    [Header("Range")]
    [Tooltip("Max distance the player can be from the grab point while anchored. Movement is free in every direction up to this range - only distance is capped.")]
    public float maxGrabRange = 4f;

    private Transform currentGrabPoint;
    private TrunkSwing trunkSwing;
    private Rigidbody rb;

    private bool isGrabbing;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        trunkSwing = GetComponent<TrunkSwing>();

        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(grabKey))
        {
            TryGrab();
        }

        if (Input.GetKeyUp(grabKey))
        {
            Release();
        }
    }

    void FixedUpdate()
    {
        if (!isGrabbing || currentGrabPoint == null || rb == null)
            return;

        Vector3 anchor = currentGrabPoint.position;
        Vector3 offset = rb.position - anchor;
        float distance = offset.magnitude;

        if (distance > maxGrabRange)
        {
            // Hard clamp onto the max-range sphere - free movement in every
            // direction, this only ever stops the player from getting farther
            // from the anchor than maxGrabRange. No spring force, no jitter.
            Vector3 outward = offset.normalized;
            rb.position = anchor + outward * maxGrabRange;

            float outwardSpeed = Vector3.Dot(rb.linearVelocity, outward);
            if (outwardSpeed > 0)
            {
                rb.linearVelocity -= outward * outwardSpeed;
            }
        }
    }

    void TryGrab()
    {
        if (trunkTip == null)
            return;

        Collider[] hits = Physics.OverlapSphere(trunkTip.position, grabDistance);

        float closestDistance = Mathf.Infinity;
        Transform closestPoint = null;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Climbable"))
                continue;

            Transform grabPoint = GetClosestGrabPoint(hit.transform);
            if (grabPoint == null)
                continue;

            float d = Vector3.Distance(trunkTip.position, grabPoint.position);

            if (d < closestDistance)
            {
                closestDistance = d;
                closestPoint = grabPoint;
            }
        }

        if (closestPoint != null)
        {
            Grab(closestPoint);
        }
    }

    Transform GetClosestGrabPoint(Transform climbable)
    {
        Transform closest = null;
        float best = Mathf.Infinity;

        foreach (Transform t in climbable.GetComponentsInChildren<Transform>())
        {
            if (!t.name.StartsWith("GrabPoint"))
                continue;

            float d = Vector3.Distance(trunkTip.position, t.position);

            if (d < best)
            {
                best = d;
                closest = t;
            }
        }

        return closest;
    }

    void Grab(Transform point)
    {
        currentGrabPoint = point;
        isGrabbing = true;

        // Set the IK target to the grab point and keep it there
        if (trunkSwing != null)
        {
            trunkSwing.isGrabbing = true;
            trunkSwing.currentGrabPoint = point;
        }

        // Stop player input fighting the grab
        if (playerController != null)
        {
            playerController.SetClimbing(true);
        }

        // No gravity while anchored - PlayerController.Move() takes over
        // full XYZ control so the player can move in any direction.
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
        }

        Debug.Log("Trunk grabbed: " + point.name);
    }

    void Release()
    {
        isGrabbing = false;
        currentGrabPoint = null;

        if (trunkSwing != null)
        {
            trunkSwing.isGrabbing = false;
            trunkSwing.currentGrabPoint = null;
        }

        if (playerController != null)
        {
            playerController.SetClimbing(false);
        }

        if (rb != null)
        {
            rb.useGravity = true;
        }

        Debug.Log("Trunk released");
    }

    void OnDrawGizmos()
    {
        if (trunkTip == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(trunkTip.position, grabDistance);

        if (currentGrabPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(trunkTip.position, currentGrabPoint.position);
        }
    }
}