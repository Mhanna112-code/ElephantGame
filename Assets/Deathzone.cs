using UnityEngine;

public class Deathzone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("entered deathZone");
            Health health = other.GetComponent<Health>();

            if (health != null)
            {
                health.Die();
            }
        }
    }
}