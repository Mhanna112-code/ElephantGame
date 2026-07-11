using UnityEngine;
using System.Collections;

public class CutsceneCamera : MonoBehaviour
{
    public Transform cameraTransform;

    public Vector3 cutscenePosition;
    public float heightOffset = 10f;
    public Vector3 cutsceneRotation; // Euler angles

    public float duration = 1.5f;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    void Start()
    {
        originalPosition = cameraTransform.position;
        originalRotation = cameraTransform.rotation;
    }
    public void PlayCutscene()
    {
        StartCoroutine(Play());
    }

    public void ResetCameraView()
    {
        StartCoroutine(ResetView());
    }

    IEnumerator Play()
    {
        GamePause.Pause();

        Transform player = GameObject.FindGameObjectWithTag("Player").transform;

        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        Vector3 targetPos = player.position + Vector3.up * heightOffset;
        Quaternion topDownRot = Quaternion.Euler(90f, 0f, 0f);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;

            cameraTransform.position = Vector3.Lerp(startPos, targetPos, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, topDownRot, t);

            yield return null;
        }

        cameraTransform.position = targetPos;
        cameraTransform.rotation = topDownRot;

        GamePause.Resume();
    }

    IEnumerator ResetView()
    {
        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;

            cameraTransform.position = Vector3.Lerp(startPos, originalPosition, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, originalRotation, t);

            yield return null;
        }

        cameraTransform.position = originalPosition;
        cameraTransform.rotation = originalRotation;
    }
}