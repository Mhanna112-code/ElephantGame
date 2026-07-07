using UnityEngine;
using System.Collections;

public class CutsceneCamera : MonoBehaviour
{
    public Transform cameraTransform;

    public Vector3 cutscenePosition;
    public float heightOffset = 10f;
    public Vector3 cutsceneRotation; // Euler angles

    public float duration = 1.5f;

    [Header("Diagnostics")]
    public bool debugLogs = true;

    private bool playing = false;

    public void PlayCutscene()
    {
        if (debugLogs)
            Debug.Log($"[Cutscene] PlayCutscene() called (playing={playing}, cameraTransform={(cameraTransform != null)})", this);

        if (playing)
        {
            if (debugLogs) Debug.Log("[Cutscene] already playing -> ignored", this);
            return;
        }
        if (cameraTransform == null)
        {
            Debug.LogWarning("[Cutscene] cameraTransform not assigned -> cannot play the cutscene", this);
            return;
        }

        // FIX: this StartCoroutine was commented out, so PlayCutscene did nothing and the camera
        // never went top-down above the platform. It is re-enabled and made safe below.
        StartCoroutine(Play());
    }

    IEnumerator Play()
    {
        playing = true;

        if (debugLogs) Debug.Log("[Cutscene] START -> pausing game (timeScale=0)", this);
        GamePause.Pause();

        // try/finally guarantees the game ALWAYS resumes, even if this object is disabled or the
        // coroutine is interrupted mid-play. Without it, an interrupted cutscene left timeScale=0
        // and froze the whole game - the likely reason this was commented out in the first place.
        try
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            Transform player = playerObj != null ? playerObj.transform : null;

            Vector3 startPos = cameraTransform.position;
            Quaternion startRot = cameraTransform.rotation;

            Vector3 targetPos = (player != null ? player.position : cameraTransform.position) + Vector3.up * heightOffset;
            Quaternion topDownRot = Quaternion.Euler(90f, 0f, 0f);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
                cameraTransform.position = Vector3.Lerp(startPos, targetPos, t);
                cameraTransform.rotation = Quaternion.Slerp(startRot, topDownRot, t);
                yield return null;
            }

            cameraTransform.position = targetPos;
            cameraTransform.rotation = topDownRot;
        }
        finally
        {
            GamePause.Resume();
            playing = false;
            if (debugLogs) Debug.Log("[Cutscene] END -> resumed (timeScale=1)", this);
        }
    }
}
