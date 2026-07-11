using UnityEngine;

public class SnapStickToPlayer : MonoBehaviour
{
    public Transform player;
    public Transform stickFront;

    public float offset = 0.02f;

    void Start()
    {
        if (player == null || stickFront == null)
            return;

        Vector3 forward = player.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 targetPosition = player.position + forward * offset;

        // Move the whole stone so the front of the sticks
        // lines up with the front of the player.
        transform.position += targetPosition - stickFront.position;
    }
}