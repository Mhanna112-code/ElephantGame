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

    private bool spikesDisabled;

    [Header("Spike Damage")]
    [Tooltip("Boss within this range of a spike when the ring is fully pulled takes damage and registers a spike hit (phase 3 counter).")]
    public float spikeDamageRadius = 3f;
    public int spikeDamage = 25;
    [Tooltip("Minimum time between spike strikes from this ring.")]
    public float spikeRefireDelay = 1.5f;
    private float lastStrikeTime = -999f;
    private float previousPullAmount;

    public void DisableSpikes()
    {
        spikesDisabled = true;
    }
    void Start()
    {
        if (spike1 != null)
        {
            Debug.Log("Blend Shapes: " + spike1.sharedMesh.blendShapeCount);

            for (int i = 0; i < spike1.sharedMesh.blendShapeCount; i++)
            {
                Debug.Log(
                    i + ": " + spike1.sharedMesh.GetBlendShapeName(i)
                );
            }

            spike1.SetBlendShapeWeight(0, 100f);
        }

        if (ring == null)
            ring = transform;

        if (playerRb == null && player != null)
            playerRb = player.GetComponent<Rigidbody>();

        startRingY = ring.position.y;
    }

    void Update()
    {
        if (spikesDisabled)
            return;
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

        // Full pull = spike strike. Damages the boss if he is standing at a
        // spike (the whole point of luring him into the honey — he wasn't
        // taking any damage before because nothing ever called into
        // BossHealth/RegisterSpikeHit).
        bool fullyPulled = pullAmount >= maxPullDistance * 0.9f;
        bool wasFullyPulled = previousPullAmount >= maxPullDistance * 0.9f;
        previousPullAmount = pullAmount;

        if (fullyPulled && !wasFullyPulled && Time.time - lastStrikeTime >= spikeRefireDelay && !spikesDisabled)
        {
            lastStrikeTime = Time.time;
            StrikeBoss();
        }
    }

    void StrikeBoss()
    {
        BossHealth bossHealth = FindFirstObjectByType<BossHealth>();
        if (bossHealth == null)
            return;

        Transform boss = bossHealth.transform;
        bool struck = false;

        foreach (SkinnedMeshRenderer spike in new[] { spike1, spike2 })
        {
            if (spike == null)
                continue;

            float d = Vector3.Distance(boss.position, spike.bounds.center);
            if (d <= spikeDamageRadius)
            {
                Debug.Log($"[Spike] STRIKE: boss at {d:F1} of '{spike.name}' (radius {spikeDamageRadius}) -> {spikeDamage} dmg", this);
                bossHealth.TakeDamage(spikeDamage);

                BossFightController controller = boss.GetComponent<BossFightController>();
                if (controller != null)
                    controller.RegisterSpikeHit();

                struck = true;
                break;
            }
        }

        if (!struck)
        {
            string d1 = spike1 != null ? Vector3.Distance(boss.position, spike1.bounds.center).ToString("F1") : "n/a";
            string d2 = spike2 != null ? Vector3.Distance(boss.position, spike2.bounds.center).ToString("F1") : "n/a";
            Debug.Log($"[Spike] full pull but boss out of range (radius {spikeDamageRadius}): d(spike1)={d1} d(spike2)={d2} bossPos={boss.position}", this);
        }
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