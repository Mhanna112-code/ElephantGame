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

    // ---- Diagnostics (leave these on; this is how we find why the trunk freezes) ----
    [Header("Diagnostics")]
    public bool debugLogs = true;
    public int logEveryNFrames = 15;    // throttle the per-frame spam (~4x/sec at 60fps)

    private Vector3 lastTrunkPos;
    private int notMovingFrames;

    void Start()
    {
        if (cam == null)
            cam = Camera.main;

        if (trunkConstraint != null)
            lastTrunkPos = trunkConstraint.position;

        if (debugLogs)
            Debug.Log($"[TrunkSwing] Start: cam={(cam != null ? cam.name : "NULL")} player={(player != null)} " +
                      $"trunkConstraint={(trunkConstraint != null)} platformMask={platformLayer.value} " +
                      $"checkDistance={checkDistance} camOrthographic={(cam != null && cam.orthographic)}", this);
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

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Log WHAT the downward ray hits, not just true/false, so we can see if platform detection works.
        Vector3 targetPos;
        float xyMag = -1f;

        PlayerController playerController = player.GetComponent<PlayerController>();

        if (playerController.IsAbovePlatform)
        {
            Plane plane = new Plane(Vector3.up, player.position);

            if (plane.Raycast(ray, out float enter))
            {
                Vector3 mouseWorld = ray.GetPoint(enter);

                Vector3 offset = mouseWorld - player.position;
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
            // 🔒 FLAT PLANE AIM (old behavior)
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

        // --- Do the move and MEASURE it. lerpT uses Time.deltaTime, so if the game is paused
        //     (a cutscene set Time.timeScale = 0), lerpT == 0 and the trunk cannot move at all. ---
        float lerpT = Time.deltaTime * smoothSpeed;
        Vector3 before = trunkConstraint.position;
        trunkConstraint.position = Vector3.Lerp(before, targetPos, lerpT);
        float movedThisFrame = (trunkConstraint.position - before).magnitude;

        if (movedThisFrame < 0.0005f) notMovingFrames++; else notMovingFrames = 0;

        if (debugLogs && (Time.frameCount % logEveryNFrames == 0))
            Debug.Log($"[TrunkSwing] isAbove={playerController.IsAbovePlatform}" +
                      $"timeScale={Time.timeScale:F2} dt={Time.deltaTime:F4} lerpT={lerpT:F3} xyMag={xyMag:F3} " +
                      $"mouse={Input.mousePosition} camActiveEnabled={cam.isActiveAndEnabled} " +
                      $"aimDist={(targetPos - player.position).magnitude:F3} movedThisFrame={movedThisFrame:F4} " +
                      $"trunk={trunkConstraint.position} target={targetPos} player={player.position}", this);

        // After ~half a second of no movement, print a loud line with the LIKELY cause spelled out.
        if (debugLogs && notMovingFrames == 30)
        {
            string cause =
                Time.deltaTime < 0.0001f
                    ? "Time.deltaTime ~0 -> game paused / cutscene set Time.timeScale=0, so Lerp(...,dt*speed)=Lerp(...,0) never moves the trunk"
                : !cam.isActiveAndEnabled
                    ? "the aim camera is disabled (a cutscene camera likely took over) -> ScreenPointToRay is stale"
                : (targetPos - player.position).magnitude < 0.05f
                    ? "aim target collapsed onto the player (ABOVE branch: ray.direction has ~0 XY after zeroing Z on a side-on camera)"
                : (targetPos - trunkConstraint.position).magnitude < 0.001f
                    ? "target == current trunk position (nothing to move toward - mouse/aim input not updating?)"
                : "UNKNOWN: target differs from trunk yet Lerp is not moving it";

        }

        lastTrunkPos = trunkConstraint.position;
    }
}
