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
        if (player.transform.position.y < transform.position.y || player.transform.position.y > highPlatform.position.y)
        {
            if (player.transform.position.y > highPlatform.position.y)
            {
                if (!playedResetCutscene) {
                    cutsceneCam.ResetCameraView();
                    playedResetCutscene = true;
            }

            }
            // Player is underneath the platform
            platformCollider.enabled = false;

            player.DisableZMovement();
            player.EnableShooting();
        }
        else if (player.transform.position.y < highPlatform.position.y)
        {
            if (!playedCutscene) {
                cutsceneCam.PlayCutscene();
                playedCutscene = true;
            }
            Debug.Log("above platform");
            // Player is above the platform
            platformCollider.enabled = true;
            player.EnableZMovement();
            player.DisableShooting();

        }
    }
}