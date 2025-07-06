using System.Collections.Generic;
using UnityEngine;

public static class SAT
{
    public static bool OBBvsOBB(PhysicalObject a, PhysicalObject b)
    {
        Vector3[] axes = new Vector3[15];
        Quaternion rA = a.transform.rotation, rB = b.transform.rotation;
        Vector3[] uA = { rA * Vector3.right, rA * Vector3.up, rA * Vector3.forward };
        Vector3[] uB = { rB * Vector3.right, rB * Vector3.up, rB * Vector3.forward };

        Vector3 halfA = a.transform.localScale / 2f;
        Vector3 halfB = b.transform.localScale / 2f;

        Vector3 centerA = a.transform.position;
        Vector3 centerB = b.transform.position;
        Vector3 T = centerB - centerA;

        for (int i = 0; i < 3; i++) axes[i] = uA[i];
        for (int i = 0; i < 3; i++) axes[3 + i] = uB[i];

        int idx = 6;
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                axes[idx++] = Vector3.Cross(uA[i], uB[j]);

        foreach (var axis in axes)
        {
            if (axis.sqrMagnitude < 1e-6f) continue; 
            if (!OverlapOnAxis(a, b, axis.normalized)) return false;
        }

        return true;
    }

    public static bool OverlapOnAxis(PhysicalObject a, PhysicalObject b, Vector3 axis)
    {
        float projA = ProjectHalfExtent(a, axis);
        float projB = ProjectHalfExtent(b, axis);
        float centerDist = Mathf.Abs(Vector3.Dot((b.transform.position - a.transform.position), axis));
        return centerDist <= projA + projB;
    }

    public static float ProjectHalfExtent(PhysicalObject obj, Vector3 axis)
    {
        Vector3 half = obj.transform.localScale / 2f;
        Quaternion r = obj.transform.rotation;
        Vector3[] u = { r * Vector3.right, r * Vector3.up, r * Vector3.forward };
        return Mathf.Abs(Vector3.Dot(axis, u[0])) * half.x +
               Mathf.Abs(Vector3.Dot(axis, u[1])) * half.y +
               Mathf.Abs(Vector3.Dot(axis, u[2])) * half.z;
    }

    public static bool SphereOBBCollisionWithMTV(PhysicalObject sphere, PhysicalObject obb, out Vector3 mtv)
    {
        mtv = Vector3.zero;

        Vector3 sphereCenter = sphere.transform.position;
        Vector3 obbCenter = obb.transform.position;

        Quaternion obbRotation = obb.transform.rotation;
        Vector3 obbHalfSize = obb.transform.localScale / 2f;

        Vector3 delta = sphereCenter - obbCenter;

        Vector3 localDelta = Quaternion.Inverse(obbRotation) * delta;

        Vector3 clamped = new Vector3(
            Mathf.Clamp(localDelta.x, -obbHalfSize.x, obbHalfSize.x),
            Mathf.Clamp(localDelta.y, -obbHalfSize.y, obbHalfSize.y),
            Mathf.Clamp(localDelta.z, -obbHalfSize.z, obbHalfSize.z)
        );

        Vector3 closestPoint = obbCenter + (obbRotation * clamped);

        Vector3 correctionVector = sphereCenter - closestPoint;
        float distanceSqr = correctionVector.sqrMagnitude;
        float radius = sphere.radius;

        if (distanceSqr >= radius * radius)
            return false; 

        float distance = Mathf.Sqrt(distanceSqr);

        if (distance < 1e-6f)
        {
            Vector3 arbitraryAxis = obbRotation * Vector3.up;
            mtv = arbitraryAxis * (radius);
        }
        else
        {
            Vector3 direction = correctionVector.normalized;
            float penetrationDepth = radius - distance;
            mtv = direction * penetrationDepth;
        }

        return true;
    }

    public static bool SphereSphereCollisionWithMTV(PhysicalObject a, PhysicalObject b, out Vector3 mtv)
    {
        mtv = Vector3.zero;

        Vector3 delta = b.transform.position - a.transform.position;
        float distSqr = delta.sqrMagnitude;
        float radiusSum = a.radius + b.radius;

        if (distSqr >= radiusSum * radiusSum)
            return false;

        float dist = Mathf.Sqrt(distSqr);
        float penetration = radiusSum - dist;

        if (dist > 1e-6f)
            mtv = delta.normalized * penetration;
        else
            mtv = Vector3.up * penetration;

        return true;
    }

    public static bool AABBAABBCollisionWithMTV(PhysicalObject a, PhysicalObject b, out Vector3 mtv)
    {
        mtv = Vector3.zero;

        Vector3 aMin = a.transform.position - a.transform.localScale / 2f;
        Vector3 aMax = a.transform.position + a.transform.localScale / 2f;
        Vector3 bMin = b.transform.position - b.transform.localScale / 2f;
        Vector3 bMax = b.transform.position + b.transform.localScale / 2f;

        if (aMax.x < bMin.x || aMin.x > bMax.x ||
            aMax.y < bMin.y || aMin.y > bMax.y ||
            aMax.z < bMin.z || aMin.z > bMax.z)
            return false;

        float dx = Mathf.Min(aMax.x - bMin.x, bMax.x - aMin.x);
        float dy = Mathf.Min(aMax.y - bMin.y, bMax.y - aMin.y);
        float dz = Mathf.Min(aMax.z - bMin.z, bMax.z - aMin.z);

        if (dx < dy && dx < dz) mtv = new Vector3(a.transform.position.x < b.transform.position.x ? -dx : dx, 0, 0);
        else if (dy < dz) mtv = new Vector3(0, a.transform.position.y < b.transform.position.y ? -dy : dy, 0);
        else mtv = new Vector3(0, 0, a.transform.position.z < b.transform.position.z ? -dz : dz);

        return true;
    }

    public static bool OBBOBBCollisionWithMTV(PhysicalObject a, PhysicalObject b, out Vector3 mtv)
    {
        mtv = Vector3.zero;
        float minPenetration = float.MaxValue;
        Vector3 bestAxis = Vector3.zero;

        Vector3[] axes = GetSeparatingAxes(a, b);
        foreach (var axis in axes)
        {
            if (axis == Vector3.zero) continue;

            float minA, maxA, minB, maxB;
            ProjectOBB(a, axis, out minA, out maxA);
            ProjectOBB(b, axis, out minB, out maxB);

            if (!Overlaps(minA, maxA, minB, maxB))
                return false;

            float penetration = GetPenetration(minA, maxA, minB, maxB);
            if (penetration < minPenetration)
            {
                minPenetration = penetration;
                bestAxis = axis;
            }
        }

        Vector3 direction = b.transform.position - a.transform.position;
        if (Vector3.Dot(direction, bestAxis) < 0)
            bestAxis = -bestAxis;

        mtv = bestAxis * minPenetration;
        return true;
    }

    public static Vector3[] GetSeparatingAxes(PhysicalObject a, PhysicalObject b)
    {
        Vector3[] axesA = GetOBBAxes(a);
        Vector3[] axesB = GetOBBAxes(b);

        List<Vector3> axes = new List<Vector3>();

        axes.AddRange(axesA);
        axes.AddRange(axesB);

        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
            {
                Vector3 cross = Vector3.Cross(axesA[i], axesB[j]);
                if (cross.sqrMagnitude > 1e-6f)
                    axes.Add(cross.normalized);
            }

        return axes.ToArray();
    }

    public static Vector3[] GetOBBAxes(PhysicalObject obj)
    {
        Quaternion rot = obj.transform.rotation;
        return new Vector3[]
        {
        rot * Vector3.right,
        rot * Vector3.up,
        rot * Vector3.forward
        };
    }

    public static void ProjectOBB(PhysicalObject obj, Vector3 axis, out float min, out float max)
    {
        Vector3 center = obj.transform.position;
        Quaternion rot = obj.transform.rotation;
        Vector3 halfSize = obj.transform.localScale / 2f;

        Vector3[] corners = new Vector3[8];
        int i = 0;
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                    corners[i++] = center + rot * Vector3.Scale(halfSize, new Vector3(x, y, z));

        min = max = Vector3.Dot(axis, corners[0]);
        for (i = 1; i < 8; i++)
        {
            float proj = Vector3.Dot(axis, corners[i]);
            min = Mathf.Min(min, proj);
            max = Mathf.Max(max, proj);
        }
    }

    public static bool Overlaps(float minA, float maxA, float minB, float maxB)
    {
        return !(minA > maxB || maxA < minB);
    }

    public static float GetPenetration(float minA, float maxA, float minB, float maxB)
    {
        return Mathf.Min(maxA, maxB) - Mathf.Max(minA, minB);
    }

}
