using UnityEngine;

public class BossSpikes : MonoBehaviour
{
    public int damage = 20;

    [Header("Disable Spike Movement After Hit")]
    public RingPull ringPull;

    private bool hitBoss;

    private void OnTriggerEnter(Collider other)
    {
        if (hitBoss)
            return;

        BossHealth boss = other.GetComponent<BossHealth>();

        if (boss == null)
            return;

        hitBoss = true;

        // Damage + flash happens here
        boss.TakeDamage(damage);

        // Stop the ring from moving spikes anymore
        if (ringPull != null)
        {
            ringPull.DisableSpikes();
        }

        BossFightController controller = other.GetComponent<BossFightController>();

        if (controller != null)
        {
            controller.RegisterSpikeHit();
        }
    }
}