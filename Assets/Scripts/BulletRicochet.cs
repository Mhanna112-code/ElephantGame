using UnityEngine;

public class BulletRicochet : MonoBehaviour
{
    private Vector3 direction;
    private float speed;

    [Header("Ricochet")]
    public float bounceSpeedMultiplier = 0.8f;
    public int maxBounces = 5;

    [Header("Recoil (issue #13)")]
    [Tooltip("Push the player away from every floor/wall the bullet ricochets off, not just the first hit.")]
    public bool recoilOnEveryBounce = true;
    public float recoilForceMultiplier = 0.8f;

    [Header("Diagnostics")]
    public bool debugLogs = true;

    private int bounceCount;

    // ROOT CAUSE (issue #16): the PLAYER object has a stray BulletRicochet component. Its OnBecameInvisible
    // called Destroy(gameObject), so when the player scrolled off camera it destroyed the PLAYER (then
    // HighRisePlatform crashed on the dead player). A real bullet is always fired via Init(); anything that
    // was never Init'd (like the one on the player) is not a bullet, so it now stays completely inert.
    private bool initialized;

    private Rigidbody playerRb;

    void Awake()
    {
        if (GetComponent<PlayerController>() != null)
            Debug.LogWarning($"[Bullet] BulletRicochet is attached to the PLAYER ('{name}'); it will stay inert, " +
                             $"but you should remove this stray component from the player in the scene (issue #16).", this);
    }

    void Start()
    {
        // ROOT CAUSE (issue #13): recoil is applied to the player's Rigidbody, but it was located ONLY via
        // FindGameObjectWithTag("Player"). In the current scene the player object is mis-tagged "Climbable"
        // (the boss-update scene rewrite dropped the Player/Trunk/MainCamera tags), so this returned null
        // and NO recoil ever fired. Find the player robustly by its PlayerController so recoil works no
        // matter what the tag is; keep the tag lookup as a fast path.
        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
            playerRb = tagged.GetComponent<Rigidbody>();

        if (playerRb == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
                playerRb = pc.GetComponent<Rigidbody>();
        }

        if (debugLogs)
        {
            if (playerRb != null)
                Debug.Log($"[Bullet] player Rigidbody found on '{playerRb.name}' -> recoil enabled", this);
            else
                Debug.LogWarning("[Bullet] no player Rigidbody found (no 'Player'-tagged object AND no PlayerController) -> recoil disabled", this);
        }
    }

    public void Init(Vector3 dir, float spd)
    {
        direction = dir.normalized;
        speed = spd;
        initialized = true;
    }

    void Update()
    {
        if (!initialized)
            return;

        Vector3 pos = transform.position;

        pos += direction * speed * Time.deltaTime;

        // 🔒 KEEP GAME ON XY PLANE (DO NOT TOUCH Y)
        // FIX: only lock Z, NOT Y (this was killing vertical recoil before)
        pos.z = 0f;

        transform.position = pos;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!initialized)
            return;

        // Destroy bullet when hitting lever
        if (collision.collider.CompareTag("lever"))
        {
            Destroy(gameObject);
            return;
        }

        if (bounceCount >= maxBounces)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 normal = collision.contacts[0].normal;
        normal.z = 0f;
        normal.Normalize();

        // ============================
        // 🔥 PLATFORM LOGIC CHECK
        // ============================
        MovingPlatform platform =
            collision.collider.GetComponent<MovingPlatform>();

        FlashingPlatform platform2 =
            collision.collider.GetComponent<FlashingPlatform>();

        if (platform != null)
        {
            if (!platform.ricochetEnabled)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (platform2 != null)
        {
            if (!platform2.ricochetEnabled)
            {
                Destroy(gameObject);
                return;
            }
        }

        // Speed at the moment of impact, BEFORE we bleed it off for the bounce. Recoil is scaled off this
        // so the push does not shrink just because we also reduce the bullet's travel speed below.
        float impactSpeed = speed;

        // ============================
        // REFLECT BULLET
        // ============================
        direction = Vector3.Reflect(direction, normal);
        direction.z = 0f;
        direction.Normalize();

        speed *= bounceSpeedMultiplier;

        // ============================
        // PLAYER RECOIL off floors / walls (issue #13)
        // ============================
        // Push the player in the direction opposite the surface (the surface normal), so shooting the
        // floor/walls launches the player. Fires on every floor/wall ricochet by default (issue #13 asks
        // for force on these collisions, not just the very first one).
        if (playerRb != null && (recoilOnEveryBounce || bounceCount == 0))
        {
            Vector3 recoilDir = normal;

            if (normal.y > 0.5f)
                recoilDir.y = Mathf.Abs(recoilDir.y) + 1f;

            recoilDir.z = 0f;
            recoilDir.Normalize();

            float force = impactSpeed * recoilForceMultiplier;
            playerRb.AddForce(recoilDir * force, ForceMode.Impulse);

            if (debugLogs)
                Debug.Log($"[Bullet] recoil off '{collision.collider.name}' tag='{collision.collider.tag}' " +
                          $"normal={normal} dir={recoilDir} force={force:F2} bounce={bounceCount}", this);
        }
        else if (debugLogs && playerRb == null)
        {
            Debug.LogWarning($"[Bullet] hit '{collision.collider.name}' but playerRb is null -> no recoil " +
                             $"(issue #13 root cause: player is not tagged 'Player')", this);
        }

        bounceCount++;
    }

    void OnBecameInvisible()
    {
        // Only a real, fired bullet cleans itself up off-screen. Without this guard the stray BulletRicochet
        // on the player would Destroy the PLAYER the moment it left the camera view (issue #16).
        if (!initialized)
            return;

        Destroy(gameObject);
    }
}