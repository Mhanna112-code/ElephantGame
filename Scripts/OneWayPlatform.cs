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

    void Update()
    {
        float playerY = player.transform.position.y;

        if (playerY > highPlatform.position.y)
        {
            // Player has finished the climb and is above the high-rise platform
            if (!playedResetCutscene)
            {
                cutsceneCam.ResetCameraView();
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
                cutsceneCam.PlayCutscene();
                playedCutscene = true;
            }

            platformCollider.enabled = true;
            player.EnableZMovement();
            player.DisableShooting();
        }
    }
}