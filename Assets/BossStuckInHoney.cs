using UnityEngine;

public class BossStuckInHoney : MonoBehaviour
{
    [Header("Movement")]
    public float normalSpeed = 5f;
    public float honeySpeed = 1.5f;

    [Header("References")]
    public Rigidbody bossRb;

    private bool inHoney = false;


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Honey"))
        {
            inHoney = true;
        }
    }


    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Honey"))
        {
            inHoney = false;
        }
    }


    public float GetCurrentSpeed()
    {
        return inHoney ? honeySpeed : normalSpeed;
    }


    public bool CanJump()
    {
        return !inHoney;
    }
}