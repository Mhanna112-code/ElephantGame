using UnityEngine;

public class SplineMover : MonoBehaviour
{
    public Transform[] points;
    public float speed = 2f;

    private float t;

    void Update()
    {
        if (points.Length < 4)
            return;

        t += speed * Time.deltaTime;

        // Loop path
        float curveTime = t % (points.Length - 3);

        int segment = Mathf.FloorToInt(curveTime);

        float localT = curveTime - segment;

        transform.position = CatmullRom(
            points[segment].position,
            points[segment + 1].position,
            points[segment + 2].position,
            points[segment + 3].position,
            localT
        );
    }


    Vector3 CatmullRom(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        float t)
    {
        return 0.5f *
        (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f*p0 - 5f*p1 + 4f*p2 - p3) * t*t +
            (-p0 + 3f*p1 - 3f*p2 + p3) * t*t*t
        );
    }
}