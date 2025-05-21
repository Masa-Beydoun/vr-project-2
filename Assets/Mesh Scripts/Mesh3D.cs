using UnityEngine;
using System;

public class Mesh3D
{
    public Node[] Nodes { get; private set; }
    public Tetrahedron[] Elements { get; private set; }

    public void GenerateCubeMesh(Vector3 size, Vector3 center)
    {
        // Generate 8 nodes for a cube
        float hx = size.x / 2, hy = size.y / 2, hz = size.z / 2;
        Nodes = new Node[]
        {
            new Node(center + new Vector3(-hx, -hy, -hz)),
            new Node(center + new Vector3(hx, -hy, -hz)),
            new Node(center + new Vector3(hx, hy, -hz)),
            new Node(center + new Vector3(-hx, hy, -hz)),
            new Node(center + new Vector3(-hx, -hy, hz)),
            new Node(center + new Vector3(hx, -hy, hz)),
            new Node(center + new Vector3(hx, hy, hz)),
            new Node(center + new Vector3(-hx, hy, hz))
        };

        // Decompose cube into 5 tetrahedrons (optimal for FEM)
        Elements = new Tetrahedron[]
        {
            new Tetrahedron(0, 1, 3, 4), // T0
            new Tetrahedron(1, 2, 3, 6), // T1
            new Tetrahedron(1, 3, 4, 6), // T2
            new Tetrahedron(3, 4, 6, 7), // T3
            new Tetrahedron(1, 4, 5, 6)  // T4
        };
    }

    public Vector3[] GetTetCorners(Tetrahedron tet)
    {
        return new Vector3[]
        {
            Nodes[tet.NodeIndices[0]].Position,
            Nodes[tet.NodeIndices[1]].Position,
            Nodes[tet.NodeIndices[2]].Position,
            Nodes[tet.NodeIndices[3]].Position
        };
    }
}
