using System.Collections;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Damage Flash")]
    public Color flashColor = Color.red;
    public float flashDuration = 0.15f;

    [Header("Phase 3")]
    public bool canTakeBulletDamage = false;

    private BossFightController bossController;
    private Animator animator;

    private Renderer[] renderers;
    private Color[][] originalColors;

    private Coroutine flashRoutine;

    private static readonly int DieHash = Animator.StringToHash("Die");


    void Awake()
    {
        currentHealth = maxHealth;

        bossController = GetComponent<BossFightController>();
        animator = GetComponent<Animator>();

        renderers = GetComponentsInChildren<Renderer>();

        // Store every material color on every renderer
        originalColors = new Color[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;

            originalColors[i] = new Color[materials.Length];

            for (int j = 0; j < materials.Length; j++)
            {
                originalColors[i][j] = materials[j].color;
            }
        }
    }


    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0)
            return;


        currentHealth -= amount;


        // Restart flash if hit multiple times quickly
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(DamageFlash());


        if (currentHealth <= 0)
        {
            currentHealth = 0;

            if (bossController != null)
            {
                bossController.Die();
            }
            else if (animator != null)
            {
                animator.SetTrigger(DieHash);
            }
        }
    }


    public void TakeBulletDamage(int amount)
    {
        if (!canTakeBulletDamage)
            return;

        TakeDamage(amount);
    }


    public void EnterPhase3(int newHealth)
    {
        canTakeBulletDamage = true;
        maxHealth = newHealth;
        currentHealth = newHealth;
    }


    IEnumerator DamageFlash()
    {
        // Turn red
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;

            for (int j = 0; j < materials.Length; j++)
            {
                materials[j].color = flashColor;
            }
        }


        yield return new WaitForSeconds(flashDuration);


        // Restore original colors
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] materials = renderers[i].materials;

            for (int j = 0; j < materials.Length; j++)
            {
                materials[j].color = originalColors[i][j];
            }
        }


        flashRoutine = null;
    }
}