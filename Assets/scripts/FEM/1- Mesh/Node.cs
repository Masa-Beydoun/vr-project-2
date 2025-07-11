using UnityEngine;

public class Node
{
    public int ID { get; set; }                 
    public bool IsBoundary { get; set; } = false;   // Flag for Dirichlet/Neumann BCs //
    public Vector3 Position;
    public Vector3 Velocity;
    public float Mass;
    public bool isPinned;


    public Node(Vector3 pos,int id = -1, float nodeMass = 1.0f, bool pinned = false)
    {
        Position = pos;
        ID = id;
        Velocity = Vector3.zero;
        Mass = nodeMass;
        isPinned = pinned;
    }

    public void ApplyForce(Vector3 force, float deltaTime)
    {
        if (isPinned || Mass <= 0f)
            return;

        // Newton's Second Law: F = m * a  a = F / m
        Vector3 acceleration = force / Mass;

        // Integrate velocity: v = v + a * dt
        Velocity += acceleration * deltaTime;
    }

}

