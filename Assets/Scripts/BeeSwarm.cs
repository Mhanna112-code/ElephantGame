using System.Collections.Generic;
using UnityEngine;

public class BeeSwarm : MonoBehaviour
{
    [Header("Swat Detection")]
    [Tooltip("Minimum horizontal trunk movement per frame (relative to the player) to count as an intentional swing.")]
    public float swatDeadzone = 0.08f;
    [Tooltip("How long a direction reversal stays 'counted' before the combo resets.")]
    public float swatWindow = 1.5f;
    [Tooltip("Number of left-right reversals needed to swat the whole cluster.")]
    public int swatsRequired = 3;

    private Transform player;
    private Transform trunkTip;
    private readonly List<Bee> bees = new List<Bee>();

    private float lastRelativeX;
    private float lastDir;
    private int reversalCount;
    private float reversalTimer;
    private bool initialized;

    public void Init(GameObject beePrefab, int beeCount, Vector3 spawnPosition, Transform playerTransform, Transform trunkTipTransform)
    {
        player = playerTransform;
        trunkTip = trunkTipTransform;

        for (int i = 0; i < beeCount; i++)
        {
            GameObject beeObject = Instantiate(beePrefab, spawnPosition, Quaternion.identity);
            Bee bee = beeObject.GetComponent<Bee>();

            if (bee == null)
                bee = beeObject.AddComponent<Bee>();

            bee.Init(player);
            bees.Add(bee);
        }

        if (trunkTip != null && player != null)
            lastRelativeX = trunkTip.position.x - player.position.x;

        initialized = true;
    }

    void Update()
    {
        if (!initialized || player == null || trunkTip == null)
            return;

        bees.RemoveAll(bee => bee == null);

        if (bees.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        if (reversalTimer > 0f)
        {
            reversalTimer -= Time.deltaTime;

            if (reversalTimer <= 0f)
                reversalCount = 0;
        }

        float relativeX = trunkTip.position.x - player.position.x;
        float delta = relativeX - lastRelativeX;
        lastRelativeX = relativeX;

        if (Mathf.Abs(delta) < swatDeadzone)
            return;

        float dir = Mathf.Sign(delta);

        if (lastDir != 0f && dir != lastDir)
        {
            reversalCount++;
            reversalTimer = swatWindow;

            if (reversalCount >= swatsRequired)
            {
                SwatAll();
                reversalCount = 0;
            }
        }

        lastDir = dir;
    }

    void SwatAll()
    {
        foreach (Bee bee in bees)
        {
            if (bee != null)
                bee.Swat();
        }

        bees.Clear();
    }
}
