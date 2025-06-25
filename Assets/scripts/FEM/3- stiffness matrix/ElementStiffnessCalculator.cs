using UnityEngine;

public static class ElementStiffnessCalculator

                        // STIFFNESS LOW :  K = V * Bᵀ * D * B  //
{
    public static float[,] ComputeLocalStiffness(Vector3[] vertices, PhysicalMaterial material)
    {
        // Compute shape functions gradients (constant per element)
        ShapeFunction[] N = ShapeFunctionCalculator.ComputeShapeFunctions(vertices);

        // grads[i] = (dNi/dx, dNi/dy, dNi/dz)
        Vector3[] grads = new Vector3[4];
        for (int i = 0; i < 4; i++)
            grads[i] = new Vector3(N[i].b, N[i].c, N[i].d);

        float volume = ComputeTetrahedronVolume(vertices);

    
        // (3D Hooke's Law) //
        // Build the constitutive matrix D (6x6)
        float E = material.youngsModulus;
        float nu = material.poissonRatio;

        float coef = E / ((1 + nu) * (1 - 2 * nu));
        float[,] D = new float[6, 6]
        {
            { coef * (1 - nu), coef * nu,       coef * nu,       0,                      0,                      0 },
            { coef * nu,       coef * (1 - nu), coef * nu,       0,                      0,                      0 },
            { coef * nu,       coef * nu,       coef * (1 - nu), 0,                      0,                      0 },
            { 0,               0,               0,               coef * (1 - 2 * nu) / 2, 0,                      0 },
            { 0,               0,               0,               0,                      coef * (1 - 2 * nu) / 2, 0 },
            { 0,               0,               0,               0,                      0,                      coef * (1 - 2 * nu) / 2 }
        };

        // Build B matrix (6x12)
        float[,] B = new float[6, 12];
        for (int i = 0; i < 4; i++)
        {
            int col = i * 3;
            float dNx = grads[i].x;
            float dNy = grads[i].y;
            float dNz = grads[i].z;

            // ε_xx
            B[0, col + 0] = dNx;
            B[0, col + 1] = 0;
            B[0, col + 2] = 0;

            // ε_yy
            B[1, col + 0] = 0;
            B[1, col + 1] = dNy;
            B[1, col + 2] = 0;

            // ε_zz
            B[2, col + 0] = 0;
            B[2, col + 1] = 0;
            B[2, col + 2] = dNz;

            // γ_xy
            B[3, col + 0] = dNy;
            B[3, col + 1] = dNx;
            B[3, col + 2] = 0;

            // γ_yz
            B[4, col + 0] = 0;
            B[4, col + 1] = dNz;
            B[4, col + 2] = dNy;

            // γ_zx
            B[5, col + 0] = dNz;
            B[5, col + 1] = 0;
            B[5, col + 2] = dNx;
        }

        // Compute K = V * Bᵀ * D * B
        float[,] Bt = Transpose(B);
        float[,] BtD = MultiplyMatrices(Bt, D);
        float[,] BtDB = MultiplyMatrices(BtD, B);

        // Multiply by volume
        float[,] Ke = new float[12, 12];
        for (int i = 0; i < 12; i++)
            for (int j = 0; j < 12; j++)
                Ke[i, j] = BtDB[i, j] * volume;

        return Ke;
    }

    // Helper: Compute tetrahedron volume
    public static float ComputeTetrahedronVolume(Vector3[] v)
    {
        Vector3 a = v[1] - v[0];
        Vector3 b = v[2] - v[0];
        Vector3 c = v[3] - v[0];
        return Mathf.Abs(Vector3.Dot(a, Vector3.Cross(b, c))) / 6f;
    }

    // Helper: Matrix transpose
    private static float[,] Transpose(float[,] mat)
    {
        int rows = mat.GetLength(0);
        int cols = mat.GetLength(1);
        float[,] result = new float[cols, rows];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                result[j, i] = mat[i, j];
        return result;
    }

    // Helper: Matrix multiplication (A: m x n, B: n x p)
    private static float[,] MultiplyMatrices(float[,] A, float[,] B)
    {
        int m = A.GetLength(0);
        int n = A.GetLength(1);
        int p = B.GetLength(1);
        float[,] result = new float[m, p];
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < p; j++)
            {
                float sum = 0f;
                for (int k = 0; k < n; k++)
                    sum += A[i, k] * B[k, j];
                result[i, j] = sum;
            }
        }
        return result;
    }
}
