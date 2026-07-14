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

    [Header("Spring Settings")]
    public float bounceForce = 12f;
    public float springCompression = 0.3f;
    public float springSpeed = 10f;

    [Header("Snap Settings")]
    public float velocityThreshold = 0.05f;

    private Rigidbody rb;
    private bool activated = false;
    private bool canBounce = false;

    private Renderer[] boxRenderers;


    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (risingBox != null)
        {
            boxRenderers = risingBox.GetComponentsInChildren<Renderer>(true);

            // Hide platform initially
            foreach (Renderer r in boxRenderers)
                r.enabled = false;

            risingBox.gameObject.SetActive(false);
        }
    }


    void OnCollisionStay(Collision collision)
    {
        if (activated)
            return;

        if (!collision.collider.CompareTag("SnapZone"))
            return;
        Debug.Log("collided by SNAPZONE BOX");
        if (rb.linearVelocity.magnitude <= velocityThreshold)
        {
            Debug.Log("ACTIVATING");
            Activate();
        }
    }


    void Activate()
    {
        activated = true;

        // Lock snapped box
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeAll;


        if (risingBox != null)
        {
            risingBox.gameObject.SetActive(true);

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

        canBounce = true;
    }


    // Spring platform effect
    void OnCollisionEnter(Collision collision)
    {
        if (!canBounce)
            return;


        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();

            if (playerRb != null)
            {
                // Remove downward velocity
                Vector3 velocity = playerRb.linearVelocity;
                velocity.y = 0;
                playerRb.linearVelocity = velocity;


                // Launch player
                playerRb.AddForce(
                    Vector3.up * bounceForce,
                    ForceMode.Impulse
                );


                // Animate spring compression
                StartCoroutine(SpringAnimation());
            }
        }
    }


    IEnumerator SpringAnimation()
    {
        Vector3 original = targetPosition;
        Vector3 compressed = original - Vector3.up * springCompression;


        // Compress down
        while (Vector3.Distance(risingBox.position, compressed) > 0.01f)
        {
            risingBox.position = Vector3.MoveTowards(
                risingBox.position,
                compressed,
                springSpeed * Time.deltaTime
            );

            yield return null;
        }


        // Expand back
        while (Vector3.Distance(risingBox.position, original) > 0.01f)
        {
            risingBox.position = Vector3.MoveTowards(
                risingBox.position,
                original,
                springSpeed * Time.deltaTime
            );

            yield return null;
        }


        risingBox.position = original;
    }
}