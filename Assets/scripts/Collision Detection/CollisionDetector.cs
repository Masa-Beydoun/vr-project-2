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
    public static List<CollisionResultEnhanced> CheckCollision(SpringMassSystem objA, SpringMassSystem objB)
    {
        List<CollisionResultEnhanced> results = new List<CollisionResultEnhanced>();
        List<Vector3> simplex = new List<Vector3>();

        if (GJK.Detect(objA, objB, simplex))
        {
            if (EPA.Expand(objA, objB, simplex, out Vector3 normal, out float depth, out Vector3 contactPoint))
            {
                List<(MassPoint, MassPoint)> pairs = FindAllPenetratingPairs(objA, objB, normal, depth);

                foreach (var (pa, pb) in pairs)
                {
                    Vector3 relativeVel = pb.velocity - pa.velocity;
                    float relVelAlongNormal = Vector3.Dot(relativeVel, normal);

                    results.Add(new CollisionResultEnhanced
                    {
                        pointA = pa,
                        pointB = pb,
                        normal = normal,
                        penetrationDepth = depth,
                        contactPoint = 0.5f * (pa.position + pb.position),
                        relativeVelocity = relVelAlongNormal,
                        collisionType = DetermineCollisionType(pa, pb, relVelAlongNormal),
                        collided = true
                    }) ;
                }
            }
        }

        return results;
    }

    private static List<(MassPoint, MassPoint)> FindAllPenetratingPairs(
    SpringMassSystem a, SpringMassSystem b, Vector3 normal, float penetrationDepth)
    {
        List<(MassPoint, MassPoint)> pairs = new List<(MassPoint, MassPoint)>();
        float epsilon = 1e-9f;
        float maxDistance = penetrationDepth + epsilon;

        foreach (var pa in a.GetMassPoints())
        {
            foreach (var pb in b.GetMassPoints())
            {
                Vector3 diff = pa.position - pb.position;
                float proj = Vector3.Dot(diff, normal);

                if (proj > -epsilon && proj <= maxDistance)
                {
                    float distanceSq = diff.sqrMagnitude;

                    if (distanceSq <= (penetrationDepth + epsilon) * (penetrationDepth + epsilon))
                    {
                        pairs.Add((pa, pb));
                    }
                }
            }
        }

        return pairs;
    }
    private static CollisionType DetermineCollisionType(MassPoint a, MassPoint b, float relVel)
    {
        bool aStatic = a.isPinned;
        bool bStatic = b.isPinned;

        if (aStatic && bStatic)
            return CollisionType.SpringMass_Static;
        else if (aStatic || bStatic)
            return CollisionType.SpringMass_Static;
        else
            return CollisionType.SpringMass_SpringMass;
    }



}
