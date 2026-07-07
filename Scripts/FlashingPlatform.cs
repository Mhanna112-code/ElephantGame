using UnityEngine;

public class FlashingPlatform : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;

    private int direction = 1;

    [Header("Wall Detection")]
    public float detectDistance = 0.6f;
    public LayerMask wallLayer;

    [Header("Flashing")]
    public float flashInterval = 1f;

    [Header("Appearance")]
    public Renderer rend;
    public Material redMat;
    public Material greenMat;

    [HideInInspector]
    public bool ricochetEnabled;

    [Header("Diagnostics")]
    public bool debugLogs = true;

    private float timer;

    void Start()
    {
        ricochetEnabled = false;      // Start RED
        UpdateColor();
    }

    void Update()
    {
        //---------------------------------
        // Flash between red and green
        //---------------------------------

        timer += Time.deltaTime;

        if (timer >= flashInterval)
        {
            timer = 0f;
            ricochetEnabled = !ricochetEnabled;
            UpdateColor();
            if (debugLogs)
                Debug.Log($"[FlashingPlatform] '{name}' -> {(ricochetEnabled ? "GREEN (bullets bounce)" : "RED (bullets absorbed)")}", this);
        }
    }

    void UpdateColor()
    {
        if (rend == null)
        {
            Debug.LogError("Renderer not assigned!");
            return;
        }

        rend.material = ricochetEnabled ? greenMat : redMat;
    }
}