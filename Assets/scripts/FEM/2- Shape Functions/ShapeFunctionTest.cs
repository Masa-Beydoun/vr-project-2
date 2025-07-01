using UnityEngine;

public class ShapeFunctionTest : MonoBehaviour
{
    void Start()
    {
        Vector3[] tetrahedron = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1)
        };

        ShapeFunction[] shapeFuncs = ShapeFunctionCalculator.ComputeShapeFunctions(tetrahedron);

        Vector3 center = (tetrahedron[0] + tetrahedron[1] + tetrahedron[2] + tetrahedron[3]) / 4f;

        for (int i = 0; i < shapeFuncs.Length; i++)
        {
            float value = shapeFuncs[i].Evaluate(center);
            Debug.Log($"N{i + 1} at center = {value}");
        }

        float sum = 0f;
        foreach (var sf in shapeFuncs)
            sum += sf.Evaluate(center);

        Debug.Log($"Sum of N_i at center = {sum} (should be ≈ 1)");
    }
}
