using UnityEngine;

public class FallingBlock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Movement")]
    [SerializeField] private float maxFallSpeed = 100f;
    [SerializeField] private float minFallSpeed = 0.5f;
    [SerializeField] private float recoverySpeed = 0.5f;
    [SerializeField] private float followSpeed;

    private bool activated;
    private float currentFallSpeed;

    private void Start()
    {
        followSpeed = Travel.Instance.moveSpeed;
        currentFallSpeed = maxFallSpeed;
    }

        private void Update()
    {
        if (!activated && player.position.z > transform.position.z)
            activated = true;

        if (!activated)
            return;

        currentFallSpeed = Mathf.MoveTowards(
            currentFallSpeed,
            maxFallSpeed,
            recoverySpeed * Time.deltaTime);

        Vector3 pos = transform.position;

        // LOCK to player horizontally (FOLLOW EXACTLY)
        pos.x = player.position.x;
        pos.z = player.position.z;

        // ONLY fall down
        pos.y -= currentFallSpeed * Time.deltaTime;

        transform.position = pos;
    }

    public void ApplySlow(float amount)
    {
        currentFallSpeed = Mathf.Max(
            minFallSpeed,
            currentFallSpeed - amount);

        Debug.Log("Block slowed. Current speed: " + currentFallSpeed);
    }
}