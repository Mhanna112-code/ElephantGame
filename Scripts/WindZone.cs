using UnityEngine;

public class WindZoneForce : MonoBehaviour
{
    public Vector3 windDirection = Vector3.up;
    public float windStrength =100f;

    public Rigidbody playerRB;
    public PlayerController player;
    public Collider platformCollider;

    public Transform highRisePlatform;

    void FixedUpdate()
    {
        
        if (playerRB != null && player.transform.position.y > transform.position.y && player.transform.position.y < highRisePlatform.position.y)
        {
            Debug.Log("Adding upward wind force");

            playerRB.AddForce(
                Vector3.up * windStrength,
                ForceMode.Impulse
            );
        }
    }
}