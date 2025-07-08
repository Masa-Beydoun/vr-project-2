using UnityEngine;

public class MassPointController : MonoBehaviour
{
    public bool isPinned = false; // Visible in Inspector at runtime
    [HideInInspector]public MassPoint point;

    public void Initialize(MassPoint point)
    {
        this.point = point;
        point.isPinned = isPinned; // Sync initial value
    }

    void Update()
    {
        if (point == null) return;

        point.isPinned = isPinned;

        if (isPinned)
        {
            // Sync the transform (visual position) to the simulation's data
            point.position = transform.position;

            // Optional: freeze movement completely
            point.velocity = Vector3.zero;
        }
        else
        {
            // Let the simulation update the transform
            transform.position = point.position;
        }
    }


}
