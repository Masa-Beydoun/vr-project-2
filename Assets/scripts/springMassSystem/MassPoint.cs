using UnityEngine;

public class MassPoint
{
    private static int globalIDCounter = 0;
    public int id;  // Unique ID for debugging

    public Vector3 position;
    public Vector3 velocity;
    public PhysicalObject physicalObject;
    public bool isPinned = false;
    public float mass = 1.0f;
    public float signedDistance;
    public string sourceName;


    public MassPoint(Vector3 position, PhysicalObject physicalObject,string name)
    {
        this.id = globalIDCounter++;
        this.position = position;
        this.velocity = Vector3.zero;
        this.physicalObject = physicalObject;
        this.sourceName = name;
    }


    public void ApplyForce(Vector3 force, float deltaTime)
    {
        if (isPinned) return;
        Vector3 acceleration = force / mass;
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
    