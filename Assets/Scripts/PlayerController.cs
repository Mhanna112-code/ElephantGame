using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 8f;

    [Header("Ground Check")]
    public Transform groundCheckPoint;
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer;
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
    private bool isRidingMinecart;

    private bool blockedPositiveX;
    private bool blockedNegativeX;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // The kinematic boss can overlap the player during jump attacks; with an
        // uncapped depenetration velocity the solver ejects the player hard
        // enough to tunnel through colliders and end up inside the floor
        // (issue #45). Cap the ejection and use continuous collision so fast
        // knockbacks sweep instead of teleporting.
        rb.maxDepenetrationVelocity = 6f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

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
        EnableZMovement();
        CheckGround();
        Debug.Log("isGrounded: " + isGrounded);
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
        if (isRidingMinecart)
            return;

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
        playerModel.localRotation = Quaternion.Euler(-90f, 0f, -90f);
    }
    else if (x < 0)
    {
        // Facing left
        playerModel.localRotation = Quaternion.Euler(-90f, 0f, 90f);
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

        float moveX = x * moveSpeed;

        // Don't keep re-driving into a wall we're already pressed against - that
        // constant push is what was pinning the player in the air against it
        // instead of letting gravity slide them down like a normal wall.
        if ((moveX > 0f && blockedPositiveX) || (moveX < 0f && blockedNegativeX))
            moveX = 0f;

        Vector3 vel = rb.linearVelocity;

        vel.x = moveX;
        vel.z = z * moveSpeed;

        rb.linearVelocity = vel;
    }

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            Vector3 normal = contact.normal;

            // Ignore floor/ceiling contacts - only steep/vertical surfaces count as walls.
            if (Mathf.Abs(normal.y) > 0.5f)
                continue;

            // contact.normal points away from the surface, back toward the player,
            // so a wall blocking rightward movement has a normal pointing left.
            if (normal.x < 0f)
                blockedPositiveX = true;
            else if (normal.x > 0f)
                blockedNegativeX = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        blockedPositiveX = false;
        blockedNegativeX = false;
    }


    void Jump()
    {
        if (isClimbing)
            return;

        if (isRidingMinecart)
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
        Vector3 origin = groundCheckPoint != null ? groundCheckPoint.position : transform.position;

        isGrounded = Physics.Raycast(
            origin,
            Vector3.down,
            groundCheckDistance,
            groundLayer
        );

        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
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


    public void SetRidingMinecart(bool riding)
    {
        isRidingMinecart = riding;
    }


    void OnDestroy()
    {
        Debug.Log("player has been destroyed");
    }
}