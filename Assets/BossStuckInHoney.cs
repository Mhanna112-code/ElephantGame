using UnityEngine;

public class BossStuckInHoney : MonoBehaviour
{
    [Header("Movement")]
    public float normalSpeed = 5f;
    public float honeySpeed = 0.5f;

    [Header("References")]
    public Rigidbody bossRb;

    // Tracked as a set of colliders (not a bare counter): puddles now melt away
    // (Destroy) while the boss can be standing in them, and a counter that
    // missed the exit event would slow him forever. Destroyed colliders become
    // null in the set and are pruned on every query.
    private readonly System.Collections.Generic.HashSet<Collider> honeyContacts =
        new System.Collections.Generic.HashSet<Collider>();

    private bool InHoney
    {
        get
        {
            honeyContacts.RemoveWhere(c => c == null);
            return honeyContacts.Count > 0;
        }
    }

    public bool IsInHoney => InHoney;


    // Honey is detected by its HoneyDrop component instead of a "Honey" tag —
    // that tag was never defined in the TagManager, so the old check could not
    // match anything (part of issue #41).
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<HoneyDrop>() != null && honeyContacts.Add(other))
        {
            Debug.Log($"[BossHoney] boss ENTERED honey '{other.name}' (contacts={honeyContacts.Count}) -> speed {honeySpeed} (normal {normalSpeed}), jumping disabled", this);
        }
    }


    void OnTriggerExit(Collider other)
    {
        if (honeyContacts.Remove(other))
        {
            honeyContacts.RemoveWhere(c => c == null);
            // 'other' may be a just-destroyed (melted) glob - don't touch .name then
            string label = other != null ? other.name : "(melted)";
            Debug.Log($"[BossHoney] boss LEFT honey '{label}' (contacts={honeyContacts.Count})", this);
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
