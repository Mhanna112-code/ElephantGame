using UnityEngine;

public class HoneyDrop : MonoBehaviour
{
    public float fallSpeed = 5f;

    private bool landed = false;


    void Update()
    {
        if (!landed)
        {
            transform.position +=
                Vector3.down *
                fallSpeed *
                Time.deltaTime;
        }
    }


    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Floor"))
        {
            landed = true;

            Rigidbody rb = GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
            }
        }
    }
}