using UnityEngine;

public class OneWayPlatform : MonoBehaviour
{
    [Header("References")]
    public Collider platformCollider;
    public PlayerController player;
    public CutsceneCamera cutsceneCam;

    [Header("Diagnostics")]
    public bool debugLogs = true;

    bool playedCutscene = false;
    bool? wasAbove = null;   // only log on transitions, not every frame

    void Update()
    {
        if (player == null)
        {
            if (debugLogs) Debug.LogWarning($"[OneWayPlatform] '{name}' player not assigned", this);
            return;
        }

        bool above = player.transform.position.y >= transform.position.y;

        if (debugLogs && wasAbove != above)
            Debug.Log($"[OneWayPlatform] '{name}' player -> {(above ? "ABOVE" : "BELOW")} " +
                      $"(playerY={player.transform.position.y:F2} platformY={transform.position.y:F2})", this);
        wasAbove = above;

        if (!above)
        {
            // Player is underneath the platform (side-on 2.5D mode)
            if (platformCollider != null) platformCollider.enabled = false;
            player.DisableZMovement();
            player.EnableShooting();
        }
        else
        {
            // Player is above the platform (top-down mode) - play the cutscene once
            if (!playedCutscene)
            {
                if (cutsceneCam != null)
                {
                    if (debugLogs) Debug.Log($"[OneWayPlatform] '{name}' -> triggering cutscene", this);
                    cutsceneCam.PlayCutscene();
                }
                else if (debugLogs)
                {
                    Debug.LogWarning($"[OneWayPlatform] '{name}' cutsceneCam not assigned -> no cutscene will play", this);
                }
                playedCutscene = true;
            }

            if (platformCollider != null) platformCollider.enabled = true;
            player.EnableZMovement();
            player.DisableShooting();
        }
    }
}
