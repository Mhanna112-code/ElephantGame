using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HorizontalResetLever : MonoBehaviour
{
    [Header("Cutscene")]
    public CutsceneCamera cutsceneCamera;

    [Header("Lever Animation")]
    public float rotateAngle = 45f;
    public float rotateSpeed = 180f;

    [Header("Push Detection")]
    [Range(0f, 1f)]
    public float requiredPush = 0.7f;

    [Header("Objects To Reset")]
    public List<Transform> objectsToReset = new List<Transform>();

    private Vector3[] startPositions;
    private Quaternion[] startRotations;
    public Renderer leverRenderer;
    public Color activatedColor = Color.green;
    public Color normalColor = Color.white;

    // ROOT CAUSE (issue #15): this lever detected the trunk with OnCollisionStay + CompareTag("Trunk").
    // The trunk is a KINEMATICALLY position-lerped transform (see TrunkSwing), so it does NOT reliably
    // generate physics collision events - the player could never "flick it on". The regular Lever.cs
    // already hit and solved this by detecting the trunk by PROXIMITY instead. Do the same here: assign
    // the trunk tip and the lever flips when the tip comes within interactRadius.
    [Header("Trunk Interaction (proximity - robust for the kinematically-lerped trunk)")]
    public Transform trunkTip;
    public float interactRadius = 0.4f;

    [Header("Diagnostics")]
    public bool debugLogs = true;

    private Quaternion startRotation;
    private Quaternion downRotation;

    private bool activated = false;
    private bool trunkInside = false;

    void Start()
    {
        startRotation = transform.rotation;

        // Rotate around the lever's LOCAL X axis
        downRotation = startRotation * Quaternion.Euler(0f, 0f, -rotateAngle);

        startPositions = new Vector3[objectsToReset.Count];
        startRotations = new Quaternion[objectsToReset.Count];

        for (int i = 0; i < objectsToReset.Count; i++)
        {
            startPositions[i] = objectsToReset[i].position;
            startRotations[i] = objectsToReset[i].rotation;
        }

        if (debugLogs)
            Debug.Log($"[ResetLever] Start '{name}': objectsToReset={objectsToReset.Count} " +
                      $"trunkTip={(trunkTip != null ? trunkTip.name : "NULL")} interactRadius={interactRadius} " +
                      $"leverRenderer={(leverRenderer != null)}", this);

        if (objectsToReset.Count == 0)
            Debug.LogWarning($"[ResetLever] '{name}' objectsToReset is EMPTY -> flicking will reset NOTHING. " +
                             $"Populate it with every second-floor object to reset (issue #15).", this);

        if (trunkTip == null)
            Debug.LogWarning($"[ResetLever] '{name}' trunkTip not assigned -> proximity flick cannot work. " +
                             $"Assign the trunk tip transform in the inspector.", this);
    }

    void Update()
    {
        if (activated || trunkTip == null)
            return;

        float d = Vector3.Distance(trunkTip.position, transform.position);
        bool inside = d <= interactRadius;

        if (inside && !trunkInside)
        {
            if (debugLogs)
                Debug.Log($"[ResetLever] '{name}' trunk entered (dist={d:F2} <= {interactRadius}) -> flick ON", this);
            Activate();
        }
        else if (debugLogs && Time.frameCount % 30 == 0 && d < interactRadius * 3f)
        {
            // helps tune interactRadius: shows how close the trunk actually gets
            Debug.Log($"[ResetLever] '{name}' trunk near (dist={d:F2}, need <= {interactRadius})", this);
        }

        trunkInside = inside;
    }

    // Secondary path: if the trunk ever DOES generate a real physics contact, honour it too.
    void OnCollisionStay(Collision collision)
    {
        if (activated)
            return;

        if (!collision.collider.CompareTag("Trunk"))
            return;

        if (debugLogs)
            Debug.Log($"[ResetLever] '{name}' OnCollisionStay TRUNK contact -> flick ON", this);

        Activate();
    }

    // Public so the trunk (proximity or contact) or any other interactor can flick the lever.
    public void Activate()
    {
        if (activated)
            return;

        activated = true;

        if (leverRenderer != null)
            leverRenderer.material.color = activatedColor;

        if (debugLogs)
            Debug.Log($"[ResetLever] '{name}' ACTIVATED -> resetting {objectsToReset.Count} objects", this);

        StartCoroutine(ActivateLever());
    }

    IEnumerator ActivateLever()
    {
        // Push lever down
        yield return RotateTo(downRotation);

        // Reset all objects
        for (int i = 0; i < objectsToReset.Count; i++)
        {
            Transform t = objectsToReset[i];

            t.position = startPositions[i];
            t.rotation = startRotations[i];

            Rigidbody rb = t.GetComponent<Rigidbody>();
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
#else
                rb.velocity = Vector3.zero;
#endif
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (debugLogs)
            Debug.Log($"[ResetLever] '{name}' reset {objectsToReset.Count} objects to their start pose", this);

        // Small pause while pressed
        yield return new WaitForSeconds(0.2f);

        // Return lever
        yield return RotateTo(startRotation);
        if (leverRenderer != null)
            leverRenderer.material.color = normalColor;

        activated = false;
    }

    IEnumerator RotateTo(Quaternion target)
    {
        while (Quaternion.Angle(transform.rotation, target) > 0.25f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                rotateSpeed * Time.deltaTime);

            yield return null;
        }

        transform.rotation = target;
    }
}