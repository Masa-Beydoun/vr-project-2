using UnityEngine;

public class MassPoint
{
    public Vector3 position;
    public Vector3 velocity;
    public PhysicalObject physicalObject;
    public bool isPinned = false;

    public float signedDistance;  // <-- Add this

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



    public override bool Equals(object obj)
    {
        if (obj is MassPoint other)
            return Vector3.Distance(this.position, other.position) < 1e-4f;
        return false;
    }

    public override int GetHashCode()
    {
        return position.GetHashCode();
    }
}
    