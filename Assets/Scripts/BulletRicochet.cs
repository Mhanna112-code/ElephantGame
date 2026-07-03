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
        if (bounceCount >= maxBounces)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 normal = collision.contacts[0].normal;

        // 🔒 FIX: only flatten Z, DO NOT TOUCH Y
        // (previously you were destroying vertical reaction here)
        normal.z = 0f;
        normal.Normalize();

        // Reflect movement direction
        direction = Vector3.Reflect(direction, normal);
        direction.z = 0f;

        direction.Normalize();

        // 🔥 FIX: keep energy increase per bounce
        speed *= bounceSpeedMultiplier;

        bounceCount++;

        // 💥 FIX: ALWAYS apply recoil to player (not collision object)
        if (playerRb != null)
        {
            // 🔒 keep plane constraint but DO NOT destroy Y
            normal.z = 0f;
            normal.Normalize();

            Vector3 recoilDir = normal; 
            recoilDir.z = 0f;

            // 🔥 IMPORTANT:
            // Force upward bias when hitting ground-like surfaces
            if (normal.y > 0.5f)
            {
                recoilDir.y = Mathf.Abs(recoilDir.y) + 1f; // guaranteed upward push
            }

            recoilDir.Normalize();

            playerRb.AddForce(recoilDir * speed * 0.8f, ForceMode.Impulse);
        }
    }
}