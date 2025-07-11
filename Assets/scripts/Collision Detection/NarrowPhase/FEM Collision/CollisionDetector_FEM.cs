using System.Collections.Generic;
using UnityEngine;

public struct CollisionResult_FEM
{
    public bool collided;
    public Vector3 normal;
    public float penetrationDepth;
    public Vector3 contactPoint;
    public Node pointA;
    public Node pointB;
}

public static class CollisionDetector_FEM
{
    public static List<CollisionResultEnhanced_FEM> CheckCollision(FEMController objA, FEMController objB)
    {
        List<CollisionResultEnhanced_FEM> results = new List<CollisionResultEnhanced_FEM>();
        List<Vector3> simplex = new List<Vector3>();

        if (GJK_FEM.Detect(objA, objB, simplex))
        {
            if (EPA_FEM.Expand(objA, objB, simplex, out Vector3 normal, out float depth, out Vector3 contactPoint))
            {
                List<(Node, Node)> pairs = FindAllPenetratingPairsFEM(objA, objB, normal, depth);

                foreach (var (pa, pb) in pairs)
                {
                    Vector3 relativeVelocity = pb.Velocity - pa.Velocity;
                    float relVelAlongNormal = Vector3.Dot(relativeVelocity, normal);
                    results.Add(new CollisionResultEnhanced_FEM
                    {
                        pointA = pa,
                        pointB = pb,
                        normal = normal,
                        penetrationDepth = depth,
                        contactPoint = 0.5f * (pa.Position + pb.Position),
                        relativeVelocity = relVelAlongNormal,
                        collisionType = CollisionType.FEM_FEM,
                        collided = true
                    });
                }
            }
        }

        return results;
    }

    private static List<(Node, Node)> FindAllPenetratingPairsFEM(FEMController a, FEMController b, Vector3 normal, float penetrationDepth)
    {
        List<(Node, Node)> pairs = new List<(Node, Node)>();
        float epsilon = 1e-9f;
        float maxDistanceSq = (penetrationDepth + epsilon) * (penetrationDepth + epsilon);

        foreach (var pa in a.GetAllNodes())
        {
            foreach (var pb in b.GetAllNodes())
            {
                Vector3 diff = pa.Position - pb.Position;
                float proj = Vector3.Dot(diff, normal);

                if (proj > -epsilon && proj <= penetrationDepth + epsilon)
                {
                    float distanceSq = diff.sqrMagnitude;

                    if (distanceSq <= maxDistanceSq)
                    {
                        pairs.Add((pa, pb));
                    }
                }
            }
        }

        return pairs;
    }

}
