using UnityEngine;
using System.Collections;

public class CutsceneCamera : MonoBehaviour
{
    public Transform cameraTransform;

    public Vector3 cutscenePosition;
    public float heightOffset = 10f;
    public Vector3 cutsceneRotation; // Euler angles

    public float duration = 1.5f;

    public void PlayCutscene()
    {
        //StartCoroutine(Play());
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

            cameraTransform.rotation = Quaternion.Slerp(
                startRot,
                topDownRot,
                t
            );

            yield return null;
        }

        cameraTransform.position = targetPos;
        cameraTransform.rotation = topDownRot;

        GamePause.Resume();
    }
}