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
    bool wasOver = false;

    void Update()
    {
        if (player == null)
        {
            if (debugLogs) Debug.LogWarning($"[OneWayPlatform] '{name}' player not assigned", this);
            return;
        }

        Vector3 pp = player.transform.position;

        // BUG FIX: the old check was `player.y >= transform.position.y` - a naive Y-only compare
        // with NO horizontal test. Any platform BELOW the player (e.g. a floor further down)
        // reported the player "ABOVE" and fired its cutscene, even when the player was nowhere
        // near it. Now this platform only decides the player's mode when the player is actually
        // HORIZONTALLY OVER its footprint, and "above" is measured against the platform's TOP
        // surface (collider bounds), not its pivot.
        Bounds b = platformCollider != null
            ? platformCollider.bounds
            : new Bounds(transform.position, Vector3.one);

        bool over = pp.x >= b.min.x && pp.x <= b.max.x;

        if (!over)
        {
            // Player is not over this platform -> it must not touch the player's mode/cutscene.
            if (debugLogs && wasOver)
                Debug.Log($"[OneWayPlatform] '{name}' player left footprint (playerX={pp.x:F2} " +
                          $"footprintX=[{b.min.x:F2},{b.max.x:F2}]) -> not controlling player", this);
            wasOver = false;
            wasAbove = null;
            return;
        }
        wasOver = true;

        float topY = platformCollider != null ? b.max.y : transform.position.y;
        bool above = pp.y >= topY;

        if (debugLogs && wasAbove != above)
            Debug.Log($"[OneWayPlatform] '{name}' player -> {(above ? "ABOVE" : "BELOW")} " +
                      $"(playerY={pp.y:F2} platformTopY={topY:F2} overFootprint=true)", this);
        wasAbove = above;

        if (!above)
        {
            // Player is underneath the platform (side-on 2.5D mode).
            // On the transition down, put the side-view camera back and re-arm the cutscene so the
            // top-down camera does not stay stuck over the player (the "camera too close" bug).
            if (playedCutscene)
            {
                if (cutsceneCam != null) cutsceneCam.RestoreCamera();
                playedCutscene = false;
                if (debugLogs) Debug.Log($"[OneWayPlatform] '{name}' -> dropped below: restored side camera, re-armed cutscene", this);
            }

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
