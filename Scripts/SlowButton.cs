using UnityEngine;

public class SlowButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FallingBlock fallingBlock;

    [Tooltip("All possible locations the button can teleport to.")]
    [SerializeField] private Transform[] buttonLocations;

    [Header("Gameplay")]
    [SerializeField] private float strengthRequired = 3f;
    [SerializeField] private float slowAmount = 0.75f;

    [Header("Visuals")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Color normalColor = Color.red;
    [SerializeField] private Color hitColor = Color.yellow;
    [SerializeField] private float yellowTime = 0.25f;

    private float currentStrength = 0f;
    private float timer;

    private void Start()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.material.color = normalColor;

        MoveToRandomLocation();
    }

    private void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                meshRenderer.material.color = normalColor;
            }
        }
    }

    /// <summary>
    /// Call this whenever water hits the button.
    /// strength = amount of water delivered.
    /// </summary>
    public void HitByWater(float strength)
    {
        currentStrength += strength;

        Debug.Log($"Button Strength: {currentStrength:F2}/{strengthRequired}");

        if (currentStrength >= strengthRequired)
        {
            currentStrength = 0f;

            if (fallingBlock != null)
                fallingBlock.ApplySlow(slowAmount);

            meshRenderer.material.color = hitColor;
            timer = yellowTime;

            MoveToRandomLocation();
        }
    }

    void MoveToRandomLocation()
    {
        if (buttonLocations == null || buttonLocations.Length == 0)
        {
            Debug.LogWarning("No button locations assigned!");
            return;
        }

        int index = Random.Range(0, buttonLocations.Length);

        Transform socket = buttonLocations[index];

        // Attach to socket so it follows falling block
        transform.SetParent(socket);

        // Reset local transform so it sits exactly on socket
        transform.localPosition = Vector3.zero;

        // Keep it upright (no tilt from socket rotation)
        transform.rotation = Quaternion.identity;
    }
}