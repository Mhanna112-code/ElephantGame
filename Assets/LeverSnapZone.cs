using UnityEngine;
using System.Collections;

public class LeverSnapZone : MonoBehaviour
{
    [Header("References")]
    public Transform lever;
    public Transform door;

    [Header("Door Target Position")]
    public Vector3 doorTargetPos = new Vector3(11.0223799f, 30.7000008f, -0.430000007f);

    [Header("Door Rise Speed")]
    public float doorSpeed = 2f;

    private bool activated = false;

    void OnTriggerEnter(Collider other)
    {
        if (activated) return;

        if (other.transform == lever)
        {
            Activate();
        }
    }

    void Activate()
    {
        activated = true;

        // 🧱 LOCK LEVER IN PLACE (NO MORE MOVEMENT)
        Rigidbody rb = lever.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // 🚪 START DOOR RISE
        if (door != null)
        {
            StartCoroutine(RaiseDoor());
        }
    }

    IEnumerator RaiseDoor()
    {
        while (Vector3.Distance(door.position, doorTargetPos) > 0.01f)
        {
            door.position = Vector3.MoveTowards(
                door.position,
                doorTargetPos,
                doorSpeed * Time.deltaTime
            );

            yield return null;
        }

        door.position = doorTargetPos;
    }
}