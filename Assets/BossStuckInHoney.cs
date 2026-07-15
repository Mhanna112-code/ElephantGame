using UnityEngine;

public class BossStuckInHoney : MonoBehaviour
{
    [Header("Movement")]
    public float normalSpeed = 5f;
    public float honeySpeed = 1.5f;

    [Header("References")]
    public Rigidbody bossRb;

    // counted (not a bool) so overlapping honey pools behave when leaving one
    private int honeyContacts = 0;

    private bool InHoney => honeyContacts > 0;


    // Honey is detected by its HoneyDrop component instead of a "Honey" tag —
    // that tag was never defined in the TagManager, so the old check could not
    // match anything (part of issue #41).
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<HoneyDrop>() != null)
        {
            honeyContacts++;
        }
    }


    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<HoneyDrop>() != null)
        {
            honeyContacts = Mathf.Max(0, honeyContacts - 1);
        }
    }


    public float GetCurrentSpeed()
    {
        return InHoney ? honeySpeed : normalSpeed;
    }


    public bool CanJump()
    {
        return !InHoney;
    }
}
