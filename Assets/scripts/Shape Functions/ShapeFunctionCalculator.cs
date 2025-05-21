using UnityEngine;
using System;

public static class ShapeFunctionCalculator
{
    public static ShapeFunction[] ComputeShapeFunctions(Vector3[] vertices)
    {
        if (vertices.Length != 4)
            throw new ArgumentException("A tetrahedron must have exactly 4 vertices.");

        Matrix4x4 A = new Matrix4x4();

        for (int i = 0; i < 4; i++)
        {
            A[i, 0] = 1;
            A[i, 1] = vertices[i].x;
            A[i, 2] = vertices[i].y;
            A[i, 3] = vertices[i].z;
        }

        ShapeFunction[] shapeFunctions = new ShapeFunction[4];

        Matrix4x4 Ainv = A.inverse;

        for (int i = 0; i < 4; i++)
        {
            Vector4 rhs = Vector4.zero;
            rhs[i] = 1;

            Vector4 coeffs = Ainv * rhs;

            shapeFunctions[i] = new ShapeFunction(coeffs.x, coeffs.y, coeffs.z, coeffs.w);
        }

        return shapeFunctions;
    }
}
