using UnityEngine;

public class HoneyDrop : MonoBehaviour
{
    public float fallSpeed = 5f;

    private bool landed = false;


    void Update()
    {
        if (!landed)
        {
            transform.position +=
                Vector3.down *
                fallSpeed *
                Time.deltaTime;
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
        if (landed || other.isTrigger)
            return;

        // Never land on the beehive we just fell out of (or any other hive).
        if (other.GetComponentInParent<BeehiveTarget>() != null)
            return;

        // Only land on surfaces BELOW the glob's centre — brushing the side of
        // a wall or a prop while falling should not freeze the drop mid-air.
        if (other.bounds.max.y > transform.position.y)
            return;

        // static level geometry has no rigidbody; the boss and player do
        bool isStaticGeometry = other.attachedRigidbody == null;

        if (isStaticGeometry || other.CompareTag("Floor"))
        {
            landed = true;

            Rigidbody rb = GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
            }
        }
    }
}
