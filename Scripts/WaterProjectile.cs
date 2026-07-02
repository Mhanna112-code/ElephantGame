using UnityEngine;

public class WaterProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 30f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] public float moveSpeed = 5f;

    [SerializeField] private float waterStrength = 1f;
    private Vector3 direction;

    public void Init(Vector3 dir)
    {
        direction = Vector3.ProjectOnPlane(dir, Vector3.forward).normalized;
    }

    void Update()
    {
        Vector3 playerMotion = Travel.Instance.transform.forward * Travel.Instance.moveSpeed;

        transform.position += (direction * speed + playerMotion) * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        SlowButton button = other.GetComponent<SlowButton>();

        if (button != null)
        {
            Debug.Log("hit button");
            button.HitByWater(waterStrength);
        }

        Destroy(gameObject);
    }
}