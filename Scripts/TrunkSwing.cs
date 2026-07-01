using System.Collections.Generic;
using UnityEngine;

// TrunkSwing - procedural elephant trunk, ported from the browser feel-test.
//
// It runs in two layers, exactly like the mockup:
//   1) SIMULATION (physics): a Verlet particle chain that reaches toward the mouse,
//      keeps its segment lengths, and COLLIDES with the world so the trunk wraps
//      around obstacles. The tip can also be pinned to "grab" an object.
//   2) SMOOTHING (spline): a Catmull-Rom curve is fit through the coarse sim
//      particles, then the visible bone Transforms are distributed along that curve
//      so the trunk looks smooth (no kinks) even with only a handful of sim points.
//
// Drop-in note: the serialized 'chain' list is kept (same name/type as the old FABRIK
// version) so the bone Transforms already assigned in the scene stay wired up.
public class TrunkSwing : MonoBehaviour
{
    [Header("Bones (visible trunk segments, base -> tip)")]
    [SerializeField] private List<Transform> chain = new List<Transform>();
    [SerializeField] private bool orientBones = true;   // rotate each bone along the curve

    [Header("Simulation")]
    [SerializeField] private int simPoints = 8;         // Verlet particles (coarse); min 4
    [SerializeField] private float trunkLength = 4.5f;  // total rest length of the trunk
    [SerializeField] private int iterations = 12;       // constraint passes = stiffness
    [SerializeField] private float damping = 0.98f;
    [SerializeField] private Vector3 gravity = new Vector3(0f, -9.81f, 0f);
    [SerializeField] private float gravityScale = 0f;   // 0 = trunk ignores gravity (reach mode)

    [Header("Reach (tip follows the mouse)")]
    [SerializeField] private bool followMouse = true;
    [Range(0f, 1f)]
    [SerializeField] private float reachStrength = 0.5f;
    [SerializeField] private Camera cam;

    [Header("Wrapping (collision)")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float trunkRadius = 0.25f;

    [Header("Grab & swing")]
    [Tooltip("When on, the tip pins in place and the trunk holds; call Grab()/Release() from gameplay.")]
    [SerializeField] private bool grabMode = false;

    // --- Verlet state -------------------------------------------------------
    private Vector3[] pos;      // current particle positions
    private Vector3[] prev;     // previous positions (Verlet velocity = pos - prev)
    private float seg;          // rest length between adjacent sim particles
    private Vector3 target;     // reach target on the ground plane
    private bool pinned;        // is the tip currently pinned?
    private Vector3 pinPoint;   // where the tip is pinned

    private void Start()
    {
        if (cam == null) cam = Camera.main;
        InitSim();
    }

    private void InitSim()
    {
        simPoints = Mathf.Max(4, simPoints);
        pos = new Vector3[simPoints];
        prev = new Vector3[simPoints];
        seg = trunkLength / (simPoints - 1);

        Vector3 root = transform.position;
        Vector3 dir = transform.forward;
        for (int i = 0; i < simPoints; i++)
        {
            pos[i] = root + dir * seg * i;
            prev[i] = pos[i];
        }
        pinned = false;
    }

    private void Update()
    {
        if (pos == null || pos.Length != simPoints) InitSim();

        UpdateTarget();
        Integrate();
        for (int k = 0; k < iterations; k++) SolveConstraints();
        ApplyToBones();
    }

    private void UpdateTarget()
    {
        if (!followMouse || cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, transform.position);
        if (plane.Raycast(ray, out float enter))
            target = ray.GetPoint(enter);
    }

    // Verlet integration: x += (x - prevX) * damping + gravity.
    private void Integrate()
    {
        Vector3 g = gravity * gravityScale * (Time.deltaTime * Time.deltaTime);
        for (int i = 0; i < simPoints; i++)
        {
            Vector3 temp = pos[i];
            Vector3 vel = (pos[i] - prev[i]) * damping;
            pos[i] += vel + g;
            prev[i] = temp;
        }
    }

    private void SolveConstraints()
    {
        // Anchor. Reach mode locks the base to the elephant; grab mode pins the tip
        // so the rest of the trunk (and, if you drive it, the body) can swing around it.
        if (grabMode)
        {
            if (!pinned) { pinPoint = pos[simPoints - 1]; pinned = true; }
            pos[simPoints - 1] = pinPoint;
        }
        else
        {
            pinned = false;
            pos[0] = transform.position;
        }

        // Distance constraints keep each segment at rest length. The fixed end gets a
        // double correction so the chain relaxes away from it instead of dragging it.
        for (int i = 0; i < simPoints - 1; i++)
        {
            Vector3 delta = pos[i + 1] - pos[i];
            float d = delta.magnitude;
            if (d < 1e-5f) continue;

            float diff = (d - seg) / d * 0.5f;
            Vector3 off = delta * diff;

            bool aFixed = (!grabMode && i == 0);
            bool bFixed = (grabMode && i + 1 == simPoints - 1);
            if (aFixed) pos[i + 1] -= off * 2f;
            else if (bFixed) pos[i] += off * 2f;
            else { pos[i] += off; pos[i + 1] -= off; }
        }

        // Reach: soft-pull the tip toward the mouse target (IK-like feel, done as a force).
        if (followMouse && !grabMode)
        {
            int t = simPoints - 1;
            pos[t] = Vector3.Lerp(pos[t], target, reachStrength);
            pos[0] = transform.position;
        }

        // Collision is what makes the trunk WRAP: push every particle out of any collider.
        for (int i = 0; i < simPoints; i++)
        {
            Collider[] hits = Physics.OverlapSphere(
                pos[i], trunkRadius, obstacleMask, QueryTriggerInteraction.Ignore);

            for (int h = 0; h < hits.Length; h++)
            {
                Vector3 closest = hits[h].ClosestPoint(pos[i]);
                Vector3 push = pos[i] - closest;
                float pd = push.magnitude;

                if (pd > 1e-5f && pd < trunkRadius)
                    pos[i] += push / pd * (trunkRadius - pd);          // outside: push out to skin
                else if (pd <= 1e-5f)
                    pos[i] += (pos[i] - hits[h].bounds.center).normalized * trunkRadius; // inside: eject
            }
        }
    }

    // SMOOTHING: distribute the visible bones along a Catmull-Rom spline through the
    // sim particles, so a coarse 8-point sim renders as a smooth, kink-free trunk.
    private void ApplyToBones()
    {
        if (chain == null || chain.Count == 0) return;

        int n = chain.Count;
        for (int i = 0; i < n; i++)
        {
            float u = (n == 1) ? 0f : (float)i / (n - 1);   // 0..1 along the trunk
            Vector3 p = SampleSpline(u);
            chain[i].position = p;

            if (orientBones)
            {
                Vector3 ahead = SampleSpline(Mathf.Min(1f, u + 0.01f));
                Vector3 tangent = ahead - p;
                if (tangent.sqrMagnitude > 1e-6f)
                    chain[i].rotation = Quaternion.LookRotation(tangent.normalized, Vector3.up);
            }
        }
    }

    // Catmull-Rom across the sim particles; u in 0..1 spans the whole trunk. The curve
    // passes exactly through every particle (interpolating), so bones sit on the solved
    // physics positions while the in-between bones are smoothly filled in.
    private Vector3 SampleSpline(float u)
    {
        int segs = simPoints - 1;
        float x = Mathf.Clamp01(u) * segs;
        int i = Mathf.Min((int)x, segs - 1);
        float t = x - i;

        Vector3 p0 = pos[Mathf.Max(i - 1, 0)];
        Vector3 p1 = pos[i];
        Vector3 p2 = pos[i + 1];
        Vector3 p3 = pos[Mathf.Min(i + 2, simPoints - 1)];
        return CatmullRom(p0, p1, p2, p3, t);
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * ((2f * p1)
            + (-p0 + p2) * t
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    // ---- Public API for the game to trigger the swing mechanic -------------
    // Pin the tip at a specific world point (e.g. a branch the trunk grabbed).
    public void Grab(Vector3 worldPoint)
    {
        grabMode = true;
        pinned = true;
        pinPoint = worldPoint;
        if (pos != null && pos.Length > 0) pos[pos.Length - 1] = worldPoint;
    }

    // Pin the tip wherever it currently is.
    public void Grab()
    {
        grabMode = true;
        pinned = false;   // captured on next solve
    }

    public void Release()
    {
        grabMode = false;
        pinned = false;
    }

    public bool IsGrabbing => grabMode;
    public Vector3 TipPosition => (pos != null && pos.Length > 0) ? pos[pos.Length - 1] : transform.position;

    private void OnDrawGizmosSelected()
    {
        if (pos == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < pos.Length; i++)
        {
            Gizmos.DrawWireSphere(pos[i], trunkRadius);
            if (i > 0) Gizmos.DrawLine(pos[i - 1], pos[i]);
        }
    }
}
