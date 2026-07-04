using UnityEngine;

// A single red button. Turns blue when hit this attempt, green when its group is solved,
// dim red while its group is still dormant. Color is set via a MaterialPropertyBlock so it
// works in URP (_BaseColor) and built-in (_Color) without instancing materials.
public class Target : MonoBehaviour
{
    [HideInInspector] public int order;             // 1..N, assigned by TargetGroup
    [HideInInspector] public TargetGroup group;     // owning group, assigned by TargetGroup

    public Color unhitColor   = Color.red;
    public Color hitColor     = new Color(0.49f, 0.77f, 1f);
    public Color solvedColor  = new Color(0.37f, 0.86f, 0.55f);
    public Color dormantColor = new Color(0.35f, 0.13f, 0.13f);

    Renderer rend;
    MaterialPropertyBlock mpb;

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    public void SetColor(Color c)
    {
        if (rend == null) rend = GetComponentInChildren<Renderer>();
        if (rend == null) return;
        rend.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", c);   // URP lit/unlit
        mpb.SetColor("_Color", c);       // built-in
        rend.SetPropertyBlock(mpb);
    }
}
