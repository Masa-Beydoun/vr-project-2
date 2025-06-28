using UnityEngine;

public class FEMDynamics
                    //  F = M⋅u¨ + K⋅u
{
    public float[] Displacement;  // u
    public float[] Velocity;      // v
    public float TimeStep = 0.01f;
    public float DampingFactor = 0.98f; // optional: tweak for stability (0.95–1.0)

    public void Initialize(int dofCount)
    {
        Displacement = new float[dofCount];
        Velocity = new float[dofCount];
    }

    public void Step(float[,] M, float[,] K, float[] f)
    {
        int n = Displacement.Length;
        float[] acceleration = new float[n];

        // Internal force: K * u
        float[] Ku = MultiplyMatrixVector(K, Displacement);

        // Net force = external - internal
        float[] netForce = new float[n];
        for (int i = 0; i < n; i++)
            netForce[i] = f[i] - Ku[i];

        // Compute acceleration (assume M is diagonal/lumped)
        for (int i = 0; i < n; i++)
            acceleration[i] = M[i, i] != 0f ? netForce[i] / M[i, i] : 0f;

        // Time integration (Semi-Implicit Euler with damping)
        // vt+Δt=vt+a⋅Δt
        for (int i = 0; i < n; i++)
        {
            Velocity[i] += acceleration[i] * TimeStep;
            Velocity[i] *= DampingFactor; // apply damping
            Displacement[i] += Velocity[i] * TimeStep;
        }
    }

    private float[] MultiplyMatrixVector(float[,] mat, float[] vec)
    {
        int n = vec.Length;
        float[] result = new float[n];

        for (int i = 0; i < n; i++)
        {
            float sum = 0f;
            for (int j = 0; j < n; j++)
                sum += mat[i, j] * vec[j];
            result[i] = sum;
        }

        return result;
    }

    // Displacement[i] → تمثل الموضع الحالي لكل عقدة
    // Velocity[i] → تمثل السرعة اللحظية
}
