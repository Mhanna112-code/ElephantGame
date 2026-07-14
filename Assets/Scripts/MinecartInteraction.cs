using UnityEngine;
using UnityEngine.Splines;

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

    [Header("Wheels")]
    public Transform[] wheels;
    public float wheelRadius = 0.3f;
    public Vector3 wheelRotationAxis = Vector3.right;

    public bool IsRiding => isRiding;

    private bool isRiding;
    private Transform playerTransform;
    private Rigidbody playerRb;
    private SplineAnimate splineAnimate;
    private float currentTilt;

    private Vector3 lastSplinePosition;
    private Quaternion lastSplineRotation;
    private bool hasLastSpline;

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

        splineAnimate = GetComponent<SplineAnimate>();

        // Cart sits still on its track until someone boards it.
        if (splineAnimate != null)
            splineAnimate.Pause();
    }

    void OnEnable()
    {
        if (splineAnimate == null)
            splineAnimate = GetComponent<SplineAnimate>();

        if (splineAnimate != null)
            splineAnimate.Updated += OnSplineUpdated;
    }

    void OnDisable()
    {
        if (splineAnimate != null)
            splineAnimate.Updated -= OnSplineUpdated;
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
            UpdateTilt();
        }
    }

    void UpdateTilt()
    {
        float input = Input.GetAxisRaw("Horizontal");
        float targetTilt = input * tiltAngle;

        currentTilt = Mathf.MoveTowards(currentTilt, targetTilt, tiltSpeed * 60f * Time.deltaTime);
    }

    // Called by SplineAnimate right after it sets this frame's base position/rotation
    // from the spline. We layer the steering lean on top instead of fighting it.
    void OnSplineUpdated(Vector3 splinePosition, Quaternion splineRotation)
    {
        transform.rotation = splineRotation * Quaternion.Euler(0f, 0f, currentTilt);

        if (hasLastSpline)
        {
            float distanceTraveled = (splinePosition - lastSplinePosition).magnitude;
            RotateWheels(distanceTraveled);
        }

        lastSplinePosition = splinePosition;
        lastSplineRotation = splineRotation;
        hasLastSpline = true;
    }

    void RotateWheels(float distanceTraveled)
    {
        if (wheels == null || wheels.Length == 0 || wheelRadius <= 0f)
            return;

        float angle = (distanceTraveled / wheelRadius) * Mathf.Rad2Deg;

        foreach (Transform wheel in wheels)
        {
            if (wheel != null)
                wheel.Rotate(wheelRotationAxis, angle, Space.Self);
        }
    }

    void Mount()
    {
        isRiding = true;

        if (splineAnimate != null)
            splineAnimate.Play();

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

        if (splineAnimate != null)
            splineAnimate.Pause();

        currentTilt = 0f;

        if (hasLastSpline)
            transform.rotation = lastSplineRotation;

        playerTransform.SetParent(null);

        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.useGravity = true;
        }

        player.SetRidingMinecart(false);
    }
}
