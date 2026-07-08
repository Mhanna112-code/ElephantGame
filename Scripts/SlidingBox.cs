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
    private int slideFrame;   // throttles the per-frame "what do I see" diagnostic

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

        // World-space half extents. NOTE: the old code used `boxCol.size * 0.5f`, which is the collider's
        // LOCAL size and ignores the object's scale. `bounds.extents` is the true world half-size, so the
        // cast/overlap volume now actually matches the box. Shrunk 5% so we don't self-trigger on a wall we
        // are already resting flush against, and so sliding PARALLEL along a rail does not false-stop.
        Vector3 center = boxCol.bounds.center;
        Vector3 halfExtents = boxCol.bounds.extents * 0.95f;
        float castDistance = move.magnitude + detectDistance;   // detectDistance is the look-ahead skin

        // -------------------------------------------------------------------------------------------------
        // TEST 1 - SWEEP: is there a blocker directly ahead within this frame's travel?
        // Use BoxCastAll (not BoxCast): plain BoxCast returns only the SINGLE nearest collider, so a nearer
        // non-blocker (the floor, a decoration) could hide a rail/box right behind it and we would slide on.
        // BoxCastAll lets us scan every collider in the swept volume and stop if ANY of them is a blocker.
        // -------------------------------------------------------------------------------------------------
        RaycastHit[] hits = Physics.BoxCastAll(center, halfExtents, slideDirection, transform.rotation, castDistance);
        foreach (RaycastHit h in hits)
        {
            if (IsSelf(h.collider))
                continue;

            if (IsBlocker(h.collider))
            {
                StopSliding($"SWEEP hit '{h.collider.name}' tag='{h.collider.tag}' layer={h.collider.gameObject.layer} dist={h.distance:F3}");
                return;
            }
        }

        // -------------------------------------------------------------------------------------------------
        // TEST 2 - OVERLAP BACKSTOP (the real rail fix):
        // Physics.BoxCast IGNORES colliders that already overlap the box at the start of the cast. The rails
        // are only ~0.12 units thick, and this kinematic box moves ~0.07 units/frame, so the moment it clips
        // into a rail the sweep stops seeing it and the box tunnels straight through - which is exactly the
        // "boxes don't collide with rails but look like they do" symptom. So before committing the move, we
        // check the box's NEXT position with an OverlapBox: if it would end up inside a blocker, stop here.
        // -------------------------------------------------------------------------------------------------
        Vector3 nextCenter = center + move;
        Collider[] overlaps = Physics.OverlapBox(nextCenter, halfExtents, transform.rotation);
        foreach (Collider c in overlaps)
        {
            if (IsSelf(c))
                continue;

            if (IsBlocker(c))
            {
                StopSliding($"OVERLAP at next pos with '{c.name}' tag='{c.tag}' layer={c.gameObject.layer} (tunnel guard)");
                return;
            }
        }

        // Nothing stopped us -> we are about to move. If this fix is WRONG (box still passes a rail), this
        // throttled log is how we find out why: it shows exactly which colliders the detection SAW this frame
        // and their tags. If a rail is passed but never appears here, the cast/overlap volume is missing it
        // (geometry/scale problem). If it appears here but IsBlocker skipped it, the tag/layer is the problem.
        if (debugLogs && (++slideFrame % 10 == 0))
        {
            string seen = "";
            foreach (RaycastHit h in hits)
                if (!IsSelf(h.collider)) seen += $" SWEEP[{h.collider.name}:{h.collider.tag}]";
            foreach (Collider c in overlaps)
                if (!IsSelf(c)) seen += $" OVERLAP[{c.name}:{c.tag}]";
            if (seen == "") seen = " (nothing in range)";
            Debug.Log($"[SlidingBox] '{name}' sliding dir={slideDirection} pos={transform.position} sees:{seen}", this);
        }

        transform.position += move;
    }

    // A collider counts as "self" if it is our own box collider or lives under this box in the hierarchy.
    // Keeps the box from stopping on its own colliders / children.
    bool IsSelf(Collider c)
    {
        return c == boxCol || c.transform.IsChildOf(transform);
    }

    // Central definition of "what stops a sliding box". This is the fix for box-vs-box: the old code only
    // stopped on other boxes via `(1 << layer) & obstacleLayers`, but obstacleLayers is EMPTY (0) on every
    // box in the scene, so that test was always false and boxes slid through each other. We now stop on the
    // Rail and SlidingBox TAGS directly (no inspector setup required), while still honouring obstacleLayers
    // if anyone chooses to set it. Trigger volumes (e.g. snap zones) are never blockers.
    bool IsBlocker(Collider c)
    {
        if (c.isTrigger)
            return false;

        if (c.CompareTag("Rail"))
            return true;

        if (c.CompareTag("SlidingBox"))
            return true;

        if (((1 << c.gameObject.layer) & obstacleLayers) != 0)
            return true;

        return false;
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
        slideFrame = 0;

        if (debugLogs)
            Debug.Log($"[SlidingBox] '{name}' START sliding dir={slideDirection} (pushed by '{collision.collider.name}')", this);
    }

    void StopSliding(string reason = "")
    {
        sliding = false;

        if (debugLogs)
            Debug.Log($"[SlidingBox] '{name}' STOP sliding (dir={slideDirection}). Reason: {reason}", this);
    }
}