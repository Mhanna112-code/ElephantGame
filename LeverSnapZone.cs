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

    private Renderer[] doorRenderers;

    void Start()
    {
        if (door != null)
        {
            // Get all renderers, even if the object is inactive
            doorRenderers = door.GetComponentsInChildren<Renderer>(true);

            // Hide the door
            foreach (Renderer r in doorRenderers)
                r.enabled = false;

            // Optional: if the entire GameObject starts disabled
            // door.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (activated) return;

        if (other.transform == lever)
        {
            Debug.Log($"[LeverSnapZone] '{name}' lever snapped in -> activating (raising door)", this);
            Activate();
        }
    }

    void Activate()
    {
        activated = true;

        // Lock lever in place
        Rigidbody rb = lever.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (door != null)
        {
            // If the GameObject was disabled
            door.gameObject.SetActive(true);

            // Make the door visible
            foreach (Renderer r in doorRenderers)
                r.enabled = true;

            // Raise the door
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