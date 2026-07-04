using UnityEngine;

public class Lever : MonoBehaviour
{
    [Header("Platform To Control")]
    public MovingPlatform targetPlatform;

    [Header("Rotation")]
    public float angle = 45f;
    public float rotateSpeed = 8f;

    [Header("Color")]
    public Renderer rend;
    public Material offMaterial; // red
    public Material onMaterial;  // green

    private bool isOn = false;

    private Quaternion offRotation;
    private Quaternion onRotation;

    void Start()
    {
        offRotation = transform.rotation;
        onRotation = offRotation * Quaternion.Euler(0f, 0f, angle);

        UpdateVisual();
    }

    void Update()
    {
        Quaternion target = isOn ? onRotation : offRotation;

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            target,
            Time.deltaTime * rotateSpeed
        );
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag("Bullet"))
            return;

        isOn = !isOn;

        UpdateVisual();

        if (targetPlatform != null)
            targetPlatform.ToggleRicochet();
    }

    void UpdateVisual()
    {
        if (rend == null) return;

        rend.material = isOn ? onMaterial : offMaterial;
    }
}