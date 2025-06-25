using UnityEngine;

public class Node
{
    public int ID { get; set; }                 
    public bool IsBoundary { get; set; } = false;   // Flag for Dirichlet/Neumann BCs //
    public Vector3 Position { get; set; }    

    public Node(Vector3 position, int id = -1)
    {
        Position = position;
        ID = id;
    }
}
