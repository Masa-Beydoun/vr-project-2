using System.Collections.Generic;
using UnityEngine;

public static class GJK
{
    public static bool Detect(SpringMassSystem a, SpringMassSystem b, List<Vector3> simplex)
    {
        Vector3 direction = a.transform.position - b.transform.position;
        if (direction == Vector3.zero)
            direction = Vector3.right;

        Vector3 support = Support(a, b, direction);
        simplex.Clear();
        simplex.Add(support);

        direction = -support;

        for (int iteration = 0; iteration < 30; iteration++)
        {
            support = Support(a, b, direction);

            if (Vector3.Dot(support, direction) < 0)
                return false;

            simplex.Add(support);

            if (HandleSimplex(simplex, ref direction))
                return true;
        }

        return false;
    }

    private static Vector3 Support(SpringMassSystem a, SpringMassSystem b, Vector3 direction)
    {
        Vector3 p1 = GetFarthestPoint(a.GetMassPoints(), direction);
        Vector3 p2 = GetFarthestPoint(b.GetMassPoints(), -direction);
        return p1 - p2;
    }

    private static Vector3 GetFarthestPoint(List<MassPoint> points, Vector3 direction)
    {
        float maxDot = float.MinValue;
        Vector3 best = points[0].position;

        foreach (var p in points)
        {
            float dot = Vector3.Dot(p.position, direction);
            if (dot > maxDot)
            {
                maxDot = dot;
                best = p.position;
            }
        }
        return best;
    }

    private static bool HandleSimplex(List<Vector3> simplex, ref Vector3 direction)
    {
        if (simplex.Count == 2)
        {
            return HandleLine(simplex, ref direction);
        }
        else if (simplex.Count == 3)
        {
            return HandleTriangle(simplex, ref direction);
        }
        else if (simplex.Count == 4)
        {
            return HandleTetrahedron(simplex, ref direction);
        }

        return false;
    }

    private static bool HandleLine(List<Vector3> simplex, ref Vector3 direction)
    {
        Vector3 a = simplex[1];
        Vector3 b = simplex[0];
        Vector3 ab = b - a;
        Vector3 ao = -a;

        direction = Vector3.Cross(Vector3.Cross(ab, ao), ab);
        return false;
    }

    private static bool HandleTriangle(List<Vector3> simplex, ref Vector3 direction)
    {
        Vector3 a = simplex[2];
        Vector3 b = simplex[1];
        Vector3 c = simplex[0];

        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ao = -a;

        Vector3 abc = Vector3.Cross(ab, ac);

        if (Vector3.Dot(Vector3.Cross(abc, ac), ao) > 0)
        {
            if (Vector3.Dot(ac, ao) > 0)
            {
                simplex.RemoveAt(1); // remove b
                direction = Vector3.Cross(Vector3.Cross(ac, ao), ac);
            }
            else
            {
                return HandleLine(new List<Vector3> { b, a }, ref direction);
            }
        }
        else
        {
            if (Vector3.Dot(Vector3.Cross(ab, abc), ao) > 0)
            {
                return HandleLine(new List<Vector3> { b, a }, ref direction);
            }
            else
            {
                if (Vector3.Dot(abc, ao) > 0)
                {
                    direction = abc;
                }
                else
                {
                    Vector3 temp = simplex[0];
                    simplex[0] = simplex[1];
                    simplex[1] = temp;
                    direction = -abc;
                }
            }
        }
        return false;
    }

    private static bool HandleTetrahedron(List<Vector3> simplex, ref Vector3 direction)
    {
        Vector3 a = simplex[3];
        Vector3 b = simplex[2];
        Vector3 c = simplex[1];
        Vector3 d = simplex[0];

        Vector3 ao = -a;

        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 ad = d - a;

        Vector3 abc = Vector3.Cross(ab, ac);
        Vector3 acd = Vector3.Cross(ac, ad);
        Vector3 adb = Vector3.Cross(ad, ab);

        if (Vector3.Dot(abc, ao) > 0)
        {
            simplex.RemoveAt(0); // remove d
            direction = abc;
            return false;
        }

        if (Vector3.Dot(acd, ao) > 0)
        {
            simplex.RemoveAt(2); // remove b
            direction = acd;
            return false;
        }

        if (Vector3.Dot(adb, ao) > 0)
        {
            simplex.RemoveAt(1); // remove c
            direction = adb;
            return false;
        }

        return true; // Origin is inside tetrahedron
    }
}
