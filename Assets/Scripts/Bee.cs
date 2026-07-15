using UnityEngine;

public class Bee : MonoBehaviour
{
    [Header("Flight")]
    public float approachSpeed = 6f;
    public float arriveThreshold = 0.25f;

    [Header("Orbit")]
    public float orbitRadius = 1.1f;
    public float orbitHeight = 1.6f;
    public float orbitSpeed = 140f;
    public float bobAmount = 0.15f;
    public float bobSpeed = 4f;

    [Header("Sting")]
    public float damage = 5f;

    private Transform player;
    private float orbitAngle;
    private float angularSpeed;
    private float bobOffset;
    private bool orbiting;

    void Awake()
    {
        Collider col = GetComponent<Collider>();

        if (col == null)
        {
            SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
            sphere.radius = 0.2f;
            col = sphere;
        }

        col.isTrigger = true;
    }

    public void Init(Transform playerTransform)
    {
        player = playerTransform;
        orbitAngle = Random.Range(0f, 360f);
        angularSpeed = orbitSpeed * (Random.value < 0.5f ? 1f : -1f);
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 orbitCenter = player.position + Vector3.up * orbitHeight;

        if (!orbiting)
        {
            transform.position = Vector3.MoveTowards(transform.position, orbitCenter, approachSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, orbitCenter) <= arriveThreshold)
                orbiting = true;
        }
        else
        {
            orbitAngle += angularSpeed * Time.deltaTime;

            float bob = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobAmount;

            Vector3 offset = new Vector3(
                Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * orbitRadius,
                bob,
                0f
            );

            transform.position = orbitCenter + offset;
        }

        // Keep the bee on the player's gameplay plane.
        transform.position = new Vector3(transform.position.x, transform.position.y, player.position.z);
    }

    public void Swat()
    {
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleContact(other);
    }

    void OnTriggerStay(Collider other)
    {
        HandleContact(other);
    }

    void HandleContact(Collider other)
    {
        // Direct trunk contact is an immediate, precise way to take a bee down,
        // on top of the trunk-waggle swat that clears the whole cluster at once.
        if (other.CompareTag("Trunk"))
        {
            Swat();
            return;
        }

        Health health = other.GetComponentInParent<Health>();

        if (health != null)
        {
            health.TakeDamage(damage);
        }
    }
}
