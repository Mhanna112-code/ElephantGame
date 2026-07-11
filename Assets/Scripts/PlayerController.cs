using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 8f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer = (1 << 0) | (1 << 9); 
    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform shootPoint;
    public float bulletSpeed = 20f;

    [Header("Mouse Aim")]
    public Camera cam;
    public float aimDistance = 50f;

    [Header("Animation")]
    public Animator animator;

    Rigidbody rb;
    bool isGrounded;
    bool canShoot = true;

    public bool IsAbovePlatform { get; private set; }

    private bool isClimbing;

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
        CheckGround();

        // Update animator
        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
        }

        Move();

        //if (Input.GetKeyDown(KeyCode.Space))
          //  Jump();

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Firing input detected");
            Shoot();
        }
    }

    public Transform playerModel;
    void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");

        // Walking animation
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(x));
        }

        // Rotate player left/right
        if (x > 0)
        {
            // Facing right
            playerModel.localRotation = Quaternion.Euler(0, 90, 0);
        }
        else if (x < 0)
        {
            // Facing left
            playerModel.localRotation = Quaternion.Euler(0, -90, 0);
        }

        if (isClimbing)
        {
            float yClimb = Input.GetAxisRaw("Vertical");

            Vector3 climbVel = rb.linearVelocity;

            climbVel.x = x * moveSpeed;
            climbVel.y = yClimb * moveSpeed;
            climbVel.z = 0f;

            rb.linearVelocity = climbVel;
            return;
        }


        float z = 0f;

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
        if (isClimbing)
            return;

        if (!isGrounded)
            return;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }


    void Shoot()
    {
        if (isClimbing)
            return;

        if (!canShoot)
            return;


        Debug.Log("shooting");

        Vector3 shootDir = GetMouseDirection();

        GameObject bullet = Instantiate(
            bulletPrefab,
            shootPoint.position,
            Quaternion.identity
        );


        Debug.DrawRay(
            shootPoint.position,
            shootDir * 5f,
            Color.green,
            2f
        );


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


    Vector3 GetMouseDirection()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        Plane plane = new Plane(
            Vector3.forward,
            shootPoint.position
        );


        if (plane.Raycast(ray, out float enter))
        {
            Vector3 mouseWorld = ray.GetPoint(enter);

            Vector3 dir = mouseWorld - shootPoint.position;

            dir.z = 0f;

            return dir.normalized;
        }

        return Vector3.right;
    }


    void CheckGround()
    {
        isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            groundCheckDistance,
            groundLayer
        );
    }


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


    public void SetClimbing(bool climbing)
    {
        isClimbing = climbing;
    }


    void OnDestroy()
    {
        Debug.Log("player has been destroyed");
    }
}