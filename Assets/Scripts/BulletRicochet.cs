using UnityEngine;

public class BulletRicochet : MonoBehaviour
{
    private Vector3 direction;
    private float speed;

    [Header("Ricochet")]
    public float bounceSpeedMultiplier = 0.8f;
    public int maxBounces = 5;

    private int bounceCount;

    private Rigidbody playerRb;

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
    }

    void Update()
    {
        Vector3 pos = transform.position;

        pos += direction * speed * Time.deltaTime;

        // 🔒 KEEP GAME ON XY PLANE (DO NOT TOUCH Y)
        // FIX: only lock Z, NOT Y (this was killing vertical recoil before)
        pos.z = 0f;

        transform.position = pos;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Destroy bullet when hitting lever
        if (collision.collider.CompareTag("lever"))
        {
            Destroy(gameObject);
            return;
        }

        // Beehive: register the hit on the parent script even when the contact
        // lands on a child collider (static hive = no rigidbody, so the hive's
        // own OnCollisionEnter never sees child-collider hits), and consume the
        // bullet instead of ricocheting off the hive.
        BeehiveTarget hive = collision.collider.GetComponentInParent<BeehiveTarget>();
        if (hive != null)
        {
            hive.Hit();
            Destroy(gameObject);
            return;
        }

        BossHealth boss = collision.collider.GetComponent<BossHealth>();

        if (boss != null)
        {
            boss.TakeBulletDamage(1);
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
        Destroy(gameObject);
    }
}