using System.Collections.Generic;
using UnityEngine;

public static class EPA
{
    private class Triangle
    {
        public Vector3 a, b, c;
        public Vector3 normal;
        public float distance;

        public Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            ComputeNormal();
        }

        public void ComputeNormal()
        {
            normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            distance = Vector3.Dot(normal, a);
        }

        public bool IsFacingOrigin() => Vector3.Dot(normal, a) > 0;
    }

    public static bool Expand(SpringMassSystem objA, SpringMassSystem objB, List<Vector3> initialSimplex,
                              out Vector3 collisionNormal, out float penetrationDepth, out Vector3 contactPoint)
    {
        const int maxIterations = 50;
        const float tolerance = 1e-4f;

        collisionNormal = Vector3.zero;
        penetrationDepth = 0f;
        contactPoint = Vector3.zero;

        List<Triangle> polytope = InitializePolytope(initialSimplex);
        if (polytope == null)
            return false;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            Triangle closestFace = null;
            float minDistance = float.MaxValue;

            foreach (var face in polytope)
            {
                float dist = Mathf.Abs(Vector3.Dot(face.normal, face.a));
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestFace = face;
                }
            }

            if (closestFace == null)
                break;

            Vector3 newPoint = Support(objA, objB, closestFace.normal);

            float d = Vector3.Dot(newPoint, closestFace.normal);
            if (d - minDistance < tolerance)
            {
                collisionNormal = closestFace.normal;
                penetrationDepth = d;
                contactPoint = newPoint - closestFace.normal * d;
                return true;
            }

            List<(Vector3, Vector3)> edgeBuffer = new List<(Vector3, Vector3)>();

            for (int i = polytope.Count - 1; i >= 0; i--)
            {
                var face = polytope[i];
                if (Vector3.Dot(face.normal, newPoint - face.a) > 0)
                {
                    AddUniqueEdge(edgeBuffer, face.a, face.b);
                    AddUniqueEdge(edgeBuffer, face.b, face.c);
                    AddUniqueEdge(edgeBuffer, face.c, face.a);
                    polytope.RemoveAt(i);
                }
            }

            foreach (var edge in edgeBuffer)
            {
                polytope.Add(new Triangle(edge.Item1, edge.Item2, newPoint));
            }
        }

        return false;
    }

    private static List<Triangle> InitializePolytope(List<Vector3> simplex)
    {
        if (simplex.Count < 4) return null;

        List<Triangle> faces = new List<Triangle>
        {
            new Triangle(simplex[0], simplex[1], simplex[2]),
            new Triangle(simplex[0], simplex[2], simplex[3]),
            new Triangle(simplex[0], simplex[3], simplex[1]),
            new Triangle(simplex[1], simplex[3], simplex[2]),
        };

        foreach (var face in faces)
        {
            if (!face.IsFacingOrigin())
            {
                Vector3 temp = face.b;
                face.b = face.c;
                face.c = temp;
                face.ComputeNormal();
            }
        }

        return faces;
    }

    private static Vector3 Support(SpringMassSystem a, SpringMassSystem b, Vector3 direction)
    {
        Vector3 p1 = GetFurthestPoint(a, direction);
        Vector3 p2 = GetFurthestPoint(b, -direction);
        return p1 - p2;
    }

    private static Vector3 GetFurthestPoint(SpringMassSystem obj, Vector3 dir)
    {
        float maxDot = float.MinValue;
        Vector3 best = Vector3.zero;
        foreach (var p in obj.GetPositions())
        {
            float dot = Vector3.Dot(p, dir);
            if (dot > maxDot)
            {
                maxDot = dot;
                best = p;
            }
        }
        return best;
    }

    private static void AddUniqueEdge(List<(Vector3, Vector3)> edges, Vector3 a, Vector3 b)
    {
        for (int i = 0; i < edges.Count; i++)
        {
            if ((Approximately(edges[i].Item1, b) && Approximately(edges[i].Item2, a)) ||
                (Approximately(edges[i].Item1, a) && Approximately(edges[i].Item2, b)))
            {
                edges.RemoveAt(i);
                return;
            }
        }
        edges.Add((a, b));
    }

    private static bool Approximately(Vector3 a, Vector3 b)
    {
        return (a - b).sqrMagnitude < 1e-6f;
    }
}
