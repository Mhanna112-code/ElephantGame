using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class BoxSnapZone : MonoBehaviour
{
    [Header("Platform to Raise")]
    public Transform risingBox;

    [Header("Target Position")]
    public Vector3 targetPosition = new Vector3(8.06000042f, 30.0999985f, 0f);

    [Header("Rise Settings")]
    public float riseSpeed = 3f;

    [Header("Snap Settings")]
    public float velocityThreshold = 0.05f;

    private Rigidbody rb;
    private bool activated = false;

    private Renderer[] boxRenderers;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (risingBox != null)
        {
            // Cache all renderers (works even if the box has children)
            boxRenderers = risingBox.GetComponentsInChildren<Renderer>();

            // Hide the box
            foreach (Renderer r in boxRenderers)
                r.enabled = false;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (activated)
            return;

        if (!collision.collider.CompareTag("SnapZone"))
            return;

        if (rb.linearVelocity.magnitude <= velocityThreshold)
        {
            Activate();
        }
    }

    void Activate()
    {
        activated = true;

        // Lock the puzzle box in place
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        if (risingBox != null)
        {
            // Make it visible before raising
            foreach (Renderer r in boxRenderers)
                r.enabled = true;

            StartCoroutine(RaiseBox());
        }
    }

    IEnumerator RaiseBox()
    {
        while (Vector3.Distance(risingBox.position, targetPosition) > 0.01f)
        {
            risingBox.position = Vector3.MoveTowards(
                risingBox.position,
                targetPosition,
                riseSpeed * Time.deltaTime
            );

            yield return null;
        }

        risingBox.position = targetPosition;
    }
}