using UnityEngine;

public class BossHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    private BossFightController bossController;
    private Animator animator;

    private static readonly int DieHash = Animator.StringToHash("Die");

    private void Awake()
    {
        currentHealth = maxHealth;
        bossController = GetComponent<BossFightController>();
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0)
            return;

        currentHealth -= amount;

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
}