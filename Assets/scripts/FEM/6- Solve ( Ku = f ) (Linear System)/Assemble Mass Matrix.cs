using UnityEngine;

public static class MassMatrixAssembler
{
    public static float[,] Assemble(Node[] nodes, Tetrahedron[] elements, PhysicalMaterial material)
    {
        int dof = nodes.Length * 3;
        float[,] M = new float[dof, dof];

        foreach (var tet in elements)
        {
            Vector3[] verts = new Vector3[4];
            for (int i = 0; i < 4; i++)
                verts[i] = nodes[tet.NodeIndices[i]].Position;

            float volume = ElementStiffnessCalculator.ComputeTetrahedronVolume(verts);
            float mass = material.Density * volume;
            float lumpedMass = mass / 4f;  // equally distribute mass to 4 nodes

            // Divide lumpedMass by 3 since we have 3 DOFs per node
            float lumpedMassPerDOF = lumpedMass / 3f;

            for (int i = 0; i < 4; i++)
            {
                int nodeIndex = tet.NodeIndices[i];
                for (int k = 0; k < 3; k++)  // x,y,z DOFs
                {
                    int globalIndex = nodeIndex * 3 + k;
                    M[globalIndex, globalIndex] += lumpedMassPerDOF;
                }
            }
        }

        return M;
    }
}
