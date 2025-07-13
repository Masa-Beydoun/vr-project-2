using System.Collections.Generic;
using UnityEngine;

public class MassPoint
{
    private static int globalIDCounter = 0;
    public int id;  // Unique ID for debugging

    public Vector3 position;
    public Vector3 velocity;
    public float mass;
    public bool isPinned;
    public Vector3 forceAccumulator = Vector3.zero; // NEW

    public PhysicalObject physicalObject;
    public float signedDistance;
    public string sourceName;
    public List<Spring> connectedSprings = new List<Spring>();

    public MassPoint(Vector3 position, PhysicalObject physicalObject, string name, bool isPinned = false)
    {
        this.id = globalIDCounter++;
        this.position = position;
        this.velocity = Vector3.zero;
        this.physicalObject = physicalObject;
        this.sourceName = name;
    }

    public void ApplyForce(Vector3 force)
    {
        if (isPinned) return;
        float maxForce = 150f;
        if (force.magnitude > maxForce)
        {
            Debug.LogWarning($"Excessive force detected: {force.magnitude}");
            force = force.normalized * maxForce;
        }

        forceAccumulator += force; // accumulate forces, not acceleration

        if (forceAccumulator.magnitude > maxForce * 2f)
        {
            Debug.LogWarning($"Accumulated force clamped from {forceAccumulator.magnitude:F2}");
            forceAccumulator = forceAccumulator.normalized * (maxForce * 2f);
        }
    }


    public void Integrate(float deltaTime)
    {
        if (isPinned)
        {
            velocity = Vector3.zero;
            forceAccumulator = Vector3.zero;
            return;
        }
        if (mass <= 0) return;

        // Calculate acceleration from accumulated forces
        Vector3 acceleration = forceAccumulator / mass;

        // Limit acceleration to prevent explosions
        float maxAcceleration = 300f; // Adjust as needed
        if (acceleration.magnitude > maxAcceleration)
        {
            Debug.LogWarning($"Acceleration clamped from {acceleration.magnitude:F2}");
            acceleration = acceleration.normalized * maxAcceleration;
        }

        // Update velocity and position
        velocity += acceleration * deltaTime;

        // Limit velocity
        float maxVelocity = 150f; // Adjust as needed
        if (velocity.magnitude > maxVelocity)
        {
            velocity = velocity.normalized * maxVelocity;
        }

        position += velocity * deltaTime;

        // Clear the force accumulator
        forceAccumulator = Vector3.zero;
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
