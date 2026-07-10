using UnityEngine;
using System.Collections;

public class SpikeDamage : MonoBehaviour
{
    [Header("Damage")]
    public float damage = 20f;
    public float invincibilityTime = 1.5f;

    [Header("Flash Effect")]
    public float flashInterval = 0.1f;

    private bool canDamage = true;

    void OnTriggerStay(Collider other)
    {
        // Ignore trunk damage but allow collision
        if (other.CompareTag("Trunk"))
        {
            return;
        }

        Health health = other.GetComponentInParent<Health>();

        if (health != null)
        {
            DealDamage(health.gameObject);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // Ignore trunk damage but allow collision
        if (collision.collider.CompareTag("Trunk"))
        {
            return;
        }

        Health health = collision.collider.GetComponentInParent<Health>();

        if (health != null)
        {
            DealDamage(health.gameObject);
        }
    }

    void DealDamage(GameObject player)
    {
        if (!canDamage)
            return;

        Health health = player.GetComponent<Health>();

        if (health != null)
        {
            Debug.Log("Spike damaged player");
            health.TakeDamage(damage);
        }

        StartCoroutine(Invincibility(player));
    }

    IEnumerator Invincibility(GameObject player)
    {
        canDamage = false;

        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();

        float timer = 0f;

        while (timer < invincibilityTime)
        {
            foreach (Renderer r in renderers)
            {
                r.enabled = !r.enabled;
            }

            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }

        foreach (Renderer r in renderers)
        {
            r.enabled = true;
        }

        canDamage = true;
    }
}