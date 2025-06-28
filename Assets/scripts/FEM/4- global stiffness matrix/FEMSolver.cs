using UnityEngine;
using System.Collections.Generic;

public class FEMSolver
{
    // public Vector3[] Nodes; // mesh nodes
    public Tetrahedron[] Tetrahedra; // mesh elements

    public float[,] GlobalStiffnessMatrix;


    public Node[] Nodes;
    // public int[,] Tetrahedra;
    public float[,] GlobalMassMatrix;


    public void AssembleGlobalStiffness(PhysicalMaterial material)
    {
        int nNodes = Nodes.Length;
        GlobalStiffnessMatrix = new float[3 * nNodes, 3 * nNodes];

        // Optional: Store local element matrices for debugging/analysis
        List<TetrahedronData> elementDataList = new List<TetrahedronData>();

        foreach (var tet in Tetrahedra)
        {
            // Get vertex positions of the current tetrahedron
            Vector3[] vertices = new Vector3[4];
            for (int i = 0; i < 4; i++)
                vertices[i] = Nodes[tet.NodeIndices[i]].Position;


            // Compute 12x12 local stiffness matrix for this tetrahedron
            float[,] Ke = ElementStiffnessCalculator.ComputeLocalStiffness(vertices, material);

            // Store for optional use
            elementDataList.Add(new TetrahedronData(tet, Ke));

            // Assemble Ke into the global matrix
            for (int i = 0; i < 4; i++)
            {
                int globalNodeI = tet.NodeIndices[i];
                for (int j = 0; j < 4; j++)
                {
                    int globalNodeJ = tet.NodeIndices[j];

                    for (int dofI = 0; dofI < 3; dofI++) // DOF for node i (x, y, z)
                    {
                        for (int dofJ = 0; dofJ < 3; dofJ++) // DOF for node j (x, y, z)
                        {
                            int globalRow = 3 * globalNodeI + dofI;
                            int globalCol = 3 * globalNodeJ + dofJ;

                            int localRow = 3 * i + dofI;
                            int localCol = 3 * j + dofJ;

                            GlobalStiffnessMatrix[globalRow, globalCol] += Ke[localRow, localCol];
                        }
                    }
                }
            }
        }
    }
}
