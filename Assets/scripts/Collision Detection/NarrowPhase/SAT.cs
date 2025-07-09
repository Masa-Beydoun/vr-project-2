using System.Collections.Generic;
using UnityEngine;

public static class SAT
{
    public static bool TestCollision(List<Vector3> pointsA, List<Vector3> pointsB)
    {
        List<Vector3> axes = new List<Vector3>();

        AddFaceNormals(pointsA, axes);

        AddFaceNormals(pointsB, axes);

        AddCrossProductAxes(pointsA, pointsB, axes);

        foreach (Vector3 axis in axes)
        {
            if (IsSeparated(axis.normalized, pointsA, pointsB))
                return false;
        }

        return true;
    }

    private static void AddFaceNormals(List<Vector3> points, List<Vector3> axes)
    {
        if (points.Count < 3) return;

        for (int i = 0; i < points.Count - 2; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            Vector3 c = points[i + 2];

            Vector3 ab = b - a;
            Vector3 ac = c - a;

            Vector3 normal = Vector3.Cross(ab, ac);
            if (normal.sqrMagnitude > 1e-5f)
                axes.Add(normal.normalized);
        }
    }

    private static void AddCrossProductAxes(List<Vector3> aPoints, List<Vector3> bPoints, List<Vector3> axes)
    {
        int count = 5;
        for (int i = 0; i < Mathf.Min(aPoints.Count, count); i++)
        {
            for (int j = 0; j < Mathf.Min(bPoints.Count, count); j++)
            {
                Vector3 dirA = aPoints[(i + 1) % aPoints.Count] - aPoints[i];
                Vector3 dirB = bPoints[(j + 1) % bPoints.Count] - bPoints[j];
                Vector3 cross = Vector3.Cross(dirA, dirB);
                if (cross.sqrMagnitude > 1e-5f)
                    axes.Add(cross.normalized);
            }
        }
    }

    private static bool IsSeparated(Vector3 axis, List<Vector3> aPoints, List<Vector3> bPoints)
    {
        GetMinMax(axis, aPoints, out float minA, out float maxA);
        GetMinMax(axis, bPoints, out float minB, out float maxB);

        return (maxA < minB) || (maxB < minA);
    }

    private static void GetMinMax(Vector3 axis, List<Vector3> points, out float min, out float max)
    {
        min = max = Vector3.Dot(axis, points[0]);
        for (int i = 1; i < points.Count; i++)
        {
            float proj = Vector3.Dot(axis, points[i]);
            if (proj < min) min = proj;
            if (proj > max) max = proj;
        }
    }
}
