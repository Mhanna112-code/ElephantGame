using UnityEngine;

public class TrunkSwing : MonoBehaviour
{
    public Camera cam;

    public Transform player;
    public Transform trunkConstraint;

    public float maxReach = 4f;
    public float smoothSpeed = 12f;

    [Header("Platform Check")]
    public LayerMask platformLayer;
    public float checkDistance = 1.5f;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        bool isAbovePlatform = Physics.Raycast(
            player.position,
            Vector3.down,
            checkDistance,
            platformLayer
        );

        Vector3 targetPos;

        if (isAbovePlatform)
        {
            // 🔥 FULL 3D AIM (no plane constraint)
            Vector3 dir = ray.direction;

            dir.z = 0f; // still keep your 2.5D constraint
            dir.Normalize();

            targetPos = player.position + dir * maxReach;
        }
        else
        {
            // 🔒 FLAT PLANE AIM (old behavior)
            Plane plane = new Plane(Vector3.forward, player.position);

            if (plane.Raycast(ray, out float enter))
            {
                Vector3 mouseWorld = ray.GetPoint(enter);

                Vector3 offset = mouseWorld - player.position;
                offset.z = 0f;

                offset = Vector3.ClampMagnitude(offset, maxReach);

                targetPos = player.position + offset;
            }
            else
            {
                targetPos = trunkConstraint.position;
            }
        }

        trunkConstraint.position = Vector3.Lerp(
            trunkConstraint.position,
            targetPos,
            Time.deltaTime * smoothSpeed
        );
    }
}