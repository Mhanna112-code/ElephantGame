using UnityEngine;

public class OneWayPlatform : MonoBehaviour
{
    [Header("References")]
    public Collider platformCollider;
    public PlayerController player;
    public CutsceneCamera cutsceneCam;

    bool playedCutscene = false;

    void Update()
    {
        if (player.transform.position.y < transform.position.y)
        {
            // Player is underneath the platform
            platformCollider.enabled = false;

            player.DisableZMovement();
            player.EnableShooting();
        }
        else
        {
            if (!playedCutscene) {
                cutsceneCam.PlayCutscene();
                playedCutscene = true;
            }
            // Player is above the platform
            platformCollider.enabled = true;
            player.EnableZMovement();
            player.DisableShooting();

        }
    }
}