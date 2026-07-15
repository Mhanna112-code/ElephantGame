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

        StartCoroutine(Shake());


        if (currentHits >= hitsRequired)
        {
            activated = true;
            StartCoroutine(ReleaseHoney());
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Bullet"))
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


            // small delay between drops
            yield return new WaitForSeconds(0.15f);
        }
    }
}