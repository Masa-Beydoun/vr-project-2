using UnityEngine;

public class MassPointDragger : MonoBehaviour
{
    public Camera mainCamera;
    private MassPoint selectedPoint;
    private Transform selectedPointTransform;
    private Vector3 offset;
    private Plane dragPlane;

    public void RegisterPoint(MassPoint point, GameObject pointVisual)
    {
        SphereCollider col = pointVisual.AddComponent<SphereCollider>();
        col.radius = 0.05f;
        col.isTrigger = true;

        PointHandle handle = pointVisual.AddComponent<PointHandle>();
        handle.Initialize(point, this);
    }

    public void StartDragging(MassPoint point, Transform pointTransform)
    {
        selectedPoint = point;
        selectedPointTransform = pointTransform;
        dragPlane = new Plane(Vector3.up, point.position); // You can adjust normal based on camera
    }

    public void StopDragging()
    {
        selectedPoint = null;
        selectedPointTransform = null;
    }

    void Update()
    {
        if (selectedPoint != null && Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                selectedPoint.position = hitPoint;
                selectedPoint.velocity = Vector3.zero; // Cancel velocity while dragging
                if (selectedPointTransform != null)
                    selectedPointTransform.position = hitPoint;
            }
        }

        if (Input.GetMouseButtonUp(0))
            StopDragging();
    }
}
