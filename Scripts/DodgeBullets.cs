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
    public float dodgeSpeed = 5f;

    private Vector3 homePosition;
    private Vector3 targetPosition;
    private bool dodging;

    [Header("Diagnostics")]
    public bool debugLogs = true;

    void Start()
    {
        homePosition = transform.position;
        targetPosition = homePosition;

        animator = GetComponent<Animator>();

        if (animator != null)
            animator.Play("Floating");

        if (debugLogs)
            Debug.Log($"[Toad] Start '{name}' home={homePosition} detectRadius={detectionRadius} " +
                      $"dodgeDistance={dodgeDistance} animator={(animator != null)}", this);
    }

    void Update()
    {
        dodging = false;

        Collider[] bullets = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            bulletLayer
        );

        foreach (Collider bullet in bullets)
        {
            Rigidbody rb = bullet.GetComponent<Rigidbody>();

            if (rb == null)
                continue;

            Vector3 toEnemy = transform.position - bullet.transform.position;

            // Bullet heading toward enemy?
            if (Vector3.Dot(rb.linearVelocity.normalized, toEnemy.normalized) > 0.8f)
            {
                Vector3 dodgeDir = Vector3.Cross(
                    rb.linearVelocity.normalized,
                    Vector3.forward
                ).normalized;

                if (Random.value > 0.5f)
                    dodgeDir = -dodgeDir;

                targetPosition = homePosition + dodgeDir * dodgeDistance;
                dodging = true;
                if (debugLogs)
                    Debug.Log($"[Toad] '{name}' DODGING bullet '{bullet.name}' -> dodgeDir={dodgeDir} target={targetPosition}", this);
                break;
            }
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
            // Hover left/right on the X axis
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