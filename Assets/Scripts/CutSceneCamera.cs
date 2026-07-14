using UnityEngine;
using System.Collections;

public class CutsceneCamera : MonoBehaviour
{
    public Transform cameraTransform;
    public Transform player;

    public float heightOffset = 10f;
    public float duration = 1.5f;

    private Vector3 followOffset;
    private Quaternion originalRotation;

    private bool followingTopDown;

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        // Remember the normal gameplay offset
        followOffset = cameraTransform.position - player.position;
        originalRotation = cameraTransform.rotation;
    }

    void LateUpdate()
    {
        // Keep the top-down view locked to the player's current position
        // instead of freezing wherever the transition-in coroutine left it.
        if (!followingTopDown)
            return;

        cameraTransform.position = player.position + Vector3.up * heightOffset;
        cameraTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    public void PlayCutscene()
    {
        StartCoroutine(Play());
    }

    public void ResetCameraView()
    {
        followingTopDown = false;
        StartCoroutine(ResetView());
    }

    IEnumerator Play()
    {
        GamePause.Pause();

        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        Quaternion topDownRot = Quaternion.Euler(90f, 0f, 0f);

        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;

            // Keep following the player while moving overhead
            Vector3 targetPos = player.position + Vector3.up * heightOffset;

            cameraTransform.position = Vector3.Lerp(startPos, targetPos, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, topDownRot, t);

            yield return null;
        }

        followingTopDown = true;

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

            // Return to the player's current position + original offset
            Vector3 targetPos = player.position + followOffset;

            cameraTransform.position = Vector3.Lerp(startPos, targetPos, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, originalRotation, t);

            yield return null;
        }

        cameraTransform.position = player.position + followOffset;
        cameraTransform.rotation = originalRotation;
    }
}
