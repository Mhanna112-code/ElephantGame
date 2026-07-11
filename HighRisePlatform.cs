using UnityEngine;

public class HighRisePlatform : MonoBehaviour
{
    [Header("References")]
    public Collider platformCollider;
    public PlayerController player;
    public Transform highRisePlatform;
    void Update()
    {
        if ( player.transform.position.y < highRisePlatform.position.y)
        {
            platformCollider.enabled = false;
        }
        else if (player.transform.position.y > highRisePlatform.position.y)
        {
            platformCollider.enabled = true;
        }
    }
}