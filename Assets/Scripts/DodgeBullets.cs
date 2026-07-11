using UnityEngine;

public class DodgeBullets : MonoBehaviour
{
    [Header("Animation")]
    private Animator animator;

    [Header("Hover")]
    public float hoverDistance = 2f;
    public float hoverSpeed = 1f;

    [Header("Bullet Detection")]
    public float detectionRadius = 8f;
    public LayerMask bulletLayer;

    [Header("Dodging")]
    public float dodgeDistance = 2f;
    public float dodgeSpeed = 20f;

    private Vector3 homePosition;
    private Vector3 targetPosition;
    private bool dodging;

    void Start()
    {
        homePosition = transform.position;
        targetPosition = homePosition;

        animator = GetComponent<Animator>();

        if (animator != null)
            animator.Play("Floating");
    }

    void Update()
    {
        dodging = false;

        Collider[] bullets = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            bulletLayer
        );

        Debug.Log($"Detected bullets: {bullets.Length}");

        foreach (Collider bullet in bullets)
        {
            Debug.Log(
                $"Bullet detected: {bullet.name} | Position: {bullet.transform.position}"
            );

            Rigidbody rb = bullet.GetComponent<Rigidbody>();

            if (rb == null)
            {
                Debug.Log($"Bullet {bullet.name} has no Rigidbody!");
                continue;
            }

            Debug.Log(
                $"Bullet Velocity: {rb.linearVelocity} | Speed: {rb.linearVelocity.magnitude}"
            );

            Vector3 toEnemy = transform.position - bullet.transform.position;

            float directionCheck = Vector3.Dot(
                rb.linearVelocity.normalized,
                toEnemy.normalized
            );

            Debug.Log(
                $"Direction check for {bullet.name}: {directionCheck}"
            );

            // Bullet heading toward enemy?
            Debug.Log($"DODGING BULLET: {bullet.name}");

            Vector3 dodgeDir = transform.position - bullet.transform.position;

            // Keep movement on XY plane
            dodgeDir.z = 0;

            dodgeDir.Normalize();

            targetPosition = homePosition + dodgeDir * dodgeDistance;

            Debug.Log($"Dodge Direction: {dodgeDir}");
            Debug.Log($"Target Position: {targetPosition}");

            dodging = true;
        }

        if (dodging)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                dodgeSpeed * Time.deltaTime
            );
        }
        else
        {
            Vector3 hoverPos = homePosition;
            hoverPos.x += Mathf.Sin(Time.time * hoverSpeed) * hoverDistance;

            transform.position = Vector3.Lerp(
                transform.position,
                hoverPos,
                5f * Time.deltaTime
            );
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}