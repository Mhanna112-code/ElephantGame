using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class SlidingBox : MonoBehaviour
{
    [Header("Sliding")]
    public float slideSpeed = 6f;

    [Header("Wall Detection")]
    public float detectDistance = 0.05f;
    public LayerMask obstacleLayers;

    [Header("Debug")]
    public bool debugLogs = true;

    private Rigidbody rb;
    private BoxCollider boxCol;

    private bool sliding = false;
    private Vector3 slideDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        boxCol = GetComponent<BoxCollider>();

        rb.isKinematic = true;
        rb.constraints =
            RigidbodyConstraints.FreezeRotation |
            RigidbodyConstraints.FreezePositionZ;
    }

    void Update()
    {
        if (!sliding)
            return;

        Vector3 move = slideDirection * slideSpeed * Time.deltaTime;

        // 🔥 IMPORTANT: cast from CURRENT position toward NEXT position
        Vector3 origin = boxCol.bounds.center;
        Vector3 halfExtents = boxCol.size * 0.5f;

        RaycastHit hit;

        bool willHit = Physics.BoxCast(
            origin,
            halfExtents * 0.95f,
            slideDirection,
            out hit,
            transform.rotation,
            move.magnitude + 0.05f
        );

        if (willHit)
        {
            if (hit.collider.CompareTag("Rail"))
            {
                if (debugLogs)
                    Debug.Log("STOPPED by Rail: " + hit.collider.name);

                StopSliding();
                return;
            }

            if (((1 << hit.collider.gameObject.layer) & obstacleLayers) != 0)
            {
                if (debugLogs)
                    Debug.Log("STOPPED by obstacle: " + hit.collider.name);

                StopSliding();
                return;
            }
        }

        transform.position += move;
    }
    void OnCollisionStay(Collision collision)
    {
        if (sliding)
            return;

        // 🚫 CRITICAL: Ignore anything that is NOT the player
        Rigidbody otherRb = collision.rigidbody;

        if (otherRb == null)
            return;

        if (!otherRb.CompareTag("Player"))
            return;

        // 🚫 Ignore trunk bones / child colliders already included in physics
        // (ensures only real player root drives interaction)
        if (collision.transform.root.CompareTag("SlidingBox"))
            return;

        if (!Input.GetKey(KeyCode.E))
            return;

        // 🔥 ONLY use true contact normal (not velocity)
        ContactPoint contact = collision.GetContact(0);

        Vector3 pushDir = contact.normal;

        // lock to axis
        if (Mathf.Abs(pushDir.x) > Mathf.Abs(pushDir.z))
        {
            slideDirection = new Vector3(Mathf.Sign(pushDir.x), 0f, 0f);
        }
        else
        {
            slideDirection = new Vector3(0f, 0f, Mathf.Sign(pushDir.z));
        }

        // 🚫 FINAL SAFETY CHECK: ensure player is actually pushing INTO the box
        float pushDot = Vector3.Dot(pushDir, collision.rigidbody.linearVelocity.normalized);

        if (pushDot <= 0f)
            return;

        sliding = true;

        if (debugLogs)
            Debug.Log("Started sliding ONLY from player force: " + slideDirection);
    }

    void StopSliding()
    {
        sliding = false;

        if (debugLogs)
            Debug.Log("Sliding stopped.");
    }
}