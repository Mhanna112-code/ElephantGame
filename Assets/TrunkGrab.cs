using UnityEngine;

public class TrunkGrab : MonoBehaviour
{
    public Transform trunkTip;
    public float grabDistance = 3f;

    public LayerMask climbLayer;

    private SpringJoint joint;

    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            TryGrab();
        }

        if(Input.GetMouseButtonUp(0))
        {
            Release();
        }
    }


    void TryGrab()
    {
        RaycastHit hit;

        if(Physics.Raycast(
            trunkTip.position,
            trunkTip.forward,
            out hit,
            grabDistance,
            climbLayer))
        {
            Grab(hit.point);
        }
    }


    void Grab(Vector3 point)
    {
        joint = gameObject.AddComponent<SpringJoint>();

        joint.connectedAnchor = point;
        joint.maxDistance = 2f;
        joint.spring = 50f;
        joint.damper = 5f;
    }


    void Release()
    {
        if(joint != null)
        {
            Destroy(joint);
        }
    }
}