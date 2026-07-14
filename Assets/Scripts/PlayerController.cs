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

    [Header("Debug")]
    public bool debugLogs = false;

    Rigidbody rb;
    bool isGrounded;
    public bool IsGrounded => isGrounded;
    bool canShoot = true;
    float desiredYaw;
    float lastAppliedYaw;
    Quaternion modelRestRotation;

    // Flip this in the inspector (works live in play mode) if left/right facing
    // comes out mirrored; the correct default can then be baked into code.
    public bool invertFacing = false;

    public bool IsAbovePlatform { get; private set; }

    private bool isClimbing;
    private bool isRidingMinecart;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Facing is script-driven (root yaw in LateUpdate), so physics must never
        // rotate the player. The scene rigidbody leaves Y rotation unfrozen, which
        // lets recoil impulses (trunk-shot launches) impart yaw spin that fights
        // the facing system mid-air.
        rb.constraints |= RigidbodyConstraints.FreezeRotationY;

        // Start from whatever facing the scene was authored with.
        desiredYaw = transform.eulerAngles.y;
        lastAppliedYaw = desiredYaw;
        modelRestRotation = playerModel != null ? playerModel.localRotation : Quaternion.identity;

        // One-time animation wiring dump so a T-pose can be diagnosed from the console:
        // reports whether the controller/avatar are assigned and whether each state's
        // clip reference actually resolved to a clip inside the imported FBX.
        if (animator == null)
        {
            Debug.LogWarning("[PlayerAnim] animator is NOT assigned; no animations will play.", this);
        }
        else
        {
            var rc = animator.runtimeAnimatorController;
            var clips = rc != null ? rc.animationClips : null;
            Debug.Log($"[PlayerAnim] animator on '{animator.gameObject.name}': controller={(rc != null ? rc.name : "NULL")} avatar={(animator.avatar != null ? animator.avatar.name : "NULL")} resolvedClips={(clips != null ? clips.Length : 0)} layers={animator.layerCount}", this);
            if (clips != null)
                foreach (var c in clips)
                    Debug.Log($"[PlayerAnim] resolved clip: '{(c != null ? c.name : "NULL (broken motion reference)")}'", this);
        }

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
        if (debugLogs) Debug.Log("isGrounded: " + isGrounded);
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

    void LateUpdate()
    {
        // Facing is done by yawing the player ROOT, which no animation clip touches.
        // The rigGirl model node is left entirely to the Animator: the clips bake their
        // own orientation into the bones, so flipping rigGirl fights the animation and
        // produces the tumbling snap seen in playtests.
        Quaternion want = Quaternion.Euler(0f, desiredYaw, 0f);
        if (transform.rotation != want)
        {
            float actualYaw = transform.eulerAngles.y;
            // Root yaw differing by >1 degree from what we last commanded means something
            // else (physics, another script) rotated the root between frames.
            if (Mathf.Abs(Mathf.DeltaAngle(actualYaw, lastAppliedYaw)) > 1f)
                Debug.LogWarning($"[Facing] root yaw was externally changed to {actualYaw:F0} (we last set {lastAppliedYaw:F0}); forcing {desiredYaw:F0}", this);
            transform.rotation = want;
        }
        if (desiredYaw != lastAppliedYaw)
            Debug.Log($"[Facing] root yaw applied: {lastAppliedYaw:F0} -> {desiredYaw:F0}", this);
        lastAppliedYaw = desiredYaw;

        // Pin the model node to its authored rest rotation. Some clips (Waddling) key
        // the rigGirl node itself to a different base orientation, which yawed the whole
        // model 90 degrees while walking. Bones still animate freely under this node.
        if (playerModel != null)
        {
            if (debugLogs && Time.frameCount % 30 == 0 && playerModel.localRotation != modelRestRotation)
            {
                var ci = animator != null ? animator.GetCurrentAnimatorClipInfo(0) : null;
                string clipName = ci != null && ci.Length > 0 && ci[0].clip != null ? ci[0].clip.name : "NONE";
                Debug.Log($"[PlayerAnim] clip '{clipName}' wrote model localEuler={playerModel.localRotation.eulerAngles}; pinning back to {modelRestRotation.eulerAngles}", this);
            }
            playerModel.localRotation = modelRestRotation;
        }
    }

    void Move()
    {
        if (isRidingMinecart)
            return;

        float x = Input.GetAxisRaw("Horizontal");

        // Walking animation
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(x));

            if (debugLogs && Time.frameCount % 30 == 0)
            {
                var st = animator.GetCurrentAnimatorStateInfo(0);
                var ci = animator.GetCurrentAnimatorClipInfo(0);
                string clipName = ci.Length > 0 && ci[0].clip != null ? ci[0].clip.name : "NONE";
                Debug.Log($"[PlayerAnim] state clip='{clipName}' normalizedTime={st.normalizedTime:F2} Speed={animator.GetFloat("Speed"):F2} IsGrounded={animator.GetBool("IsGrounded")}", this);
            }
        }

        // Record desired facing; applied to the root in LateUpdate.
        float prevYaw = desiredYaw;
    if (x > 0)
    {
        // Model's authored forward is -X, so facing the direction of movement
        // needs yaw 180 when moving right. Tick invertFacing if the art changes.
        desiredYaw = invertFacing ? 0f : 180f;
    }
    else if (x < 0)
    {
        desiredYaw = invertFacing ? 180f : 0f;
    }
        if (desiredYaw != prevYaw)
            Debug.Log($"[Facing] input x={x} -> desiredYaw={desiredYaw} (invertFacing={invertFacing}) rootYawNow={transform.eulerAngles.y:F0}", this);

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