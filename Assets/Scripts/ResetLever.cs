using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ResetLever : MonoBehaviour
{
    [Header("Cutscene")]
    public CutsceneCamera cutsceneCamera;

    [Header("Lever Animation")]
    public float rotateAngle = 45f;
    public float rotateSpeed = 180f;

    [Header("Objects To Reset")]
    public List<Transform> objectsToReset = new List<Transform>();

    private Vector3[] startPositions;
    private Quaternion[] startRotations;

    private Quaternion startRotation;
    private Quaternion downRotation;

    private bool activated = false;

    void Start()
    {
        startRotation = transform.rotation;
        downRotation = startRotation * Quaternion.Euler(0f, 0f, -rotateAngle);

        startPositions = new Vector3[objectsToReset.Count];
        startRotations = new Quaternion[objectsToReset.Count];

        for (int i = 0; i < objectsToReset.Count; i++)
        {
            startPositions[i] = objectsToReset[i].position;
            startRotations[i] = objectsToReset[i].rotation;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (activated)
            return;
        Debug.Log("collided with trunk");
        if (!collision.collider.CompareTag("Bullet"))
            return;

        activated = true;
        StartCoroutine(ActivateLever());
    }

    IEnumerator ActivateLever()
    {
        // Play cutscene
        if (cutsceneCamera != null)
            cutsceneCamera.PlayCutscene();

        // Rotate lever down
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
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // Rotate lever back up
        yield return RotateTo(startRotation);

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