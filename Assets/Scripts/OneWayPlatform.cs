using System.Linq;
using UnityEngine;

public class OneWayPlatform : MonoBehaviour
{
    [Header("References")]
    public Collider platformCollider;
    public PlayerController player;
    public CutsceneCamera cutsceneCam;

    public Transform highPlatform;
    bool playedCutscene = false;
    bool playedResetCutscene = false;
    void Start()
    {
        // The player object is named "Elephant_Girl (1)" in the scene, so the old
        // GameObject.Find("Player") lookup returned null and this script threw every frame.
        if (player == null)
        {
            GameObject byName = GameObject.Find("Player");
            if (byName != null) player = byName.GetComponent<PlayerController>();
        }
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                Debug.Log($"[OneWayPlatform] '{name}': no GameObject named 'Player'; auto-found PlayerController on '{player.gameObject.name}'.", this);
        }
        if (cutsceneCam == null)
        {
            cutsceneCam = FindFirstObjectByType<CutsceneCamera>();
            if (cutsceneCam != null)
                Debug.Log($"[OneWayPlatform] '{name}': cutsceneCam not assigned in inspector; auto-found on '{cutsceneCam.gameObject.name}'.", this);
        }

        Debug.Log($"[OneWayPlatform] Start '{name}': player={(player != null ? player.gameObject.name : "NULL")} cutsceneCam={(cutsceneCam != null ? cutsceneCam.gameObject.name : "NULL")} platformCollider={(platformCollider != null ? "OK" : "NULL")} highPlatform={(highPlatform != null ? "OK" : "NULL")}", this);

        if (player == null || platformCollider == null || highPlatform == null)
        {
            Debug.LogError($"[OneWayPlatform] '{name}': missing required reference(s) above; disabling so it does not throw every frame. Wire them in the inspector.", this);
            enabled = false;
        }
    }

    void Update()
    {
        float playerY = player.transform.position.y;

        if (playerY > highPlatform.position.y)
        {
            // Player has finished the climb and is above the high-rise platform
            if (!playedResetCutscene)
            {
                if (cutsceneCam != null) cutsceneCam.ResetCameraView();
                else Debug.LogWarning($"[OneWayPlatform] '{name}': cutsceneCam is NULL; skipping ResetCameraView.", this);
                playedResetCutscene = true;
            }

            platformCollider.enabled = true; // keep the floor solid so bullets can still ricochet
            player.DisableZMovement();       // camera is back to the side view here, so trunk aim
                                              // must use the forward plane, not the top-down one
            player.EnableShooting();
        }
        else if (playerY < transform.position.y)
        {
            // Player is underneath the platform
            platformCollider.enabled = false;

            player.DisableZMovement();
            player.EnableShooting();
        }
        else
        {
            // Player is inside the climbing shaft, between the platform base and the high platform
            if (!playedCutscene)
            {
                if (cutsceneCam != null) cutsceneCam.PlayCutscene();
                else Debug.LogWarning($"[OneWayPlatform] '{name}': cutsceneCam is NULL; skipping PlayCutscene.", this);
                playedCutscene = true;
            }

            platformCollider.enabled = true;
            player.EnableZMovement();
            player.DisableShooting();
        }
    }
}