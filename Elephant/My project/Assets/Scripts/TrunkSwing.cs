using System.Collections.Generic;
using UnityEngine;

public class TrungSwing : MonoBehaviour
{
    [SerializeField] private List<Transform> chain;
    [SerializeField] private float segmentLength = 0.6f;
    [SerializeField] private int iterations = 6;
    [SerializeField] private float smooth = 20f;

    private Vector3 target;

    void Update()
    {
        UpdateTarget();
        SolveIK();
    }

    void UpdateTarget()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, transform.position);

        if (plane.Raycast(ray, out float enter))
        {
            target = ray.GetPoint(enter);
        }
    }

    void SolveIK()
    {
        // 1. Move tip toward target
        chain[^1].position = Vector3.Lerp(
            chain[^1].position,
            target,
            Time.deltaTime * smooth
        );

        for (int i = 0; i < iterations; i++)
        {
            // BACKWARD PASS (tip -> base)
            for (int j = chain.Count - 2; j >= 0; j--)
            {
                Vector3 dir = (chain[j].position - chain[j + 1].position).normalized;
                chain[j].position = chain[j + 1].position + dir * segmentLength;
            }

            // FORWARD PASS (base -> tip)
            chain[0].position = transform.position; // root locked to capsule

            for (int j = 1; j < chain.Count; j++)
            {
                Vector3 dir = (chain[j].position - chain[j - 1].position).normalized;
                chain[j].position = chain[j - 1].position + dir * segmentLength;
            }
        }
    }
}