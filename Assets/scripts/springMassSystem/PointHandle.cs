using UnityEngine;

public class PointHandle : MonoBehaviour
{
    private MassPoint point;
    private MassPointDragger dragger;

    public void Initialize(MassPoint point, MassPointDragger dragger)
    {
        this.point = point;
        this.dragger = dragger;
    }

    void OnMouseDown()
    {
        dragger.StartDragging(point, transform);
    }
}
