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

    [Header("Diagnostics")]
    public bool debugLogs = true;

    void Start()
    {
        // always target the player directly (not collision object)
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
            playerRb = player.GetComponent<Rigidbody>();

        if (debugLogs)
            Debug.Log($"[Bullet] spawned playerRb={(playerRb != null)} maxBounces={maxBounces} " +
                      $"bounceMult={bounceSpeedMultiplier}", this);
    }

    public void Init(Vector3 dir, float spd)
    {
        direction = dir.normalized;
        speed = spd;
        if (debugLogs) Debug.Log($"[Bullet] Init dir={direction} speed={speed}", this);
    }

    void Update()
    {
        Vector3 pos = transform.position;

        pos += direction * speed * Time.deltaTime;

        // keep on the XY plane
        pos.z = 0f;

        transform.position = pos;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (bounceCount >= maxBounces)
        {
            if (debugLogs) Debug.Log($"[Bullet] maxBounces ({maxBounces}) reached on '{collision.collider.name}' -> destroy", this);
            Destroy(gameObject);
            return;
        }

        Vector3 normal = collision.contacts[0].normal;
        normal.z = 0f;
        normal.Normalize();

        // Platform ricochet gate: red platform absorbs the bullet, green lets it bounce.
        MovingPlatform platform =
            collision.collider.GetComponent<MovingPlatform>();
        FlashingPlatform platform2 =
            collision.collider.GetComponent<FlashingPlatform>();

        if (platform != null)
        {
            if (!platform.ricochetEnabled)
            {
                if (debugLogs) Debug.Log($"[Bullet] hit RED MovingPlatform '{platform.name}' (ricochet off) -> no bounce", this);
                return;
            }
            if (debugLogs) Debug.Log($"[Bullet] hit GREEN MovingPlatform '{platform.name}' -> bounce allowed", this);
        }
        if (platform2 != null)
        {
            if (!platform2.ricochetEnabled)
            {
                if (debugLogs) Debug.Log($"[Bullet] hit RED FlashingPlatform '{platform2.name}' (ricochet off) -> no bounce", this);
                return;
            }
            if (debugLogs) Debug.Log($"[Bullet] hit GREEN FlashingPlatform '{platform2.name}' -> bounce allowed", this);
        }

        // Reflect
        direction = Vector3.Reflect(direction, normal);
        direction.z = 0f;
        direction.Normalize();

        speed *= bounceSpeedMultiplier;

        // Player recoil (first hit only)
        if (playerRb != null && bounceCount == 0)
        {
            Vector3 recoilDir = normal;

            if (normal.y > 0.5f)
                recoilDir.y = Mathf.Abs(recoilDir.y) + 1f;

            recoilDir.z = 0f;
            recoilDir.Normalize();

            playerRb.AddForce(recoilDir * speed * 0.8f, ForceMode.Impulse);

            if (debugLogs)
                Debug.Log($"[Bullet] first-hit RECOIL to player: dir={recoilDir} force={recoilDir * speed * 0.8f}", this);
        }

        bounceCount++;

        if (debugLogs)
            Debug.Log($"[Bullet] bounced off '{collision.collider.name}' normal={normal} -> " +
                      $"newDir={direction} speed={speed:F2} bounce {bounceCount}/{maxBounces}", this);
    }

    void OnBecameInvisible()
    {
        if (debugLogs) Debug.Log("[Bullet] off-screen -> destroy", this);
        Destroy(gameObject);
    }
}
