using UnityEngine;

public class MovingCol : MonoBehaviour
{
    [SerializeField] private float moveDistance = 3f;
    [SerializeField] private float speed = 2f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        float offset = Mathf.Sin(Time.time * speed) * moveDistance;

        transform.position = startPos + Vector3.up * offset;
    }
}