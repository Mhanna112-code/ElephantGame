using UnityEngine;
using System.Collections;

public class BeehiveTarget : MonoBehaviour
{
    [Header("Honey")]
    public GameObject honeyPrefab;
    public Transform honeySpawnPoint;
    public int honeyAmount = 3;

    [Header("Hits")]
    public int hitsRequired = 3;

    [Header("Shake")]
    public float shakeDuration = 0.5f;
    public float shakeAmount = 0.15f;

    [Header("Bee Swarm")]
    public GameObject beePrefab;
    public int beeCount = 6;
    public Transform player;
    public Transform trunkTip;

    private int currentHits = 0;
    private bool activated = false;

    private Vector3 originalPosition;


    void Start()
    {
        originalPosition = transform.localPosition;
    }


    public void Hit()
    {
        if (activated)
            return;

        currentHits++;

        Debug.Log($"[Hive] '{name}' hit {currentHits}/{hitsRequired}", this);

        StartCoroutine(Shake());


        if (currentHits >= hitsRequired)
        {
            activated = true;
            Debug.Log($"[Hive] '{name}' releasing honey (prefab={(honeyPrefab != null)}, spawnPoint={(honeySpawnPoint != null)})", this);
            StartCoroutine(ReleaseHoney());
            SpawnBeeSwarm();
        }
    }

    void SpawnBeeSwarm()
    {
        if (beePrefab == null || player == null)
            return;

        Vector3 spawnPosition = honeySpawnPoint != null ? honeySpawnPoint.position : transform.position;

        GameObject swarmObject = new GameObject("BeeSwarm");
        BeeSwarm swarm = swarmObject.AddComponent<BeeSwarm>();
        swarm.Init(beePrefab, beeCount, spawnPosition, player, trunkTip != null ? trunkTip : player);
    }

    // Bullet hits are routed here by BulletRicochet (which reliably receives the
    // collision even when it lands on a child collider). The old OnCollisionEnter
    // is kept only as a guard against double-counting removal regressions.
    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Bullet"))
            Debug.Log($"[Hive] '{name}' hive-side collision also saw the bullet (info only, not counted)", this);
        if (false && collision.collider.CompareTag("Bullet"))
        {
            Hit();

            Destroy(collision.gameObject);
        }
    }

    IEnumerator Shake()
    {
        float timer = 0f;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;

            transform.localPosition =
                originalPosition +
                Random.insideUnitSphere * shakeAmount;

            yield return null;
        }

        transform.localPosition = originalPosition;
    }


    IEnumerator ReleaseHoney()
    {
        yield return new WaitForSeconds(0.2f);


        for (int i = 0; i < honeyAmount; i++)
        {
            Vector3 spawnPos = honeySpawnPoint.position;

            GameObject honey = Instantiate(
                honeyPrefab,
                spawnPos,
                Quaternion.identity
            );

            // Otherwise the honey overlaps the hive's own collider at spawn
            // and HoneyDrop registers a false landing before it ever falls.
            Collider honeyCollider = honey.GetComponentInChildren<Collider>();

            if (honeyCollider != null)
            {
                foreach (Collider hiveCollider in GetComponentsInChildren<Collider>())
                {
                    Physics.IgnoreCollision(honeyCollider, hiveCollider, true);
                }
            }


            // small delay between drops
            yield return new WaitForSeconds(0.15f);
        }
    }
}