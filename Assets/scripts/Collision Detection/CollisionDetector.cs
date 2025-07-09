using System.Collections.Generic;
using UnityEngine;

public struct CollisionResult
{
    public bool collided;
    public Vector3 normal;
    public float penetrationDepth;
    public Vector3 contactPoint;
    public MassPoint pointA;
    public MassPoint pointB;
}

public static class CollisionDetector
{
    public static CollisionResult CheckCollision(SpringMassSystem objA, SpringMassSystem objB)
    {
        List<Vector3> simplex = new List<Vector3>();
        if (GJK.Detect(objA, objB, simplex))
        {
            if (EPA.Expand(objA, objB, simplex, out Vector3 normal, out float depth, out Vector3 contactPoint))
            {
                (MassPoint pa, MassPoint pb) = FindClosestSupportPoints(objA, objB, normal);

                return new CollisionResult
                {
                    collided = true,
                    normal = normal,
                    penetrationDepth = depth,
                    contactPoint = contactPoint,
                    pointA = pa,
                    pointB = pb
                };
            }
        }

        return new CollisionResult { collided = false };
    }

    private static (MassPoint, MassPoint) FindClosestSupportPoints(SpringMassSystem a, SpringMassSystem b, Vector3 direction)
    {
        MassPoint bestA = null, bestB = null;
        float maxDot = float.MinValue;

        foreach (var pa in a.GetMassPoints())
        {
            foreach (var pb in b.GetMassPoints())
            {
                Vector3 diff = pa.position - pb.position;
                float dot = Vector3.Dot(diff, direction);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    bestA = pa;
                    bestB = pb;
                }
            }
        }
        return (bestA, bestB);
    }
}
