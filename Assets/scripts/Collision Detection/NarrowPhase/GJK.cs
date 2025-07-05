using System.Collections.Generic;
using UnityEngine;

public interface IConvexCollider
{
    Vector3 GetFurthestPointInDirection(Vector3 direction);
    Vector3 GetCenter();
}

public class GJK
{
    const int MaxIterations = 30;

    public static bool CheckCollision(IConvexCollider a, IConvexCollider b)
    {
        Vector3 direction = b.GetCenter() - a.GetCenter();
        if (direction == Vector3.zero) direction = Vector3.right;

        List<Vector3> simplex = new List<Vector3>();
        Vector3 point = Support(a, b, direction);
        simplex.Add(point);
        direction = -point;

        for (int i = 0; i < MaxIterations; i++)
        {
            point = Support(a, b, direction);

            if (Vector3.Dot(point, direction) <= 0)
                return false;

            simplex.Add(point);

            if (HandleSimplex(ref simplex, ref direction))
                return true;
        }

        return false;
    }

    private static Vector3 Support(IConvexCollider a, IConvexCollider b, Vector3 direction)
    {
        Vector3 pointA = a.GetFurthestPointInDirection(direction);
        Vector3 pointB = b.GetFurthestPointInDirection(-direction);
        return pointA - pointB;
    }

    private static bool HandleSimplex(ref List<Vector3> simplex, ref Vector3 direction)
    {
        if (simplex.Count == 2)
            return Line(ref simplex, ref direction);
        else if (simplex.Count == 3)
            return Triangle(ref simplex, ref direction);
        else
            return Tetrahedron(ref simplex, ref direction);
    }

    private static bool Line(ref List<Vector3> simplex, ref Vector3 direction)
    {
        Vector3 A = simplex[1];
        Vector3 B = simplex[0];
        Vector3 AB = B - A;
        Vector3 AO = -A;

        if (Vector3.Dot(AB, AO) > 0)
        {
            direction = Vector3.Cross(Vector3.Cross(AB, AO), AB);
        }
        else
        {
            simplex.RemoveAt(0);
            direction = AO;
        }

        return false;
    }

    private static bool Triangle(ref List<Vector3> simplex, ref Vector3 direction)
    {
        Vector3 A = simplex[2];
        Vector3 B = simplex[1];
        Vector3 C = simplex[0];

        Vector3 AB = B - A;
        Vector3 AC = C - A;
        Vector3 AO = -A;

        Vector3 ABC = Vector3.Cross(AB, AC);

        if (Vector3.Dot(Vector3.Cross(ABC, AC), AO) > 0)
        {
            if (Vector3.Dot(AC, AO) > 0)
            {
                simplex.RemoveAt(1); // remove B
                direction = Vector3.Cross(Vector3.Cross(AC, AO), AC);
            }
            else
            {
                return Line(ref simplex, ref direction);
            }
        }
        else
        {
            if (Vector3.Dot(Vector3.Cross(AB, ABC), AO) > 0)
            {
                return Line(ref simplex, ref direction);
            }
            else
            {
                if (Vector3.Dot(ABC, AO) > 0)
                {
                    direction = ABC;
                }
                else
                {
                    Vector3 temp = simplex[0];
                    simplex[0] = simplex[1];
                    simplex[1] = temp;
                    direction = -ABC;
                }
            }
        }

        return false;
    }

    private static bool Tetrahedron(ref List<Vector3> simplex, ref Vector3 direction)
    {
        Vector3 A = simplex[3];
        Vector3 B = simplex[2];
        Vector3 C = simplex[1];
        Vector3 D = simplex[0];

        Vector3 AO = -A;

        Vector3 AB = B - A;
        Vector3 AC = C - A;
        Vector3 AD = D - A;

        Vector3 ABC = Vector3.Cross(AB, AC);
        Vector3 ACD = Vector3.Cross(AC, AD);
        Vector3 ADB = Vector3.Cross(AD, AB);

        if (Vector3.Dot(ABC, AO) > 0)
        {
            simplex.RemoveAt(0); // remove D
            direction = ABC;
            return false;
        }

        if (Vector3.Dot(ACD, AO) > 0)
        {
            simplex.RemoveAt(2); // remove B
            direction = ACD;
            return false;
        }

        if (Vector3.Dot(ADB, AO) > 0)
        {
            simplex.RemoveAt(1); // remove C
            direction = ADB;
            return false;
        }

        return true; // collision confirmed
    }
}
