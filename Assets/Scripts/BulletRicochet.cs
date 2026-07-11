using UnityEngine;

public class BulletRicochet : MonoBehaviour
{
    private Vector3 direction;
    private float speed;

    [Header("Ricochet")]
    public float bounceSpeedMultiplier = 0.8f;
    public int maxBounces = 5;

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
        // ✅ FIX: always target the player directly (not collision object)
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
            playerRb = player.GetComponent<Rigidbody>();
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

        // ============================
        // REFLECT BULLET
        // ============================
        direction = Vector3.Reflect(direction, normal);
        direction.z = 0f;
        direction.Normalize();

        speed *= bounceSpeedMultiplier;

        // ============================
        // PLAYER RECOIL (first hit only)
        // ============================
        if (playerRb != null && bounceCount == 0)
        {
            Vector3 recoilDir = normal;

            if (normal.y > 0.5f)
                recoilDir.y = Mathf.Abs(recoilDir.y) + 1f;

            recoilDir.z = 0f;
            recoilDir.Normalize();

            playerRb.AddForce(recoilDir * speed * 0.8f, ForceMode.Impulse);
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