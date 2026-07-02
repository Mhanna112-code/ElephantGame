using UnityEngine;

public class WaterTrunkBeam : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform trunkTip;
    [SerializeField] private Camera cam;
    [SerializeField] private float range = 50f;
    [SerializeField] private LayerMask hitMask;

    [Header("Visual")]
    [SerializeField] private LineRenderer line;
    [SerializeField] private ParticleSystem splash;

    [Header("Control")]
    [SerializeField] private bool firing;

    private void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            firing = true;

        if (Input.GetMouseButtonUp(0))
            firing = false;

        if (firing)
            ShootWater();
        else
            line.enabled = false;
    }

    void ShootWater()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        Vector3 start = trunkTip.position;
        Vector3 end;

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask))
        {
            end = hit.point;

            // 💡 gameplay effect hook
            ApplyWaterEffect(hit.collider, hit.point);

            if (splash != null)
            {
                splash.transform.position = hit.point;
                if (!splash.isPlaying) splash.Play();
            }
        }
        else
        {
            end = ray.origin + ray.direction * range;
            if (splash != null) splash.Stop();
        }

        DrawBeam(start, end);
    }

    void DrawBeam(Vector3 start, Vector3 end)
    {
        line.enabled = true;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    void ApplyWaterEffect(Collider hit, Vector3 point)
    {
        // Example interactions:
        // - push objects
        // - activate levers
        // - slow enemies
        // - fill areas

        Rigidbody rb = hit.attachedRigidbody;
        if (rb != null)
        {
            Vector3 force = (hit.transform.position - trunkTip.position).normalized;
            rb.AddForce(force * 5f, ForceMode.Impulse);
        }
    }
}