using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;

    private Vector3 offset;

    void Start()
    {
        // Store the camera's starting distance from the player
        offset = transform.position - player.position;
    }

    void LateUpdate()
    {
        // Keep the same distance, but follow the player
        transform.position = player.position + offset;
    }
}