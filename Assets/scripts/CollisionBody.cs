
using UnityEngine;

[RequireComponent(typeof(PhysicalObject))]
public class CollisionBody : MonoBehaviour
{
    public PhysicalObject physicalObject;

    private void Awake()
    {
        physicalObject = GetComponent<PhysicalObject>();
    }

    public Vector3 Position => transform.position;
    public Vector3 Velocity => physicalObject.velocity;
    public float Mass => physicalObject.mass;
    public float Radius => physicalObject.radius;
    public Vector3 Size => physicalObject.transform.localScale;
    public PhysicalObject.ShapeType Shape => physicalObject.shape;
}
