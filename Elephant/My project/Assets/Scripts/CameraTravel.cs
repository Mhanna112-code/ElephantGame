using UnityEngine;

public class CameraTravel : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        // Constant forward movement
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

    }
}
