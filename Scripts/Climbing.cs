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

    private Transform currentGrabPoint;
    private TrunkSwing trunkSwing;
    private Rigidbody rb;

    private bool isGrabbing;
    private float ropeLength;

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
    [Header("Swing Physics")]
    public float ropeStrength = 2f;
    public float swingDamping = 0.15f;

    void FixedUpdate()
    {
        if (!isGrabbing || currentGrabPoint == null || trunkBase == null)
            return;

        Vector3 anchor = currentGrabPoint.position;
        Vector3 trunkPos = trunkBase.position;

        Vector3 ropeVector = trunkPos - anchor;
        float distance = ropeVector.magnitude;

        Vector3 direction = ropeVector.normalized;


        // Rope is stretched
        if (distance > ropeLength)
        {
            float stretch = distance - ropeLength;

            // Stronger pull back toward grab point
            rb.AddForce(
                -direction * stretch * ropeStrength,
                ForceMode.Force
            );


            // Remove only outward velocity
            float outwardVelocity = Vector3.Dot(rb.linearVelocity, direction);

            if (outwardVelocity > 0)
            {
                rb.linearVelocity -= direction * outwardVelocity * swingDamping;
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

        // Use the current distance between trunk base and grab point as the rope length
        if (trunkBase != null)
        {
            ropeLength = Vector3.Distance(trunkBase.position, point.position) + 0.5f;
        }

        // Stop player input fighting the grab
        if (playerController != null)
        {
            playerController.SetClimbing(true);
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