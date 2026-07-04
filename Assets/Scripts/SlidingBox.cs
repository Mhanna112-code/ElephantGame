using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SlidingBox : MonoBehaviour
{
    [Header("Sliding")]
    public float slideSpeed = 4f;
    public float slideDuration = 3f;

    private Rigidbody rb;

    private bool sliding = false;
    private float slideTimer = 0f;
    private Vector3 slideDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Prevent movement in unwanted directions
        rb.constraints =
            RigidbodyConstraints.FreezeRotation |
            RigidbodyConstraints.FreezePositionZ;
    }

    void FixedUpdate()
    {
        if (!sliding)
            return;

        slideTimer += Time.fixedDeltaTime;

        if (slideTimer >= slideDuration)
        {
            StopSliding();
            return;
        }

        rb.linearVelocity = new Vector3(
            slideDirection.x * slideSpeed,
            rb.linearVelocity.y,
            0f
        );
    }

    void OnCollisionStay(Collision collision)
    {
        if (!collision.collider.CompareTag("Player"))
            return;

        Rigidbody playerRb = collision.collider.GetComponent<Rigidbody>();

        if (playerRb == null)
            return;

        // Only start sliding if player is actually pushing
        if (Mathf.Abs(playerRb.linearVelocity.x) > 0.1f)
        {
            slideDirection = new Vector3(
                Mathf.Sign(playerRb.linearVelocity.x),
                0f,
                0f
            );

            sliding = true;
            slideTimer = 0f;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Stop when hitting anything except the player
        if (!collision.collider.CompareTag("Player"))
        {
            StopSliding();
        }
    }

    void StopSliding()
    {
        sliding = false;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }
}