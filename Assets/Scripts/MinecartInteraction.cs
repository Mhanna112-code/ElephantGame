using UnityEngine;

public class MinecartInteraction : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;

    [Header("Interaction")]
    public float interactDistance = 2f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Seat")]
    public Vector3 seatLocalPosition = new Vector3(-0.280000001f, -0.219999999f, 0.0500000007f);

    [Header("Tilt")]
    public float tiltAngle = 25f;
    public float tiltSpeed = 5f;

    private bool isRiding;
    private Transform playerTransform;
    private Rigidbody playerRb;
    private MovingBoxSpline railway;
    private Quaternion baseRotation;
    private float currentTilt;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.GetComponent<PlayerController>();
        }

        if (player != null)
        {
            playerTransform = player.transform;
            playerRb = player.GetComponent<Rigidbody>();
        }

        railway = GetComponent<MovingBoxSpline>();
        baseRotation = transform.rotation;

        // Cart sits still on its track until someone boards it.
        if (railway != null)
            railway.enabled = false;
    }

    void Update()
    {
        if (player == null || playerTransform == null)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            if (isRiding)
            {
                Dismount();
            }
            else if (Vector3.Distance(playerTransform.position, transform.position) <= interactDistance)
            {
                Mount();
            }
        }

        if (isRiding)
        {
            ApplyTilt();
        }
    }

    void ApplyTilt()
    {
        float input = Input.GetAxisRaw("Horizontal");
        float targetTilt = input * tiltAngle;

        currentTilt = Mathf.MoveTowards(currentTilt, targetTilt, tiltSpeed * 60f * Time.deltaTime);

        transform.rotation = baseRotation * Quaternion.Euler(0f, 0f, currentTilt);
    }

    void Mount()
    {
        isRiding = true;

        if (railway != null)
            railway.enabled = true;

        player.SetRidingMinecart(true);

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.useGravity = false;
            playerRb.isKinematic = true;
        }

        playerTransform.SetParent(transform);
        playerTransform.localPosition = seatLocalPosition;
        playerTransform.localRotation = Quaternion.identity;
    }

    void Dismount()
    {
        isRiding = false;

        if (railway != null)
            railway.enabled = false;

        currentTilt = 0f;
        transform.rotation = baseRotation;

        playerTransform.SetParent(null);

        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.useGravity = true;
        }

        player.SetRidingMinecart(false);
    }
}
