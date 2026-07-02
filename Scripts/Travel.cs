using UnityEngine;
using UnityEngine.SceneManagement;

public class Travel : MonoBehaviour
{
    [Header("Movement")]
    public static Travel Instance;
    [SerializeField] public float moveSpeed = 5f;
    [SerializeField] private Transform cameraTransform;
    private Vector3 cameraStartOffset;
    [Header("Jump")]
    [SerializeField] private float jumpForce = 7f;

    [SerializeField] private float fallLimit = -50f;

    private Vector3 startPosition;
    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        startPosition = transform.position; // save spawn point
        if (cameraTransform != null)
        {
            cameraStartOffset = cameraTransform.position - transform.position;
        }

    }

    void Update()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        transform.position += transform.forward * moveSpeed * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        CheckFall();
    }

    void CheckFall()
    {
        if (transform.position.y < fallLimit) {
            Respawn();
        }
    }

    void Respawn()
    {
       /* transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        // optional: reset velocity if using Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        ResetCamera();*/
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    }

    void ResetCamera()
{
    if (cameraTransform != null)
    {
        cameraTransform.position = startPosition + cameraStartOffset;
        cameraTransform.rotation = Quaternion.identity;
    }
}
}