using UnityEngine;

public class BossSpikes : MonoBehaviour
{
    public int damage = 20;

    void OnTriggerEnter(Collider other)
    {
        BossHealth boss = other.GetComponent<BossHealth>();

        if (boss != null)
        {
            boss.TakeDamage(damage);
        }
    }
}