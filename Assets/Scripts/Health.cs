using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;


    [Header("UI")]
    public Image healthFill;


    [Header("Invincibility")]
    public float invincibleTime = 1.5f;
    public float flashInterval = 0.1f;


    private bool isInvincible = false;

    private Renderer[] renderers;
    private Collider playerCollider;


    void Start()
    {
        currentHealth = maxHealth;

        UpdateHealthBar();


        renderers = GetComponentsInChildren<Renderer>();

        playerCollider = GetComponent<Collider>();
    }


    public void TakeDamage(float damage)
    {
        // Ignore damage while invincible
        if (isInvincible)
            return;


        currentHealth -= damage;

        currentHealth = Mathf.Clamp(
            currentHealth,
            0,
            maxHealth
        );


        UpdateHealthBar();


        StartCoroutine(DamageInvincibility());


        if (currentHealth <= 0)
        {
            Die();
        }
    }


    IEnumerator DamageInvincibility()
    {
        isInvincible = true;


        // Allow player to pass through boss
        GameObject boss = GameObject.FindWithTag("Boss");

        Collider bossCollider = null;

        if (boss != null)
        {
            bossCollider = boss.GetComponent<Collider>();

            if (bossCollider != null)
            {
                Physics.IgnoreCollision(
                    playerCollider,
                    bossCollider,
                    true
                );
            }
        }


        float timer = 0f;


        while (timer < invincibleTime)
        {
            timer += flashInterval;


            // Flash player
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = !renderer.enabled;
            }


            yield return new WaitForSeconds(flashInterval);
        }


        // Turn rendering back on
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }


        // Restore boss collision
        if (bossCollider != null)
        {
            Physics.IgnoreCollision(
                playerCollider,
                bossCollider,
                false
            );
        }


        isInvincible = false;
    }



    public void Heal(float amount)
    {
        currentHealth += amount;

        currentHealth = Mathf.Clamp(
            currentHealth,
            0,
            maxHealth
        );

        UpdateHealthBar();
    }



    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(
            health,
            0,
            maxHealth
        );

        UpdateHealthBar();
    }



    void UpdateHealthBar()
    {
        if (healthFill != null)
        {
            healthFill.fillAmount =
                currentHealth / maxHealth;
        }
    }



    void Die()
    {
        Debug.Log("Player Died");

        // Add death logic here
    }
}