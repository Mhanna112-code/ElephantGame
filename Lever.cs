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

    // The trunk is a kinematically position-lerped transform, so it does NOT reliably fire physics
    // OnCollisionEnter. Detect it by PROXIMITY instead: assign the trunk tip and the lever flips
    // when the tip gets within interactRadius. Toggles once per touch, re-arms when the trunk leaves.
    [Header("Trunk Interaction")]
    public Transform trunkTip;
    public float interactRadius = 0.4f;

    [Header("Diagnostics")]
    public bool debugLogs = true;

    private bool isOn = false;
    private bool trunkInside = false;

    private Quaternion offRotation;
    private Quaternion onRotation;

    void Start()
    {
        offRotation = transform.rotation;
        onRotation = offRotation * Quaternion.Euler(0f, 0f, angle);

        UpdateVisual();

        if (debugLogs)
            Debug.Log($"[Lever] Start '{name}': targetPlatform={(targetPlatform != null)} " +
                      $"trunkTip={(trunkTip != null ? trunkTip.name : "NULL")} interactRadius={interactRadius} " +
                      $"rend={(rend != null)}", this);
    }

    void Update()
    {
        Quaternion target = isOn ? onRotation : offRotation;

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            target,
            Time.deltaTime * rotateSpeed
        );

        // --- Trunk proximity interaction (robust; no physics collision needed) ---
        if (trunkTip != null)
        {
            float d = Vector3.Distance(trunkTip.position, transform.position);
            bool inside = d <= interactRadius;

            if (inside && !trunkInside)
            {
                if (debugLogs)
                    Debug.Log($"[Lever] '{name}' trunk entered (dist={d:F2} <= {interactRadius}) -> Activate", this);
                Activate();
            }
            else if (debugLogs && Time.frameCount % 30 == 0 && d < interactRadius * 3f)
            {
                // helps tune interactRadius: shows how close the trunk actually gets
                Debug.Log($"[Lever] '{name}' trunk near (dist={d:F2}, need <= {interactRadius}) inside={inside}", this);
            }

            trunkInside = inside;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Kept for bullets. NOTE: this path fires for bullets only; the trunk uses proximity above.
        if (debugLogs)
            Debug.Log($"[Lever] '{name}' OnCollisionEnter '{collision.collider.name}' tag='{collision.collider.tag}' " +
                      $"isBullet={collision.collider.CompareTag("Bullet")}", this);

        if (!collision.collider.CompareTag("Bullet"))
            return;

        Activate();
    }

    // Public so a bullet, the trunk, or any other interactor can flip the lever.
    public void Activate()
    {
        isOn = !isOn;
        UpdateVisual();

        if (targetPlatform != null)
            targetPlatform.ToggleRicochet();

        if (debugLogs)
            Debug.Log($"[Lever] '{name}' toggled -> isOn={isOn} (targetPlatform={(targetPlatform != null)})", this);
    }

    void UpdateVisual()
    {
        if (rend == null) return;

        rend.material = isOn ? onMaterial : offMaterial;
    }
}
