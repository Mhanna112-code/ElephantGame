using UnityEngine;

public class RingPull : MonoBehaviour
{
    [Header("References")]
    public Transform ring;
    public Transform ringPullPoint;
    public Transform trunkConstraint;

    public Transform player;
    public Rigidbody playerRb;

    [Header("Pull Settings")]
    public float maxPullDistance = 2f;
    public float pullSpeed = 2f;
    public float returnSpeed = 3f;

    [Header("Spikes")]
    public SkinnedMeshRenderer spike1;
    public SkinnedMeshRenderer spike2;
    public int spikeBlendShapeIndex = 0;

    private bool attached = false;
    private bool trunkTouching = false;

    private float startRingY;
    private float playerStartY;


    void Start()
    {
        Debug.Log("Blend Shapes: " + spike1.sharedMesh.blendShapeCount);

        for (int i = 0; i < spike1.sharedMesh.blendShapeCount; i++)
        {
            Debug.Log(
                i + ": " + spike1.sharedMesh.GetBlendShapeName(i)
            );
        }

        spike1.SetBlendShapeWeight(0, 100f);
        if (ring == null)
            ring = transform;

        if (playerRb == null && player != null)
            playerRb = player.GetComponent<Rigidbody>();

        startRingY = ring.position.y;
    }


    void Update()
    {
        if (trunkTouching && Input.GetKeyDown(KeyCode.E))
        {
            if (!attached)
                Attach();
            else
                Release();
        }


        if (attached)
        {
            PullRing();
        }
        else
        {
            ReturnRing();
        }
    }


    void Attach()
    {
        attached = true;

        playerStartY = player.position.y;

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.useGravity = false;
        }

        if (trunkConstraint != null && ringPullPoint != null)
        {
            trunkConstraint.position = ringPullPoint.position;
        }
    }


    void Release()
    {
        attached = false;

        if (playerRb != null)
        {
            playerRb.useGravity = true;
        }
    }


    void PullRing()
    {
        // Keep trunk attached to ring
        if (trunkConstraint != null && ringPullPoint != null)
        {
            trunkConstraint.position = ringPullPoint.position;
        }


        // Pull player downward slowly
        if (playerRb != null)
        {
            Vector3 newPosition = playerRb.position;

            newPosition.y -= pullSpeed * Time.deltaTime;

            playerRb.MovePosition(newPosition);
        }


        // Calculate how far player has pulled
        float pullAmount = Mathf.Clamp(
            playerStartY - player.position.y,
            0f,
            maxPullDistance
        );


        // Move ring down
        Vector3 ringPosition = ring.position;

        ringPosition.y = startRingY - pullAmount;

        ring.position = ringPosition;


        UpdateSpikes(pullAmount);
    }


    void ReturnRing()
    {
        Vector3 ringPosition = ring.position;

        ringPosition.y = Mathf.MoveTowards(
            ringPosition.y,
            startRingY,
            returnSpeed * Time.deltaTime
        );

        ring.position = ringPosition;


        float currentPull = startRingY - ring.position.y;

        UpdateSpikes(currentPull);
    }


    void UpdateSpikes(float pullAmount)
    {
        float weight = Mathf.InverseLerp(
            0f,
            maxPullDistance,
            pullAmount
        ) * 100f;


        if (spike1 != null)
        {
            spike1.SetBlendShapeWeight(
                spikeBlendShapeIndex,
                weight
            );
        }
    }


    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("trunk grabbed");
        if (collision.collider.CompareTag("Trunk"))
        {
            trunkTouching = true;
        }
    }


    void OnCollisionExit(Collision collision)
    {
        Debug.Log("trunk grabbed");
        if (collision.collider.CompareTag("Trunk"))
        {
            trunkTouching = false;

            if (attached)
                Release();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("trunk grabbed");
        if (other.CompareTag("Trunk"))
        {
            trunkTouching = true;
        }
    }


    void OnTriggerExit(Collider other)
    {
        Debug.Log("trunk grabbed");
        if (other.CompareTag("Trunk"))
        {
            trunkTouching = false;

            if (attached)
                Release();
        }
    }

}