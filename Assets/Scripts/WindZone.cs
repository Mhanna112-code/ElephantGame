using UnityEngine;

public class WindZoneForce : MonoBehaviour
{
    public Vector3 windDirection = Vector3.up;
    public float windStrength = 20f;

    public Rigidbody playerRB;

    void FixedUpdate()
    {
        if (playerRB != null)
        {
            Debug.Log("Adding upward wind force");

            playerRB.AddForce(
                Vector3.up * 500f,
                ForceMode.Impulse
            );
        }
    }
}