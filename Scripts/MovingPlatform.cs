using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;

    [Header("Detection")]
    public float detectDistance = 0.6f;
    public LayerMask wallLayer;

    private int direction = 1;

    void Update()
    {
        Vector3 moveDir = Vector3.right * direction;

        // 🔥 Predict collision using BoxCast (NOT raycast)
        bool willHitWall = Physics.BoxCast(
            transform.position,
            transform.localScale * 0.45f,   // size of platform
            moveDir,
            Quaternion.identity,
            detectDistance,
            wallLayer
        );

        if (willHitWall)
        {
            direction *= -1;
        }

        transform.position += moveDir * speed * Time.deltaTime;
    }
}