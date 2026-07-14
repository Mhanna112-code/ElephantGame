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

    [Header("Idle Relax")]
    // After the mouse has been still this long, the trunk stops aiming at the stale
    // cursor position and relaxes to a neutral pose in front of the player. Without
    // this, a trunk-shot at the ground leaves the cursor (and therefore the trunk)
    // pointing backwards for the whole launch flight, which reads as the player
    // facing the wrong way.
    public float idleRelaxSeconds = 0.35f;
    // The cursor must move this far (net, in pixels) from its resting spot to count
    // as deliberate aiming. Hand tremor oscillates in place and never accumulates
    // net displacement, so this is robust to mouse DPI and framerate.
    public float mouseDeadzonePixels = 15f;
    public float neutralForward = 1.5f;
    public float neutralHeight = 1.0f;

    [Header("Diagnostics")]
    public bool debugLogs = false;
    public int logEveryNFrames = 15;

    private PlayerController playerController;
    private int notMovingFrames;
    private Vector3 lastMousePos;
    private float lastMouseActivityTime;
    private string lastBranch;

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

        Vector3 mousePos = Input.mousePosition;
        // lastMousePos is the anchor: where the cursor last settled. Net displacement
        // beyond the deadzone means deliberate aiming; in-place tremor never trips it.
        if ((mousePos - lastMousePos).sqrMagnitude > mouseDeadzonePixels * mouseDeadzonePixels)
        {
            lastMousePos = mousePos;
            lastMouseActivityTime = Time.time;
        }
        if (Input.GetMouseButton(0))
            lastMouseActivityTime = Time.time;
        if (Input.GetMouseButtonUp(0))
        {
            // The shot just fired: aiming intent is over. Relax the trunk right away
            // instead of leaving it pointing at the ground you launched off.
            lastMouseActivityTime = Time.time - idleRelaxSeconds;
            lastMousePos = mousePos;
        }

        Ray ray = cam.ScreenPointToRay(mousePos);

        // Pick the aim plane from the CAMERA's actual orientation, not IsAbovePlatform.
        // IsAbovePlatform is a height check, so a launch arc that rises past the
        // platform briefly flips it while the camera is still side-view — and a mouse
        // ray intersected with the wrong plane throws the trunk off the gameplay
        // plane (backwards into the screen).
        bool isAbovePlatform = Vector3.Dot(cam.transform.forward, Vector3.down) > 0.7f;
        Vector3 targetPos;
        string branch;

        // Airborne: never aim. The trunk is the character's face, and mid-flight the
        // cursor is usually wherever the launch shot was aimed — behind the player —
        // which makes her read as flying backwards. Bullets aim via GetMouseDirection
        // independently of the trunk visual, so shooting is unaffected.
        bool airborne = playerController != null && !playerController.IsGrounded;

        if (airborne || Time.time - lastMouseActivityTime > idleRelaxSeconds)
        {
            // Mouse idle: relax to a neutral pose ahead of the player. The model
            // faces -X at root yaw 0, so facing direction is -player.right.
            branch = "IDLE(neutral)";
            Vector3 facingDir = -player.right;
            targetPos = player.position + facingDir * neutralForward + Vector3.up * neutralHeight;
        }
        else if (isAbovePlatform)
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

        // Unconditional on-change log: which aim mode the trunk is in and where it
        // is being sent. A handful of lines per state change, not per frame.
        if (branch != lastBranch)
        {
            Debug.Log($"[Trunk] {lastBranch ?? "start"} -> {branch} target={targetPos} player={player.position} facingDir={-player.right} rootYaw={player.eulerAngles.y:F0}", this);
            lastBranch = branch;
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