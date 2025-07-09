using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;


public class Mesh3D
{
    public Node[] Nodes { get; private set; }
    public Tetrahedron[] Elements { get; private set; }


    // cube generate 
    public void GenerateCubeMesh(Vector3 size, Vector3 center)
    {
        float desiredElementSize = 0.5f;


        // Calculates the number of divisions in each direction based on the volume of the cube //
        int nx = Mathf.Max(1, Mathf.CeilToInt(size.x / desiredElementSize));
        int ny = Mathf.Max(1, Mathf.CeilToInt(size.y / desiredElementSize));
        int nz = Mathf.Max(1, Mathf.CeilToInt(size.z / desiredElementSize));


        // Calculates the distance between points in each direction //
        float dx = size.x / nx;
        float dy = size.y / ny;
        float dz = size.z / nz;

        List<Node> nodes = new();

        // store the 3D index of each node for quick lookup //
        Dictionary<(int, int, int), int> nodeIndexMap = new();

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
                    nodes.Add(new Node(pos, nodes.Count));
                }
            }
        }

        List<Tetrahedron> tets = new();
        int idx(int i, int j, int k) => nodeIndexMap[(i, j, k)];


        // Loops through each small cube cell in the grid and splits it into 5 tetrahedrons using
        // the surrounding 8 nodes. These five tetrahedrons together fill the volume of the cube //

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
    

    // sphere generate 
    public void GenerateSphereMesh(float radius, float resolution, Vector3 center)
    {
        float spacing = resolution; // how far apart the grid points are //
        List<Node> nodes = new();
        Dictionary<(int, int, int), int> indexMap = new();


        // Computes how many grid cells fit across the sphere’s diameter
        int divisions = Mathf.CeilToInt(radius * 2 / spacing);

        // Step 1: Add nodes inside the sphere


        // We create a cube grid that surrounds the sphere, centered at the origin, 
        // with coordinates from -divisions to +divisions.
        for (int x = -divisions; x <= divisions; x++)
        {
            for (int y = -divisions; y <= divisions; y++)
            {
                for (int z = -divisions; z <= divisions; z++)
                {
                    Vector3 pos = new Vector3(x, y, z) * spacing + center;
                    // Keep only points that lie inside the sphere (distance from center ≤ radius) 
                    if ((pos - center).magnitude <= radius)
                    {
                        int id = nodes.Count;
                        var node = new Node(pos, id);

                        float dist = (pos - center).magnitude;
                        if (Mathf.Abs(dist - radius) < spacing * 0.5f)
                            node.IsBoundary = true;

                        nodes.Add(node);
                        indexMap[(x, y, z)] = id;
                    }
                }
            }
        }

        List<Tetrahedron> tets = new();
        int idx(int i, int j, int k) => indexMap.ContainsKey((i, j, k)) ? indexMap[(i, j, k)] : -1;

        // Step 2: Decompose cube into 5 tetrahedrons, only if all 8 nodes are inside
        for (int x = -divisions; x < divisions; x++)
        {
            for (int y = -divisions; y < divisions; y++)
            {
                for (int z = -divisions; z < divisions; z++)
                {
                    int[] v = new int[8]
                    {
                        idx(x, y, z),
                        idx(x+1, y, z),
                        idx(x, y+1, z),
                        idx(x+1, y+1, z),
                        idx(x, y, z+1),
                        idx(x+1, y, z+1),
                        idx(x, y+1, z+1),
                        idx(x+1, y+1, z+1)
                    };

                    if (Array.Exists(v, id => id == -1)) continue;

                    // Create 5 tetrahedrons (5-tet subdivision)
                    tets.Add(new Tetrahedron(v[0], v[1], v[2], v[4]));
                    tets.Add(new Tetrahedron(v[1], v[2], v[3], v[7]));
                    tets.Add(new Tetrahedron(v[2], v[4], v[6], v[7]));
                    tets.Add(new Tetrahedron(v[1], v[4], v[5], v[7]));
                    tets.Add(new Tetrahedron(v[4], v[2], v[1], v[7]));
                }
            }
        }

        Nodes = nodes.ToArray();
        Elements = tets.ToArray();
    }




    // Cylinder mesh generation
    public void GenerateCylinderMesh(float radius, float height, float resolution, Vector3 center)
    {
        float spacing = resolution;
        List<Node> nodes = new();
        Dictionary<(int, int, int), int> indexMap = new();

        // Calculate grid divisions
        int radialDivisions = Mathf.CeilToInt(radius * 2 / spacing);
        int heightDivisions = Mathf.CeilToInt(height / spacing);

        // Generate nodes in a rectangular grid, then filter for cylinder shape
        for (int x = -radialDivisions; x <= radialDivisions; x++)
        {
            for (int y = -heightDivisions/2; y <= heightDivisions/2; y++)
            {
                for (int z = -radialDivisions; z <= radialDivisions; z++)
                {
                    Vector3 pos = new Vector3(x * spacing, y * spacing, z * spacing) + center;
                    
                    // Check if point is inside cylinder (distance from Y-axis <= radius, within height bounds)
                    float distFromYAxis = Mathf.Sqrt(pos.x * pos.x + pos.z * pos.z);
                    float relativeY = pos.y - center.y;
                    
                    if (distFromYAxis <= radius && Mathf.Abs(relativeY) <= height / 2)
                    {
                        int id = nodes.Count;
                        var node = new Node(pos, id);

                        // Mark boundary nodes (on cylindrical surface or top/bottom faces)
                        bool onCylindricalSurface = Mathf.Abs(distFromYAxis - radius) < spacing * 0.5f;
                        bool onTopBottomFace = (Mathf.Abs(relativeY - height/2) < spacing * 0.5f || 
                                            Mathf.Abs(relativeY + height/2) < spacing * 0.5f) && 
                                            distFromYAxis <= radius;
                        
                        if (onCylindricalSurface || onTopBottomFace)
                            node.IsBoundary = true;

                        nodes.Add(node);
                        indexMap[(x, y, z)] = id;
                    }
                }
            }
        }

        List<Tetrahedron> tets = new();
        int idx(int i, int j, int k) => indexMap.ContainsKey((i, j, k)) ? indexMap[(i, j, k)] : -1;

        // Generate tetrahedrons using 5-tet decomposition
        for (int x = -radialDivisions; x < radialDivisions; x++)
        {
            for (int y = -heightDivisions/2; y < heightDivisions/2; y++)
            {
                for (int z = -radialDivisions; z < radialDivisions; z++)
                {
                    int[] v = new int[8]
                    {
                        idx(x, y, z),
                        idx(x+1, y, z),
                        idx(x, y+1, z),
                        idx(x+1, y+1, z),
                        idx(x, y, z+1),
                        idx(x+1, y, z+1),
                        idx(x, y+1, z+1),
                        idx(x+1, y+1, z+1)
                    };

                    // Skip if any vertex is outside the cylinder
                    if (Array.Exists(v, id => id == -1)) continue;

                    // Create 5 tetrahedrons
                    tets.Add(new Tetrahedron(v[0], v[1], v[2], v[4]));
                    tets.Add(new Tetrahedron(v[1], v[2], v[3], v[7]));
                    tets.Add(new Tetrahedron(v[2], v[4], v[6], v[7]));
                    tets.Add(new Tetrahedron(v[1], v[4], v[5], v[7]));
                    tets.Add(new Tetrahedron(v[4], v[2], v[1], v[7]));
                }
            }
        }

        Nodes = nodes.ToArray();
        Elements = tets.ToArray();
    }

    // Capsule mesh generation (cylinder with hemispherical ends)
    public void GenerateCapsuleMesh(float radius, float height, float resolution, Vector3 center)
    {
        float spacing = resolution;
        List<Node> nodes = new();
        Dictionary<(int, int, int), int> indexMap = new();

        // Calculate grid divisions
        int radialDivisions = Mathf.CeilToInt(radius * 2 / spacing);
        int heightDivisions = Mathf.CeilToInt(height / spacing);
        int totalHeightDivisions = heightDivisions + 2 * radialDivisions; // Add space for hemispheres

        // Generate nodes in a rectangular grid, then filter for capsule shape
        for (int x = -radialDivisions; x <= radialDivisions; x++)
        {
            for (int y = -totalHeightDivisions/2; y <= totalHeightDivisions/2; y++)
            {
                for (int z = -radialDivisions; z <= radialDivisions; z++)
                {
                    Vector3 pos = new Vector3(x * spacing, y * spacing, z * spacing) + center;
                    
                    float distFromYAxis = Mathf.Sqrt(pos.x * pos.x + pos.z * pos.z);
                    float relativeY = pos.y - center.y;
                    
                    bool isInsideCapsule = false;
                    
                    // Check cylindrical middle section
                    if (Mathf.Abs(relativeY) <= height / 2 && distFromYAxis <= radius)
                    {
                        isInsideCapsule = true;
                    }
                    // Check top hemisphere
                    else if (relativeY > height / 2)
                    {
                        Vector3 topSphereCenter = center + Vector3.up * (height / 2);
                        float distFromTopCenter = (pos - topSphereCenter).magnitude;
                        if (distFromTopCenter <= radius)
                            isInsideCapsule = true;
                    }
                    // Check bottom hemisphere
                    else if (relativeY < -height / 2)
                    {
                        Vector3 bottomSphereCenter = center - Vector3.up * (height / 2);
                        float distFromBottomCenter = (pos - bottomSphereCenter).magnitude;
                        if (distFromBottomCenter <= radius)
                            isInsideCapsule = true;
                    }
                    
                    if (isInsideCapsule)
                    {
                        int id = nodes.Count;
                        var node = new Node(pos, id);

                        // Mark boundary nodes
                        bool isBoundary = false;
                        
                        // Cylindrical surface boundary
                        if (Mathf.Abs(relativeY) <= height / 2 && 
                            Mathf.Abs(distFromYAxis - radius) < spacing * 0.5f)
                        {
                            isBoundary = true;
                        }
                        // Top hemisphere boundary
                        else if (relativeY > height / 2)
                        {
                            Vector3 topSphereCenter = center + Vector3.up * (height / 2);
                            float distFromTopCenter = (pos - topSphereCenter).magnitude;
                            if (Mathf.Abs(distFromTopCenter - radius) < spacing * 0.5f)
                                isBoundary = true;
                        }
                        // Bottom hemisphere boundary
                        else if (relativeY < -height / 2)
                        {
                            Vector3 bottomSphereCenter = center - Vector3.up * (height / 2);
                            float distFromBottomCenter = (pos - bottomSphereCenter).magnitude;
                            if (Mathf.Abs(distFromBottomCenter - radius) < spacing * 0.5f)
                                isBoundary = true;
                        }
                        
                        node.IsBoundary = isBoundary;
                        nodes.Add(node);
                        indexMap[(x, y, z)] = id;
                    }
                }
            }
        }

        List<Tetrahedron> tets = new();
        int idx(int i, int j, int k) => indexMap.ContainsKey((i, j, k)) ? indexMap[(i, j, k)] : -1;

        // Generate tetrahedrons using 5-tet decomposition
        for (int x = -radialDivisions; x < radialDivisions; x++)
        {
            for (int y = -totalHeightDivisions/2; y < totalHeightDivisions/2; y++)
            {
                for (int z = -radialDivisions; z < radialDivisions; z++)
                {
                    int[] v = new int[8]
                    {
                        idx(x, y, z),
                        idx(x+1, y, z),
                        idx(x, y+1, z),
                        idx(x+1, y+1, z),
                        idx(x, y, z+1),
                        idx(x+1, y, z+1),
                        idx(x, y+1, z+1),
                        idx(x+1, y+1, z+1)
                    };

                    // Skip if any vertex is outside the capsule
                    if (Array.Exists(v, id => id == -1)) continue;

                    // Create 5 tetrahedrons
                    tets.Add(new Tetrahedron(v[0], v[1], v[2], v[4]));
                    tets.Add(new Tetrahedron(v[1], v[2], v[3], v[7]));
                    tets.Add(new Tetrahedron(v[2], v[4], v[6], v[7]));
                    tets.Add(new Tetrahedron(v[1], v[4], v[5], v[7]));
                    tets.Add(new Tetrahedron(v[4], v[2], v[1], v[7]));
                }
            }
        }

        Nodes = nodes.ToArray();
        Elements = tets.ToArray();
    }


    
    // using it in stiff matrix 
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


    // public Vector3[] GetNodePositions()
    // {
    //     Vector3[] positions = new Vector3[Nodes.Length];
    //     for (int i = 0; i < Nodes.Length; i++)
    //         positions[i] = Nodes[i].Position;
    //     return positions;
    // }
}
