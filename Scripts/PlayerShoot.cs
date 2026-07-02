using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private GameObject waterProjectilePrefab;
    [SerializeField] private Transform firePoint;

    [Header("Aim")]
    [SerializeField] private Camera cam;
    [SerializeField] private float aimDistance = 50f;

    [Header("Firing")]
    [SerializeField] private float fireRate = 0.15f;

    private float nextFireTime;

    private void Start()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Fire()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        Plane aimPlane = new Plane(transform.forward, firePoint.position);

        Vector3 targetPoint = firePoint.position + transform.right * 5f;

        if (aimPlane.Raycast(ray, out float enter))
        {
            targetPoint = ray.GetPoint(enter);
        }

        Vector3 direction = (targetPoint - firePoint.position).normalized;

        // 🔥 FORCE direction to stay on movement plane
        direction = Vector3.ProjectOnPlane(direction, transform.forward).normalized;

        GameObject obj = Instantiate(
            waterProjectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        obj.GetComponent<WaterProjectile>().Init(direction);
    }
}