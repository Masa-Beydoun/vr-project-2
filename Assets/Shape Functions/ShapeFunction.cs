using UnityEngine;

public struct ShapeFunction
{
    public float a, b, c, d;

    public ShapeFunction(float a, float b, float c, float d)
    {
        this.a = a; this.b = b; this.c = c; this.d = d;
    }

    public float Evaluate(Vector3 point)
    {
        return a + b * point.x + c * point.y + d * point.z;
    }
}
