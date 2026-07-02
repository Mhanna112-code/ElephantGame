using System.Collections.Generic;
using UnityEngine;

public class TrunkSwing : MonoBehaviour
{
    [Header("Bones (base -> tip)")]
    [SerializeField] private List<Transform> chain = new List<Transform>();
    [SerializeField] private bool orientBones = true;

    [Header("Simulation")]
    [SerializeField] private int simPoints = 8;
    [SerializeField] private float trunkLength = 4.5f;
    [SerializeField] private bool autoLengthFromBones = true;
    [SerializeField] private int iterations = 10;

    [Header("Motion")]
    [SerializeField] private float damping = 0.98f;
    [SerializeField] private Vector3 gravity = new Vector3(0, -9.81f, 0);
    [SerializeField] private float gravityScale = 0f;

    [Header("Mouse")]
    [SerializeField] private bool followMouse = true;
    [SerializeField] private float reachStrength = 0.5f;
    [SerializeField] private Camera cam;

    [Header("Collision")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [SerializeField] private float radius = 0.25f;

    [Header("Grab")]
    [SerializeField] private bool grabMode;
    private Vector3 pinPoint;

    // simulation
    private Vector3[] pos;
    private Vector3[] prev;
    private float seg;

    private Vector3 target;
    private Vector3 prevRoot;

    private void Start()
    {
        if (cam == null) cam = Camera.main;
        Init();
    }

    private void Init()
    {
        simPoints = Mathf.Max(4, simPoints);

        pos = new Vector3[simPoints];
        prev = new Vector3[simPoints];

        if (autoLengthFromBones && chain != null && chain.Count >= 2)
        {
            float sum = 0f;
            for (int i = 0; i < chain.Count - 1; i++)
                sum += Vector3.Distance(chain[i].position, chain[i + 1].position);

            if (sum > 0.01f)
                trunkLength = sum;
        }

        seg = trunkLength / (simPoints - 1);

        Vector3 root = transform.position;
        Vector3 dir = transform.forward;

        for (int i = 0; i < simPoints; i++)
        {
            pos[i] = root + dir * seg * i;
            prev[i] = pos[i];
        }

        prevRoot = root;
    }

    private void Update()
    {
        if (pos == null || pos.Length != simPoints)
            Init();

        HandleTarget();
        CarryBody();
        Integrate();
        Solve();
        Apply();
    }

    // ---------------- TARGET ----------------
    void HandleTarget()
    {
        if (!followMouse || cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, transform.position);

        if (plane.Raycast(ray, out float enter))
            target = ray.GetPoint(enter);
    }

    // ---------------- BODY CARRY ----------------
    void CarryBody()
    {
        Vector3 root = transform.position;
        Vector3 delta = root - prevRoot;

        if (!grabMode && delta.sqrMagnitude > 0f)
        {
            for (int i = 0; i < simPoints; i++)
            {
                pos[i] += delta;
                prev[i] += delta;
            }
        }

        prevRoot = root;
    }

    // ---------------- VERLET ----------------
    void Integrate()
    {
        Vector3 g = gravity * gravityScale * Time.deltaTime * Time.deltaTime;

        for (int i = 0; i < simPoints; i++)
        {
            Vector3 temp = pos[i];
            Vector3 vel = (pos[i] - prev[i]) * damping;

            pos[i] += vel + g;
            prev[i] = temp;
        }
    }

    // ---------------- CONSTRAINT SOLVER ----------------
    void Solve()
    {
        int n = simPoints;

        // 🔥 HARD ROOT LOCK
        pos[0] = transform.position;
        prev[0] = pos[0];

        // TIP
        if (grabMode)
            pos[n - 1] = pinPoint;
        else if (followMouse)
            pos[n - 1] = Vector3.Lerp(pos[n - 1], target, reachStrength);

        // 🔥 IMPORTANT: full iterative constraint solve
        for (int it = 0; it < iterations; it++)
        {
            // BACKWARD
            for (int i = n - 2; i >= 0; i--)
            {
                Vector3 dir = pos[i] - pos[i + 1];
                float d = dir.magnitude;
                if (d < 0.00001f) continue;

                float diff = seg - d;
                dir /= d;

                if (i == 0)
                {
                    pos[i + 1] -= dir * diff; // root fixed
                }
                else
                {
                    pos[i]     += dir * diff * 0.5f;
                    pos[i + 1] -= dir * diff * 0.5f;
                }
            }

            // FORWARD
            for (int i = 1; i < n; i++)
            {
                Vector3 dir = pos[i] - pos[i - 1];
                float d = dir.magnitude;
                if (d < 0.00001f) continue;

                float diff = seg - d;
                dir /= d;

                pos[i - 1] -= dir * diff * 0.5f;
                pos[i]     += dir * diff * 0.5f;
            }
        }

        // FINAL ROOT LOCK (prevents drift)
        pos[0] = transform.position;
    }

    // ---------------- APPLY ----------------
    void Apply()
    {
        if (chain == null || chain.Count == 0) return;

        for (int i = 0; i < chain.Count; i++)
        {
            float t = (chain.Count == 1) ? 0 : (float)i / (chain.Count - 1);
            Vector3 p = Sample(t);

            chain[i].position = p;

            if (orientBones && i < chain.Count - 1)
            {
                Vector3 dir = Sample(Mathf.Min(1f, t + 0.02f)) - p;
                if (dir.sqrMagnitude > 0.00001f)
                    chain[i].rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }
    }

    // ---------------- SIMPLE INTERPOLATION ----------------
    Vector3 Sample(float t)
    {
        float x = t * (simPoints - 1);
        int i = Mathf.Min((int)x, simPoints - 2);
        float u = x - i;

        return Vector3.Lerp(pos[i], pos[i + 1], u);
    }

    // ---------------- PUBLIC ----------------
    public void Grab(Vector3 p)
    {
        grabMode = true;
        pinPoint = p;
    }

    public void Release()
    {
        grabMode = false;
    }

    public bool IsGrabbing => grabMode;
}