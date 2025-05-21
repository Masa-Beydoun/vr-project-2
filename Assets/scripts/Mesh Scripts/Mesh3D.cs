using UnityEngine;
using System;
using System.Collections.Generic;

public class Mesh3D
{
    public Node[] Nodes { get; private set; }
    public Tetrahedron[] Elements { get; private set; }

    public void GenerateCubeMesh(Vector3 size, Vector3 center)
    {
        int nx = 2, ny = 2, nz = 2;
        float dx = size.x / nx;
        float dy = size.y / ny;
        float dz = size.z / nz;

        List<Node> nodes = new List<Node>();
        Dictionary<(int, int, int), int> nodeIndexMap = new Dictionary<(int, int, int), int>();

        // Generate grid nodes
        for (int i = 0; i <= nx; i++)
        {
            for (int j = 0; j <= ny; j++)
            {
                for (int k = 0; k <= nz; k++)
                {
                    Vector3 pos = new Vector3(
                        center.x - size.x / 2 + i * dx,
                        center.y - size.y / 2 + j * dy,
                        center.z - size.z / 2 + k * dz
                    );
                    nodeIndexMap[(i, j, k)] = nodes.Count;
                    nodes.Add(new Node(pos));
                }
            }
        }

        List<Tetrahedron> tets = new List<Tetrahedron>();

        // Helper to get node index
        int idx(int i, int j, int k) => nodeIndexMap[(i, j, k)];

        // For each small cube, decompose into 5 tetrahedrons
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                for (int k = 0; k < nz; k++)
                {
                    int v000 = idx(i, j, k);
                    int v100 = idx(i + 1, j, k);
                    int v010 = idx(i, j + 1, k);
                    int v110 = idx(i + 1, j + 1, k);
                    int v001 = idx(i, j, k + 1);
                    int v101 = idx(i + 1, j, k + 1);
                    int v011 = idx(i, j + 1, k + 1);
                    int v111 = idx(i + 1, j + 1, k + 1);

                    // Tetrahedralization of the cube (5 tetrahedrons)
                    tets.Add(new Tetrahedron(v000, v100, v010, v001));
                    tets.Add(new Tetrahedron(v100, v110, v010, v111));
                    tets.Add(new Tetrahedron(v010, v001, v011, v111));
                    tets.Add(new Tetrahedron(v100, v001, v101, v111));
                    tets.Add(new Tetrahedron(v001, v010, v100, v111));
                }
            }
        }

        Nodes = nodes.ToArray();
        Elements = tets.ToArray();
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
