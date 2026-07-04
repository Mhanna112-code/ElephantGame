using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public float speed = 3f;
    private int direction = 1;

    public bool ricochetEnabled = false;

    [Header("Color")]
    public Renderer rend;
    public Material redMat;
    public Material greenMat;

    [Header("Wall Detection")]
    public float detectDistance = 0.6f;
    public LayerMask wallLayer;

    void Start()
    {
        ricochetEnabled = false;
        UpdateColor();
    }

    void Update()
    {
        Vector3 moveDir = Vector3.right * direction;

        // 🔥 Detect wall BEFORE moving
        RaycastHit hit;

        bool willHitWall = Physics.BoxCast(
            transform.position,
            transform.localScale * 0.45f,
            moveDir,
            out hit,
            Quaternion.identity,
            detectDistance,
            wallLayer
        );

        if (willHitWall)
        {
            direction *= -1;
        }

        transform.position += moveDir * speed * Time.deltaTime;
    }

    public void ToggleRicochet()
    {
        ricochetEnabled = !ricochetEnabled;
        Debug.Log("Platform toggled: " + ricochetEnabled);
        UpdateColor();
    }

    void UpdateColor()
    {
        if (rend == null)
        {
            Debug.LogError("NO RENDERER ASSIGNED ON PLATFORM");
            return;
        }

        rend.material = ricochetEnabled ? greenMat : redMat;
    }
}