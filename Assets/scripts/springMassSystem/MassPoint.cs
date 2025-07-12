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

    public Vector3 acceleration; // Added acceleration storage
    public PhysicalObject physicalObject;
    public float signedDistance;
    public string sourceName;
    public List<Spring> connectedSprings = new List<Spring>();

    public MassPoint(Vector3 position, PhysicalObject physicalObject, string name, bool isPinned = false)
    {
        this.id = globalIDCounter++;
        this.position = position;
        this.velocity = Vector3.zero;
        this.acceleration = Vector3.zero; // Initialize acceleration
        this.physicalObject = physicalObject;
        this.sourceName = name;
    }

    public void ApplyForce(Vector3 force, float deltaTime)
    {
        if (isPinned) return;
        if (mass <= 0f)
        {
            Debug.LogError($"MassPoint {id} has zero or negative mass! Force = {force}");
            return;
        }
        if (force.magnitude > 1000f)
        {
            Debug.LogWarning($"Excessive force detected: {force.magnitude}");
            force = force.normalized * 1000f;
        }


        // F = m * a => a = F / m
        Vector3 newAcceleration = force / mass;
        acceleration += newAcceleration; // Accumulate acceleration (forces can come multiple times per step)
    }

    public void Integrate(float deltaTime)
    {
        if (isPinned)
        {
            // Clamp velocity to prevent explosion
            if (velocity.magnitude > 50f)
            {
                velocity = velocity.normalized * 50f;
            }

            // Keep pinned points fixed: zero velocity and acceleration
            velocity = Vector3.zero;
            acceleration = Vector3.zero;
            return;
        }

        // Semi-implicit Euler integration:
        velocity += acceleration * deltaTime;
        position += velocity * deltaTime;

        // Clear acceleration after integration for next frame
        acceleration = Vector3.zero;
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
