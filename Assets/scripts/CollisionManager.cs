using System;
using System.Collections.Generic;
using UnityEngine;

public class CollisionManager : MonoBehaviour
{
    private PhysicalObject[] physicalObjects;
    public const BroadPhaseMethod broadPhaseMethod = BroadPhaseMethod.SweepAndPrune;
    IBroadPhase broadPhase;
    [SerializeField] private CollisionHandler collisionHandler;

    void Start()
    {
        switch (broadPhaseMethod)
        {
            case BroadPhaseMethod.SweepAndPrune:
                broadPhase = new SweepAndPrune();
                break;
            case BroadPhaseMethod.UniformGrid:
                float cellSize = 1.0f;
                broadPhase = new UniformGrid(cellSize: cellSize);
                break;
            case BroadPhaseMethod.Octree:
                broadPhase = new Octree(new Bounds(Vector3.zero, Vector3.one * 10000));
                break;
        }
    }

    void FixedUpdate()
    {
        physicalObjects = FindObjectsOfType<PhysicalObject>();

        var candidatePairs = broadPhase.GetCollisionPairs(physicalObjects);

        List<(PhysicalObject, PhysicalObject, Vector3)> realCollidedPairs = new List<(PhysicalObject, PhysicalObject, Vector3)>();
        foreach (var (a, b) in candidatePairs)
        {
            Vector3 mtv = Vector3.zero;
            if (IsColliding(a, b, out mtv))
            {
                bool isMassPointA = a.GetComponent<MassPointController>() != null;
                bool isMassPointB = b.GetComponent<MassPointController>() != null;

                MassPoint m1 = isMassPointA ? a.GetComponent<MassPointController>()?.point : null;
                MassPoint m2 = isMassPointB ? b.GetComponent<MassPointController>()?.point : null;

                if (isMassPointA && isMassPointB)
                {
                    // Same source skip (optional)
                    if (m1 != null && m2 != null && m1.sourceName == m2.sourceName)
                        return;

                    Debug.Log(
                        $"[MassPoint vs MassPoint]\n" +
                        $" - MassPoint 1: ID = {m1?.GetHashCode()}, Source = {m1?.sourceName}, Pos = {m1?.position}\n" +
                        $" - MassPoint 2: ID = {m2?.GetHashCode()}, Source = {m2?.sourceName}, Pos = {m2?.position}\n" +
                        $" - MTV: {mtv}"
                    );
                }
                else if (isMassPointA && !isMassPointB)
                {
                    Debug.Log(
                        $"[MassPoint vs Object]\n" +
                        $" - MassPoint: ID = {m1?.GetHashCode()}, Source = {m1?.sourceName}, Pos = {m1?.position}\n" +
                        $" - Other Object: {b.name}\n" +
                        $" - MTV: {mtv}"
                    );
                }
                else if (!isMassPointA && isMassPointB)
                {
                    Debug.Log(
                        $"[Object vs MassPoint]\n" +
                        $" - MassPoint: ID = {m2?.GetHashCode()}, Source = {m2?.sourceName}, Pos = {m2?.position}\n" +
                        $" - Other Object: {a.name}\n" +
                        $" - MTV: {mtv}"
                    );
                }
                else
                {
                    Debug.Log($"[Object vs Object] Collision Detected between {a.name} and {b.name} with MTV: {mtv}");
                }

            }
        }
    }

    static bool IsColliding(PhysicalObject a, PhysicalObject b, out Vector3 mtv)
    {
        mtv = Vector3.zero;
        bool collided = false;

        if (a.shapeType == BoundingShapeType.Sphere && b.shapeType == BoundingShapeType.Sphere)
            collided = SAT.SphereSphereCollisionWithMTV(a, b, out mtv);
        else if (a.shapeType == BoundingShapeType.AABB && b.shapeType == BoundingShapeType.AABB)
            collided = SAT.AABBAABBCollisionWithMTV(a, b, out mtv);
        else if (a.shapeType == BoundingShapeType.Sphere && b.shapeType == BoundingShapeType.AABB)
            collided = SAT.SphereOBBCollisionWithMTV(a, b, out mtv);
        else if (a.shapeType == BoundingShapeType.AABB && b.shapeType == BoundingShapeType.Sphere)
            collided = SAT.SphereOBBCollisionWithMTV(b, a, out mtv);
        else if (a.shapeType == BoundingShapeType.AABB && b.shapeType == BoundingShapeType.AABB)
            collided = SAT.OBBOBBCollisionWithMTV(a, b, out mtv);

        if (collided)
        {
            if (!a.isStatic) a.transform.position -= mtv * 0.5f;
            if (!b.isStatic) b.transform.position += mtv * 0.5f;
            return true;
        }

        return false;
    }

    void ResolveCollision(PhysicalObject a, PhysicalObject b)
    {
        if (a.isStatic && b.isStatic) return;

        Vector3 normal = (b.transform.position - a.transform.position).normalized;
        float relativeVelocity = Vector3.Dot(b.velocity - a.velocity, normal);

        float restitution = 1f;

        float invMassA = a.isStatic ? 0f : 1f / a.mass;
        float invMassB = b.isStatic ? 0f : 1f / b.mass;

        float impulseScalar = -(1f + restitution) * relativeVelocity / (invMassA + invMassB);
        Vector3 impulse = impulseScalar * normal;

        if (!a.isStatic)
            a.velocity -= impulse * invMassA;

        if (!b.isStatic)
            b.velocity += impulse * invMassB;

        if (a.shapeType == BoundingShapeType.Sphere && b.shapeType == BoundingShapeType.Sphere)
        {
            float penetrationDepth = (a.radius + b.radius) - Vector3.Distance(a.transform.position, b.transform.position);
            Vector3 correction = normal * penetrationDepth / (invMassA + invMassB);

            if (!a.isStatic)
                a.transform.position -= correction * invMassA;

            if (!b.isStatic)
                b.transform.position += correction * invMassB;
        }

        if (!a.isStatic)
        {
            a.velocity = Vector3.zero;
            a.isStatic = true;
        }

        if (!b.isStatic)
        {
            b.velocity = Vector3.zero;
            b.isStatic = true;
        }
    }
}
