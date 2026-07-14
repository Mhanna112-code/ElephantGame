using UnityEngine;

// Fix for issue #35 (camera won't stay on the player / whips around when the player rotates).
//
// ROOT CAUSE: the Main Camera is a CHILD of the player MODEL (Elephant_Girl), and PlayerController.Move()
// flips playerModel.localRotation 180 degrees between (-90,0,-90) and (-90,0,90) to face the player
// left/right. Being a child of that model, the camera INHERITS the 180 degree flip and swings around.
//
// FIX: every LateUpdate (after movement + the facing flip have run), force the camera's WORLD position and
// WORLD rotation. Setting the world transform overrides whatever rotation it inherited from the parent, so
// the camera follows the player's POSITION but keeps a fixed orientation. No re-parenting needed.
//
// Set `active = false` (or call SetActive(false)) to hand the camera to something else, e.g. the top-down
// CutsceneCamera when the player is above the platform.
[DefaultExecutionOrder(1000)]
public class FollowCamera : MonoBehaviour
{
    [Header("References")]
    [Tooltip("What to follow (the player root). Leave empty to auto-find the PlayerController.")]
    public Transform target;

    [Header("Framing")]
    [Tooltip("If true, capture the camera's current world offset + rotation at Start as the fixed side view. " +
             "If false, use the worldOffset/fixedEuler values below.")]
    public bool captureOnStart = true;
    public Vector3 worldOffset;   // camera world position minus target world position
    public Vector3 fixedEuler;    // the camera's fixed world rotation (euler)

    [Header("Control")]
    [Tooltip("When false, this script stops driving the camera (e.g. so the top-down cutscene can take over).")]
    public bool active = true;

    [Header("Diagnostics")]
    public bool debugLogs = true;
    public int logEveryNFrames = 30;

    void Start()
    {
        if (target == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) target = pc.transform;
        }

        if (target == null)
        {
            Debug.LogError("[FollowCamera] No target set and no PlayerController found -> camera will NOT follow. " +
                           "Assign 'target' to the player root.", this);
            enabled = false;
            return;
        }

        if (captureOnStart)
        {
            worldOffset = transform.position - target.position;
            fixedEuler = transform.rotation.eulerAngles;
        }

        if (debugLogs)
            Debug.Log($"[FollowCamera] Start: target='{target.name}' parent='{(transform.parent != null ? transform.parent.name : "none")}' " +
                      $"capturedWorldOffset={worldOffset} fixedEuler={fixedEuler}. " +
                      $"The camera is parented under the rotating player model; this script overrides that rotation every frame.", this);
    }

    void LateUpdate()
    {
        if (!active || target == null)
            return;

        Quaternion beforeRot = transform.rotation;   // what it inherited from the parent this frame

        // Force world transform: follow position, keep a FIXED rotation (immune to the player's facing flip).
        transform.position = target.position + worldOffset;
        transform.rotation = Quaternion.Euler(fixedEuler);

        if (debugLogs && Time.frameCount % Mathf.Max(1, logEveryNFrames) == 0)
            Debug.Log($"[FollowCamera] targetPos={target.position} -> camPos={transform.position} " +
                      $"inheritedEuler={beforeRot.eulerAngles} forcedEuler={transform.eulerAngles} " +
                      $"(if inherited != forced, we just corrected a player-rotation leak into the camera)", this);
    }

    // Public hook so OneWayPlatform / CutsceneCamera can hand the camera off for the top-down view.
    public void SetActive(bool value)
    {
        active = value;
        if (debugLogs) Debug.Log($"[FollowCamera] active -> {value}", this);
    }
}
