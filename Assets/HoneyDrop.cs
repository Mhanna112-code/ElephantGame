using UnityEngine;

public class HoneyDrop : MonoBehaviour
{
    public float fallSpeed = 5f;

    private bool landed = false;
    private float spawnY;
    private float airtime;
    private int contactLogs;
    private bool watchdogFired;


    void Start()
    {
        spawnY = transform.position.y;
        Rigidbody rb = GetComponent<Rigidbody>();
        Collider col = GetComponentInChildren<Collider>();
        Debug.Log($"[Honey] spawned '{name}' at {transform.position} rb={(rb != null ? (rb.isKinematic ? "kinematic" : "dynamic") : "MISSING")} " +
                  $"collider={(col != null ? col.GetType().Name : "MISSING")} trigger={(col != null && col.isTrigger)} layer={gameObject.layer}", this);
    }


    void Update()
    {
        if (!landed)
        {
            transform.position +=
                Vector3.down *
                fallSpeed *
                Time.deltaTime;

            airtime += Time.deltaTime;

            // Falling far past the floor without a single trigger contact means
            // no events are being generated at all — name the facts once.
            if (!watchdogFired && (airtime > 3f || spawnY - transform.position.y > 15f))
            {
                watchdogFired = true;
                Debug.LogWarning($"[Honey] '{name}' STILL FALLING after {airtime:F1}s (y {spawnY:F1} -> {transform.position.y:F1}) " +
                                 $"with {contactLogs} trigger contacts seen. If contacts=0, no trigger events are generated: " +
                                 $"check the instantiated glob has the kinematic Rigidbody (prefab reimport!) and the floor collider under it.", this);
            }
        }
    }


    // The honey's collider is (correctly) a trigger so the boss can stand inside
    // it for BossStuckInHoney. Triggers never fire OnCollisionEnter, which is why
    // the glob fell straight through the room (issue #41). Landing is now trigger
    // based, and works against any static geometry so the floor does not need a
    // special tag. Requires a kinematic Rigidbody on this object so Unity
    // generates trigger events at all (added on the Honey prefab).
    void OnTriggerEnter(Collider other)
    {
        // Name the first few contacts BEFORE any guard decides — silent guard
        // rejections are undiagnosable from a log dump.
        if (!landed && contactLogs < 5)
        {
            contactLogs++;
            Debug.Log($"[Honey] '{name}' contact {contactLogs}: '{other.name}' trigger={other.isTrigger} rb={(other.attachedRigidbody != null)} layer={other.gameObject.layer}", this);
        }

        if (landed || other.isTrigger)
            return;

        // Never land on the beehive we just fell out of (or any other hive).
        if (other.GetComponentInParent<BeehiveTarget>() != null)
        {
            Debug.Log($"[Honey] ignoring contact with hive collider '{other.name}' while falling", this);
            return;
        }

        // No below-the-glob geometry test: the room floor is a NON-CONVEX mesh
        // collider, and Physics.ClosestPoint silently no-ops on those (returns
        // the query point), which made every real floor contact look like a
        // side-brush. The glob falls straight down and hive/trigger/rigidbody
        // contacts are already excluded above, so any remaining static contact
        // IS ground. (Diagnosed from two rounds of [Honey] contact logs.)

        // static level geometry has no rigidbody; the boss and player do
        bool isStaticGeometry = other.attachedRigidbody == null;

        if (isStaticGeometry || other.CompareTag("Floor"))
        {
            landed = true;
            Debug.Log($"[Honey] landed on '{other.name}' at y={transform.position.y:F2}", this);

            Rigidbody rb = GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
            }
        }
    }
}
