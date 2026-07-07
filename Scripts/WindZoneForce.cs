using UnityEngine;

// WindZoneForce - an updraft VOLUME. While a rigidbody (the player) is inside this zone's
// trigger collider, it gets a continuous wind force (windDirection * windStrength).
//
// Previous version bug: it applied `AddForce(Vector3.up * 500f, ForceMode.Impulse)` EVERY
// FixedUpdate, unconditionally, ignoring its own windDirection/windStrength and with no zone
// check - which rocketed the player to ~Y=1220. This version fixes all of that:
//   - continuous ForceMode.Force (not a per-frame impulse),
//   - respects windDirection / windStrength,
//   - only pushes while the body is actually inside the trigger,
//   - identifies the player by the attached rigidbody's tag (handles the trunk-bone rig).
//
// Setup: the collider on this object must have "Is Trigger" = true so the player passes through
// the volume. (Reset() sets that automatically when the component is first added.)
[RequireComponent(typeof(Collider))]
public class WindZoneForce : MonoBehaviour
{
    [Header("Wind")]
    public Vector3 windDirection = Vector3.up;
    public float windStrength = 50f;
    public bool onlyPlayer = true;   // only push rigidbodies tagged "Player"

    [Header("Diagnostics")]
    public bool debugLogs = true;

    void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    void OnTriggerStay(Collider other)
    {
        // attachedRigidbody resolves child colliders (trunk bones / capsule) to the player root.
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;
        if (onlyPlayer && !rb.CompareTag("Player")) return;

        Vector3 dir = windDirection.sqrMagnitude > 0.0001f ? windDirection.normalized : Vector3.up;
        rb.AddForce(dir * windStrength, ForceMode.Force);   // continuous, NOT per-frame impulse

        if (debugLogs && Time.frameCount % 30 == 0)
            Debug.Log($"[WindZone] pushing '{rb.name}' force={dir * windStrength} " +
                      $"(dir={windDirection} strength={windStrength}) vel={rb.linearVelocity}", this);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.9f);
        Vector3 d = windDirection.sqrMagnitude > 0.0001f ? windDirection.normalized : Vector3.up;
        Gizmos.DrawRay(transform.position, d * 2.5f);
    }
}
