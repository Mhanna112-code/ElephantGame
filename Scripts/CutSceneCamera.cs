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

    // The camera is a CHILD of the player, so the cutscene setting its WORLD position permanently
    // corrupts its authored local offset (the "camera too close/broken" bug). Remember the original
    // LOCAL transform so we can put the side-view camera back when leaving the top-down section.
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private bool savedOriginal = false;

    void Start()
    {
        if (cameraTransform != null)
        {
            originalLocalPos = cameraTransform.localPosition;
            originalLocalRot = cameraTransform.localRotation;
            savedOriginal = true;
        }
    }

    // Put the camera back to its authored side-view offset. Called when the player drops below the
    // platform (OneWayPlatform). Aborts any in-progress cutscene and makes sure the game is unpaused.
    public void RestoreCamera()
    {
        if (!savedOriginal || cameraTransform == null) return;

        StopAllCoroutines();
        if (GamePause.IsPaused) GamePause.Resume();
        playing = false;

        cameraTransform.localPosition = originalLocalPos;
        cameraTransform.localRotation = originalLocalRot;

        if (debugLogs) Debug.Log("[Cutscene] RestoreCamera -> back to side-view offset", this);
    }

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
