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

    void Start()
    {
        box = GetComponent<BoxCollider>();

        ricochetEnabled = false;
        UpdateColor();
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

            // Recalculate movement after reversing
            moveDir = Vector3.right * direction;
        }

        transform.position += moveDir * speed * Time.deltaTime;
    }

    public void ToggleRicochet()
    {
        ricochetEnabled = !ricochetEnabled;
        UpdateColor();
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