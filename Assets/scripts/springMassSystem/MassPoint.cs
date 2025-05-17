using UnityEngine;
public class MassPoint
{
    public Vector3 position;
    public Vector3 velocity;
    public PhysicalObject physicalObject;  // reference to PhysicalObject
    public bool isPinned = false;

    public MassPoint(Vector3 position, PhysicalObject physicalObject)
    {
        this.position = position;
        this.velocity = Vector3.zero;
        this.physicalObject = physicalObject;
    }

    public float Mass => physicalObject != null ? physicalObject.mass : 1f;

    public void ApplyForce(Vector3 force, float deltaTime)
    {
        if (isPinned) return;
        Vector3 acceleration = force / Mass;
        velocity += acceleration * deltaTime;
    }

    public void Integrate(float deltaTime)
    {
        if (isPinned) return;
        position += velocity * deltaTime;
    }
}
