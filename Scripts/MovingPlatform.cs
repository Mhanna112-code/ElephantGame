using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;

    private int direction = 1;

    [Header("Ricochet")]
    public bool ricochetEnabled = false;

    [Header("Color")]
    public Renderer rend;
    public Material redMat;
    public Material greenMat;

    [Header("Wall Detection")]
    public float detectDistance = 0.1f;
    public LayerMask wallLayer;

    private BoxCollider box;

    [Header("Diagnostics")]
    public bool debugLogs = true;

    void Start()
    {
        box = GetComponent<BoxCollider>();

        ricochetEnabled = false;
        UpdateColor();

        if (debugLogs)
            Debug.Log($"[MovingPlatform] Start '{name}' speed={speed} detectDistance={detectDistance} " +
                      $"wallLayer={wallLayer.value} ricochetEnabled={ricochetEnabled}", this);
    }

    void Update()
    {
        Vector3 moveDir = Vector3.right * direction;

        RaycastHit hit;

        bool willHitWall = Physics.BoxCast(
            box.bounds.center,
            box.bounds.extents,
            moveDir,
            out hit,
            transform.rotation,
            detectDistance,
            wallLayer
        );

        if (willHitWall)
        {
            direction *= -1;

            if (debugLogs)
                Debug.Log($"[MovingPlatform] '{name}' hit wall '{hit.collider.name}' -> reverse to dir={direction}", this);

            // Recalculate movement after reversing
            moveDir = Vector3.right * direction;
        }

        transform.position += moveDir * speed * Time.deltaTime;
    }

    public void ToggleRicochet()
    {
        ricochetEnabled = !ricochetEnabled;
        UpdateColor();

        if (debugLogs)
            Debug.Log($"[MovingPlatform] '{name}' ToggleRicochet -> {(ricochetEnabled ? "GREEN (bounces)" : "RED (absorbs)")}", this);
    }

    void UpdateColor()
    {
        if (rend == null)
            return;

        rend.material = ricochetEnabled ? greenMat : redMat;
    }

    void OnDrawGizmosSelected()
    {
        BoxCollider b = GetComponent<BoxCollider>();
        if (b == null) return;

        Gizmos.color = Color.yellow;

        Vector3 moveDir = Vector3.right * direction;
        Gizmos.DrawWireCube(
            b.bounds.center + moveDir * detectDistance,
            b.bounds.size
        );
    }
}