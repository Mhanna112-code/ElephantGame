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
        Debug.Log("triggered");
        // Ignore trunk damage but allow collision
        if (other.CompareTag("Trunk"))
        {
            return;
        }

        HandleHit(other.gameObject);
    }

    void OnCollisionStay(Collision collision)
    {
        Debug.Log("collided");
        // Ignore trunk damage but allow collision
        if (collision.collider.CompareTag("Trunk"))
        {
            return;
        }

        HandleHit(collision.collider.gameObject);
    }

    void HandleHit(GameObject hitObject)
    {
        if (!canDamage)
            return;

        Health health = hitObject.GetComponentInParent<Health>();
        GameObject cart = null;
        Debug.Log("checking + minecart");

        if (health == null)
        {
            // The minecart's own colliders take the hit while riding, since the
            // player is parented underneath it and GetComponentInParent can't see down.
            MinecartInteraction minecart = hitObject.GetComponentInParent<MinecartInteraction>();
            Debug.Log("minecart: " + minecart);
            if (minecart != null && minecart.IsRiding && minecart.player != null)
            {
                health = minecart.player.GetComponent<Health>();
                cart = minecart.gameObject;
            }
        }

        if (health == null)
            return;

        Debug.Log("Spike damaged player");
        health.TakeDamage(damage);

        StartCoroutine(Invincibility(health.gameObject, cart));
    }

    IEnumerator Invincibility(GameObject player, GameObject cart)
    {
        canDamage = false;

        Renderer[] playerRenderers = player.GetComponentsInChildren<Renderer>();
        Renderer[] cartRenderers = cart != null ? cart.GetComponentsInChildren<Renderer>() : new Renderer[0];

        float timer = 0f;

        while (timer < invincibilityTime)
        {
            foreach (Renderer r in playerRenderers)
            {
                r.enabled = !r.enabled;
            }

            foreach (Renderer r in cartRenderers)
            {
                r.enabled = !r.enabled;
            }

            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }

        foreach (Renderer r in playerRenderers)
        {
            r.enabled = true;
        }

        foreach (Renderer r in cartRenderers)
        {
            r.enabled = true;
        }

        canDamage = true;
    }
}