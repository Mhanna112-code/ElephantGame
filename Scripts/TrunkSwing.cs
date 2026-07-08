using UnityEngine;

public class TrunkSwing : MonoBehaviour
{
    public Camera cam;

    public Transform player;
    public Transform trunkConstraint;

    public float maxReach = 4f;
    public float smoothSpeed = 12f;

    [Header("Platform Check")]
    public LayerMask platformLayer;
    public float checkDistance = 1.5f;

    [Header("Climbing")]
    public bool isGrabbing = false;
    public Transform currentGrabPoint;

    [Header("Diagnostics")]
    public bool debugLogs = false;
    public int logEveryNFrames = 15;

    private PlayerController playerController;
    private int notMovingFrames;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        if (player != null)
            playerController = player.GetComponent<PlayerController>();

        if (debugLogs)
            Debug.Log($"[TrunkSwing] Start: cam={(cam != null ? cam.name : "NULL")} player={(player != null)} " +
                      $"trunkConstraint={(trunkConstraint != null)} playerController={(playerController != null)} " +
                      $"platformMask={platformLayer.value} checkDistance={checkDistance} " +
                      $"camOrthographic={(cam != null && cam.orthographic)}", this);

        if (playerController == null)
            Debug.LogWarning("[TrunkSwing] No PlayerController on the 'player' transform. IsAbovePlatform will be treated as false.", this);
    }

    void Update()
    {
        if (cam == null || player == null || trunkConstraint == null)
        {
            if (debugLogs)
                Debug.LogWarning($"[TrunkSwing] MISSING REF cam={(cam != null)} player={(player != null)} " +
                                 $"trunkConstraint={(trunkConstraint != null)} -> trunk cannot aim.", this);
            return;
        }

        if (isGrabbing && currentGrabPoint != null)
        {
            Vector3 before = trunkConstraint.position;
            trunkConstraint.position = currentGrabPoint.position;

            float movedThisFrame = (trunkConstraint.position - before).magnitude;
            if (movedThisFrame < 0.0005f) notMovingFrames++;
            else notMovingFrames = 0;

            if (debugLogs && (Time.frameCount % logEveryNFrames == 0))
            {
                Debug.Log($"[TrunkSwing] GRABBING locked to {currentGrabPoint.name} " +
                          $"trunk={trunkConstraint.position} target={currentGrabPoint.position}", this);
            }

            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        bool isAbovePlatform = playerController != null && playerController.IsAbovePlatform;
        Vector3 targetPos;
        string branch;

        if (isAbovePlatform)
        {
            branch = "ABOVE(plane up)";
            Plane plane = new Plane(Vector3.up, player.position);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 offset = ray.GetPoint(enter) - player.position;
                offset = Vector3.ClampMagnitude(offset, maxReach);
                targetPos = player.position + offset;
            }
            else
            {
                targetPos = trunkConstraint.position;
            }
        }
        else
        {
            branch = "BELOW(plane fwd)";
            Plane plane = new Plane(Vector3.forward, player.position);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 offset = ray.GetPoint(enter) - player.position;
                offset.z = 0f;
                offset = Vector3.ClampMagnitude(offset, maxReach);
                targetPos = player.position + offset;
            }
            else
            {
                targetPos = trunkConstraint.position;
            }
        }

        float lerpT = Time.deltaTime * smoothSpeed;
        Vector3 beforeMove = trunkConstraint.position;
        trunkConstraint.position = Vector3.Lerp(beforeMove, targetPos, lerpT);
        float moved = (trunkConstraint.position - beforeMove).magnitude;

        if (moved < 0.0005f) notMovingFrames++;
        else notMovingFrames = 0;

        if (debugLogs && (Time.frameCount % logEveryNFrames == 0))
            Debug.Log($"[TrunkSwing] isAbove={isAbovePlatform} branch={branch} " +
                      $"timeScale={Time.timeScale:F2} dt={Time.deltaTime:F4} lerpT={lerpT:F3} " +
                      $"mouse={Input.mousePosition} camActive={cam.isActiveAndEnabled} " +
                      $"aimDist={(targetPos - player.position).magnitude:F3} movedThisFrame={moved:F4} " +
                      $"trunk={trunkConstraint.position} target={targetPos} player={player.position}", this);

        if (debugLogs && notMovingFrames == 30)
        {
            string cause =
                Time.deltaTime < 0.0001f
                    ? "Time.deltaTime ~0 -> paused / cutscene"
                : !cam.isActiveAndEnabled
                    ? "aim camera disabled"
                : (targetPos - player.position).magnitude < 0.05f
                    ? "aim target collapsed onto the player"
                : (targetPos - trunkConstraint.position).magnitude < 0.001f
                    ? "target == current trunk position"
                : "UNKNOWN";

            Debug.LogWarning($"[TrunkSwing] TRUNK NOT MOVING for {notMovingFrames} frames. LIKELY CAUSE: {cause}.", this);
        }
    }
}