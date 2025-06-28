using UnityEngine;
using System.Collections.Generic;

public static class GravityForceGenerator
{
    public static float[] ComputeGravityForce(Node[] nodes, Tetrahedron[] tetrahedra, float density, Vector3 gravity)
    {
        int numNodes = nodes.Length;
        float[] forceVector = new float[3 * numNodes];

        foreach (var tet in tetrahedra)
        {
            // Get positions of the 4 corners
            Vector3[] verts = new Vector3[4];
            for (int i = 0; i < 4; i++)
                verts[i] = nodes[tet.NodeIndices[i]].Position;

            // Compute volume of the tetrahedron
            float volume = ComputeTetrahedronVolume(verts);

            // Total weight = mass * gravity = (density * volume) * gravity
            Vector3 totalWeight = density * volume * gravity;

            // Distribute the force equally among the 4 nodes of the tetrahedron
            for (int i = 0; i < 4; i++)
            {
                int nodeIndex = tet.NodeIndices[i];
                forceVector[3 * nodeIndex + 0] += totalWeight.x / 4f;
                forceVector[3 * nodeIndex + 1] += totalWeight.y / 4f;
                forceVector[3 * nodeIndex + 2] += totalWeight.z / 4f;
            }
        }

        return forceVector;
    }

    private static float ComputeTetrahedronVolume(Vector3[] v)
    {
        Vector3 a = v[1] - v[0];
        Vector3 b = v[2] - v[0];
        Vector3 c = v[3] - v[0];
        return Mathf.Abs(Vector3.Dot(a, Vector3.Cross(b, c))) / 6f;
    }
}
