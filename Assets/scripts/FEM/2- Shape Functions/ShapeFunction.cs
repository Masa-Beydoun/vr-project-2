using UnityEngine;

public struct ShapeFunction
{
    public float a, b, c, d;

    public ShapeFunction(float a, float b, float c, float d)
    {
        this.a = a; this.b = b; this.c = c; this.d = d;
    }

    // Evaluate N(x,y,z) = a + b*x + c*y + d*z at a given point
    public float Evaluate(Vector3 point)
    {
        return a + b * point.x + c * point.y + d * point.z;
    }

    public Vector3 Gradient()
    {
    return new Vector3(b, c, d);
    }

}
