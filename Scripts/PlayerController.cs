using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 8f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer;

    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform shootPoint;
    public float bulletSpeed = 20f;

    [Header("Mouse Aim")]
    public Camera cam;
    public float aimDistance = 50f;

    Rigidbody rb;
    bool isGrounded;

    bool canShoot = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Collider bulletCol = GetComponent<Collider>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (bulletCol != null && player != null)
        {
            Collider[] playerCols = player.GetComponentsInChildren<Collider>();

            foreach (var col in playerCols)
            {
                Physics.IgnoreCollision(bulletCol, col);
            }
        }
    }

    void Update()
    {
        Debug.Log(rb.constraints);
        CheckGround();
        Move();

        if (Input.GetKeyDown(KeyCode.Space))
            Jump();

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Firing input detected");
            Shoot();
        }
    }

    void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");

        float z = 0f;

        // Only allow Z movement when it has been enabled
        if ((rb.constraints & RigidbodyConstraints.FreezePositionZ) == 0)
        {
            z = Input.GetAxisRaw("Vertical");
        }

        Vector3 vel = rb.linearVelocity;
        vel.x = x * moveSpeed;
        vel.z = z * moveSpeed;

        rb.linearVelocity = vel;
    }

    void Jump()
    {
        if (!isGrounded) return;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }

void Shoot()
{
    if (canShoot) {
        Debug.Log("shooting");
        Vector3 shootDir = GetMouseDirection();

        GameObject bullet = Instantiate(bulletPrefab, shootPoint.position, Quaternion.identity);
        Debug.DrawRay(shootPoint.position, shootDir * 5f, Color.green, 2f);
        BulletRicochet b = bullet.GetComponent<BulletRicochet>();

        if (b != null)
        {
            b.Init(shootDir, bulletSpeed);
        }
        else
        {
            Debug.LogError("BulletRicochet missing on prefab!");
        }
    }
}
    [SerializeField] private Transform firePoint;

    Vector3 GetMouseDirection()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Plane parallel to the XY plane at the player's Z position
        Plane plane = new Plane(Vector3.forward, shootPoint.position);

        if (plane.Raycast(ray, out float enter))
        {
            Vector3 mouseWorld = ray.GetPoint(enter);

            Vector3 dir = mouseWorld - shootPoint.position;
            dir.z = 0f;

            // Flip because camera is rotated -90° around Y
            return dir.normalized;
        }

        return Vector3.right;
    }

    void CheckGround()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    public bool IsAbovePlatform { get; private set; }

    public void EnableZMovement()
    {
        IsAbovePlatform = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void DisableZMovement()
    {
        IsAbovePlatform = false;

        rb.constraints =
            RigidbodyConstraints.FreezeRotation |
            RigidbodyConstraints.FreezePositionZ;

        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
    }
    public void EnableShooting()
    {
        canShoot = true;
    }

    public void DisableShooting()
    {
        canShoot = false;
    }
}