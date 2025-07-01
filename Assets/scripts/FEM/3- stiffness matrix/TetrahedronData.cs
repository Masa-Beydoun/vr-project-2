
using UnityEngine;

public class TetrahedronData
{
    public Tetrahedron Tetrahedron { get; }
    public float[,] LocalStiffnessMatrix { get; }

    public TetrahedronData(Tetrahedron tet, float[,] ke)
    {
        Tetrahedron = tet;
        LocalStiffnessMatrix = ke;
    }
}
