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

    private Quaternion startRotation;
    private Quaternion downRotation;

    private bool activated = false;

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
    }

    void OnCollisionStay(Collision collision)
    {
        if (activated)
            return;

        if (!collision.collider.CompareTag("Trunk"))
        {
            Debug.Log("Not trunk! Tag is: " + collision.collider.tag);
            return;
        }

        // Average contact normal
        Vector3 normal = Vector3.zero;

        foreach (ContactPoint contact in collision.contacts)
        {
            normal += contact.normal;
        }

        normal.Normalize();

        // Direction trunk is pushing
        Vector3 pushDirection = -normal;

        // Is it pushing along the lever's negative local X axis?
        float pushAmount = Vector3.Dot(pushDirection, -transform.right);

        Debug.Log("Push amount: " + pushAmount);

        if (pushAmount > 0)
        {
            leverRenderer.material.color = activatedColor;
            Debug.Log("Lever activated!");
            activated = true;
            StartCoroutine(ActivateLever());
        }
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

        // Small pause while pressed
        yield return new WaitForSeconds(0.2f);

        // Return lever
        yield return RotateTo(startRotation);
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