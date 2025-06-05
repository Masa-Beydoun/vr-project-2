using System;
using System.Collections.Generic;
using UnityEngine;

public class CollisionManager : MonoBehaviour
{
    private PhysicalObject[] physicalObjects;
    public const BroadPhaseMethod broadPhaseMethod = BroadPhaseMethod.Octree;
    IBroadPhase broadPhase;

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

        float start = Time.realtimeSinceStartup;
        var candidatePairs = broadPhase.GetCollisionPairs(physicalObjects);
        float end = Time.realtimeSinceStartup;
        Debug.Log($"[{broadPhaseMethod}] Time: {(end - start) * 1000f} ms - Pairs: {candidatePairs.Count}");

        foreach (var (a, b) in candidatePairs)
        {
            if (IsColliding(a, b))
            {
                ResolveCollision(a, b);
            }
        }
    }


    bool IsColliding(PhysicalObject a, PhysicalObject b)
    {
        if (a.shape == PhysicalObject.ShapeType.Sphere && b.shape == PhysicalObject.ShapeType.Sphere)
        {
            float distance = Vector3.Distance(a.transform.position, b.transform.position);
            return distance < (a.radius + b.radius);
        }

        if (a.shape == PhysicalObject.ShapeType.AABB && b.shape == PhysicalObject.ShapeType.AABB)
        {
            Vector3 aHalfSize = a.transform.localScale / 2;
            Vector3 bHalfSize = b.transform.localScale / 2;

            Vector3 aMin = a.transform.position - aHalfSize;
            Vector3 aMax = a.transform.position + aHalfSize;
            Vector3 bMin = b.transform.position - bHalfSize;
            Vector3 bMax = b.transform.position + bHalfSize;

            return (aMin.x <= bMax.x && aMax.x >= bMin.x) &&
                   (aMin.y <= bMax.y && aMax.y >= bMin.y) &&
                   (aMin.z <= bMax.z && aMax.z >= bMin.z);
        }

        // Sphere vs AABB
        if (a.shape == PhysicalObject.ShapeType.Sphere && b.shape == PhysicalObject.ShapeType.AABB)
        {
            return SphereAABBCollision(a, b);
        }

        if (a.shape == PhysicalObject.ShapeType.AABB && b.shape == PhysicalObject.ShapeType.Sphere)
        {
            return SphereAABBCollision(b, a);
        }

        return false;
    }

    bool SphereAABBCollision(PhysicalObject sphere, PhysicalObject aabb)
    {
        Vector3 boxHalfSize = aabb.transform.localScale / 2;
        Vector3 boxMin = aabb.transform.position - boxHalfSize;
        Vector3 boxMax = aabb.transform.position + boxHalfSize;
        Vector3 sphereCenter = sphere.transform.position;


        float x = Mathf.Max(boxMin.x, Mathf.Min(sphereCenter.x, boxMax.x));
        float y = Mathf.Max(boxMin.y, Mathf.Min(sphereCenter.y, boxMax.y));
        float z = Mathf.Max(boxMin.z, Mathf.Min(sphereCenter.z, boxMax.z));

        Vector3 closestPoint = new Vector3(x, y, z);
        float distanceSquared = (closestPoint - sphereCenter).sqrMagnitude;

        return distanceSquared < (sphere.radius * sphere.radius);
    }

    void ResolveCollision(PhysicalObject a, PhysicalObject b)
    {
        if (a.isStatic && b.isStatic) return;

        Vector3 normal = (b.transform.position - a.transform.position).normalized;

        float relativeVelocity = Vector3.Dot(b.velocity - a.velocity, normal);
        //if (relativeVelocity > 0.01f) return; 


        float restitution = 1f;

        float invMassA = a.isStatic ? 0f : 1f / a.mass;
        float invMassB = b.isStatic ? 0f : 1f / b.mass;

        float impulseScalar = -(1f + restitution) * relativeVelocity / (invMassA + invMassB);
        Vector3 impulse = impulseScalar * normal;

        if (!a.isStatic)
            a.velocity -= impulse * invMassA;

        if (!b.isStatic)
            b.velocity += impulse * invMassB;


        if (a.shape == PhysicalObject.ShapeType.Sphere && b.shape == PhysicalObject.ShapeType.Sphere)
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
            //Debug.Log($"{a.name} has been stopped and marked static");
        }


        if (!b.isStatic)
        {
            b.velocity = Vector3.zero;
            b.isStatic = true;
            //Debug.Log($"{a.name} has been stopped and marked static");
        }

    }

    

}
