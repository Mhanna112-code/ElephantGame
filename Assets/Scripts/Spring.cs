using UnityEngine;

public class Spring : MonoBehaviour
{
    public float launchForce = 30f;

    void OnCollisionEnter(Collision collision)
    {
        TryLaunch(collision.rigidbody);
    }

    void OnTriggerEnter(Collider other)
    {
        TryLaunch(other.attachedRigidbody);
    }

    void TryLaunch(Rigidbody playerRb)
    {
        if (playerRb == null)
            return;

        PlayerController player = playerRb.GetComponent<PlayerController>();

        if (player == null)
            return;

        Vector3 pos = playerRb.position;
        pos.z = 0f;
        playerRb.position = pos;

        Vector3 velocity = playerRb.linearVelocity;
        velocity.y = 0f;
        playerRb.linearVelocity = velocity;

        playerRb.AddForce(Vector3.up * launchForce, ForceMode.VelocityChange);
    }
}
