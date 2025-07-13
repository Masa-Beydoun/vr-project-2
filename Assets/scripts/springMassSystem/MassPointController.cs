using UnityEngine;

public class MassPointController : MonoBehaviour
{
    public bool isPinned = false; // Debug view only
    [HideInInspector] public MassPoint point;

    public void Initialize(MassPoint point)
    {
        this.point = point;
        this.isPinned = point.isPinned; // reflect initial state only
    }

    void Update()
    {
        if (point == null) return;

        //isPinned = point.isPinned; // display current state (don't set it!)
        point.isPinned = isPinned;
        if (point.isPinned)
        {
            Debug.Log($"[Pinned] Point ID: {point.id}, Position: {transform.position}");
            point.position = transform.position;
            point.velocity = Vector3.zero;
        }
        else
        {
            transform.position = point.position;
        }
    }

}
