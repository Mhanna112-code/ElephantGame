using UnityEngine;

// A barrier that blocks the player until its TargetGroup is solved. Open() disables the
// colliders and hides the renderers.
public class Gate : MonoBehaviour
{
    public bool startClosed = true;
    public bool IsOpen { get; private set; }

    Collider[] cols;
    Renderer[] rends;

    void Awake()
    {
        cols = GetComponentsInChildren<Collider>();
        rends = GetComponentsInChildren<Renderer>();
        SetOpen(!startClosed);
    }

    public void Open()  { SetOpen(true); }
    public void Close() { SetOpen(false); }

    void SetOpen(bool open)
    {
        IsOpen = open;
        if (cols != null) foreach (var c in cols) if (c) c.enabled = !open;
        if (rends != null) foreach (var r in rends) if (r) r.enabled = !open;
    }
}
