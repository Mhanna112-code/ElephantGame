using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SlidingBox : MonoBehaviour
{
    [Header("Sliding")]
    public float slideSpeed = 4f;
    public float slideDuration = 3f;

    // ---- Diagnostics (kept in; they show the push/E/identity checks) ----
    [Header("Diagnostics")]
    public bool debugLogs = true;

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

        if (debugLogs)
            Debug.Log($"[SlidingBox] Start on '{name}': rb={(rb != null)} isKinematic={rb.isKinematic} " +
                      $"useGravity={rb.useGravity} slideSpeed={slideSpeed} constraints={rb.constraints}", this);

        if (slideSpeed <= 0f)
            Debug.LogWarning($"[SlidingBox] '{name}' slideSpeed is {slideSpeed} -> the box cannot move. Set it > 0.", this);
    }

    void FixedUpdate()
    {
        if (!sliding)
            return;

        slideTimer += Time.fixedDeltaTime;

        if (slideTimer >= slideDuration)
        {
            if (debugLogs) Debug.Log($"[SlidingBox] slide duration reached ({slideTimer:F2}s >= {slideDuration}) -> stop", this);
            StopSliding();
            return;
        }

        // Move the box. A KINEMATIC Rigidbody ignores linearVelocity (assigning it does nothing and
        // it reads back as zero - which is exactly what the logs showed), so it must be driven with
        // MovePosition. A dynamic body uses velocity. Either way the box actually slides now.
        Vector3 posBefore = rb.position;
        if (rb.isKinematic)
        {
            rb.MovePosition(rb.position + new Vector3(slideDirection.x * slideSpeed * Time.fixedDeltaTime, 0f, 0f));
        }
        else
        {
            rb.linearVelocity = new Vector3(slideDirection.x * slideSpeed, rb.linearVelocity.y, 0f);
        }

        if (debugLogs && Time.frameCount % 15 == 0)
            Debug.Log($"[SlidingBox] sliding dir={slideDirection.x} isKinematic={rb.isKinematic} slideSpeed={slideSpeed} " +
                      $"vel={rb.linearVelocity} posDelta={(rb.position - posBefore).x:F4} timer={slideTimer:F2}/{slideDuration}", this);
    }

    void OnCollisionStay(Collision collision)
    {
        // FIX: identify the player by the ATTACHED RIGIDBODY (which lives on the tagged root),
        // not the specific child collider that touched. The elephant is a rig - its
        // CapsuleCollider and trunk bone colliders (Bone, Bone.001...) are NOT tagged "Player",
        // so the old collision.collider.CompareTag("Player") failed for them and the box never
        // started sliding. collision.rigidbody is the player's root body for ALL of those parts.
        Rigidbody playerRb = collision.rigidbody;
        bool isPlayer = playerRb != null && playerRb.CompareTag("Player");

        if (debugLogs && Time.frameCount % 15 == 0)
            Debug.Log($"[SlidingBox] Stay contact='{collision.collider.name}' body='{(playerRb != null ? playerRb.name : "null")}' " +
                      $"isPlayer={isPlayer} E={Input.GetKey(KeyCode.E)} " +
                      $"playerVelX={(playerRb != null ? playerRb.linearVelocity.x : 0f):F2} sliding={sliding}", this);

        if (!isPlayer)
            return;

        // Only start a new slide if we're not already sliding
        if (sliding)
            return;

        // Player must be pressing E
        if (!Input.GetKey(KeyCode.E))
        {
            if (debugLogs && Time.frameCount % 30 == 0)
                Debug.Log($"[SlidingBox] player touching but E not held -> no slide", this);
            return;
        }

        // Player must also be moving against the box
        if (Mathf.Abs(playerRb.linearVelocity.x) < 0.1f)
        {
            if (debugLogs && Time.frameCount % 30 == 0)
                Debug.Log($"[SlidingBox] E held but not pushing (velX={playerRb.linearVelocity.x:F2} < 0.1) -> no slide", this);
            return;
        }

        slideDirection = new Vector3(
            Mathf.Sign(playerRb.linearVelocity.x),
            0f,
            0f
        );

        sliding = true;
        slideTimer = 0f;

        if (debugLogs)
            Debug.Log($"[SlidingBox] START sliding dir={slideDirection.x} (E held, pushing velX={playerRb.linearVelocity.x:F2})", this);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Stop only when hitting a real obstacle - NOT the player's own colliders. Same identity
        // fix: a wall has no Rigidbody (collision.rigidbody == null) so it still stops the box,
        // but a trunk bone / capsule (whose body is the tagged player) no longer stops it.
        Rigidbody otherRb = collision.rigidbody;
        bool isPlayer = otherRb != null && otherRb.CompareTag("Player");

        if (!isPlayer)
        {
            if (debugLogs) Debug.Log($"[SlidingBox] hit obstacle '{collision.collider.name}' -> StopSliding", this);
            StopSliding();
        }
    }

    void StopSliding()
    {
        if (debugLogs && sliding) Debug.Log($"[SlidingBox] StopSliding on '{name}'", this);
        sliding = false;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }
}
