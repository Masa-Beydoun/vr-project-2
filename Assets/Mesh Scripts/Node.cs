using UnityEngine;

public class Node
{
    public int Id;
    public Vector3 Position;

    public Node(int id, float x, float y, float z)
    {
        Id = id;
        Position = new Vector3(x, y, z);
    }
}
