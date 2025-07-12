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

        // Add debug output
        //Debug.Log($"[CollisionDetector] Checking collision between {objA.name} and {objB.name}");
        //Debug.Log($"[CollisionDetector] Object A points: {objA.allPoints?.Count ?? 0}, Object B points: {objB.allPoints?.Count ?? 0}");

        // Check if objects have points
        if (objA.allPoints == null || objB.allPoints == null || objA.allPoints.Count == 0 || objB.allPoints.Count == 0)
        {
            Debug.LogWarning($"[CollisionDetector] One or both objects have no mass points!");
            return results;
        }

        // Add fallback simple distance-based collision detection
        bool gjkResult = false;
        try
        {
            gjkResult = GJK.Detect(objA, objB, simplex);
            Debug.Log($"[CollisionDetector] GJK result: {gjkResult}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CollisionDetector] GJK failed: {e.Message}");
            Debug.LogError($"[CollisionDetector] Falling back to simple collision detection");
            return SimpleCollisionDetection(objA, objB);
        }

        if (gjkResult)
        {
            //Debug.Log($"[CollisionDetector] GJK detected collision, running EPA...");

            try
            {
                if (EPA.Expand(objA, objB, simplex, out Vector3 normal, out float depth, out Vector3 contactPoint))
                {
                    Debug.Log($"[CollisionDetector] EPA successful - Normal: {normal}, Depth: {depth:F4}");

                    List<(MassPoint, MassPoint)> pairs = FindAllPenetratingPairs(objA, objB, normal, depth);
                    Debug.Log($"[CollisionDetector] Found {pairs.Count} penetrating pairs");

                    foreach (var (pa, pb) in pairs)
                    {
                        if (pa.sourceName == pb.sourceName) continue;
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
                        });
                    }
                }
                else
                {
                    Debug.LogWarning("[CollisionDetector] EPA failed to expand");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CollisionDetector] EPA failed: {e.Message}");
                Debug.LogError($"[CollisionDetector] Falling back to simple collision detection");
                return SimpleCollisionDetection(objA, objB);
            }
        }
        else
        {
            Debug.Log("[CollisionDetector] GJK detected no collision");
        }

        //Debug.Log($"[CollisionDetector] Final result: {results.Count} collisions found");
        return results;
    }

    // Fallback simple collision detection
    private static List<CollisionResultEnhanced> SimpleCollisionDetection(SpringMassSystem objA, SpringMassSystem objB)
    {
        List<CollisionResultEnhanced> results = new List<CollisionResultEnhanced>();

        //Debug.Log("[CollisionDetector] Using simple collision detection");

        float collisionDistance = 0.2f; // Adjust based on your scale

        foreach (var pointA in objA.allPoints)
        {
            foreach (var pointB in objB.allPoints)
            {
                if (pointA.sourceName == pointB.sourceName) continue;

                Vector3 diff = pointA.position - pointB.position;
                float distance = diff.magnitude;

                if (distance < collisionDistance)
                {
                    //Debug.Log($"[CollisionDetector] Simple collision found! Distance: {distance:F4}, Threshold: {collisionDistance:F4}");

                    Vector3 normal = diff.normalized;
                    float penetration = collisionDistance - distance;
                    Vector3 relativeVel = pointB.velocity - pointA.velocity;
                    float relVelAlongNormal = Vector3.Dot(relativeVel, normal);

                    results.Add(new CollisionResultEnhanced
                    {
                        pointA = pointA,
                        pointB = pointB,
                        normal = normal,
                        penetrationDepth = penetration,
                        contactPoint = 0.5f * (pointA.position + pointB.position),
                        relativeVelocity = relVelAlongNormal,
                        collisionType = DetermineCollisionType(pointA, pointB, relVelAlongNormal),
                        collided = true
                    });
                }
            }
        }

        //Debug.Log($"[CollisionDetector] Simple detection found {results.Count} collisions");
        return results;
    }

    private static List<(MassPoint, MassPoint)> FindAllPenetratingPairs(
        SpringMassSystem a, SpringMassSystem b, Vector3 normal, float penetrationDepth)
    {
        List<(MassPoint, MassPoint)> pairs = new List<(MassPoint, MassPoint)>();
        float epsilon = 1e-9f;
        float maxDistance = penetrationDepth + epsilon;

        //Debug.Log($"[CollisionDetector] Finding penetrating pairs with depth: {penetrationDepth:F4}");

        foreach (var pa in a.allPoints)
        {
            foreach (var pb in b.allPoints)
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

        Debug.Log($"[CollisionDetector] Found {pairs.Count} penetrating pairs");
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