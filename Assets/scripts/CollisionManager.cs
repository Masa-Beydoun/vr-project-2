using System;
using System.Collections.Generic;
using UnityEngine;

public class CollisionManager : MonoBehaviour
{
    private PhysicalObject[] physicalObjects;
    public const BroadPhaseMethod broadPhaseMethod = BroadPhaseMethod.SweepAndPrune;
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

        //BroadPhaseMethod broadPhaseMethod = BroadPhaseMethod.SweepAndPrune;
        //broadPhase = new SweepAndPrune();
        //float start = Time.realtimeSinceStartup;
        //var candidatePairs = broadPhase.GetCollisionPairs(physicalObjects);
        //float end = Time.realtimeSinceStartup;
        //Debug.Log($"[{broadPhaseMethod}] Time: {(end - start) * 1000f} ms - Pairs: {candidatePairs.Count}");

        //broadPhaseMethod = BroadPhaseMethod.UniformGrid;
        //float cellSize = 3.0f;
        //broadPhase = new UniformGrid(cellSize: cellSize);
        //start = Time.realtimeSinceStartup;
        //candidatePairs = broadPhase.GetCollisionPairs(physicalObjects);
        //end = Time.realtimeSinceStartup;
        //Debug.Log($"[{broadPhaseMethod}] Time: {(end - start) * 1000f} ms - Pairs: {candidatePairs.Count}");

        //broadPhaseMethod = BroadPhaseMethod.Octree;
        //broadPhase = new Octree(new Bounds(Vector3.zero, Vector3.one * 10000));
        //start = Time.realtimeSinceStartup;
        //candidatePairs = broadPhase.GetCollisionPairs(physicalObjects);
        //end = Time.realtimeSinceStartup;
        //Debug.Log($"[{broadPhaseMethod}] Time: {(end - start) * 1000f} ms - Pairs: {candidatePairs.Count}");

        var candidatePairs = broadPhase.GetCollisionPairs(physicalObjects);
        List<(PhysicalObject, PhysicalObject,Vector3)> realCollidedPairs = new List<(PhysicalObject, PhysicalObject, Vector3)>();
        foreach (var (a, b) in candidatePairs)
        {
            Vector3 mtv = Vector3.zero;
            if (IsColliding(a, b, out mtv))
            {
                Debug.Log($"Collision Detected between {a.name} and {b.name} mtv = {mtv}");
                realCollidedPairs.Add((a, b, mtv));
                ResolveCollision(a, b);
            }
        }
    }

    static bool IsColliding(PhysicalObject a, PhysicalObject b, out Vector3 mtv)
    {
<<<<<<< HEAD
        if (a.shapeType == ShapeType.Sphere && b.shapeType == ShapeType.Sphere)
        {
            float distance = Vector3.Distance(a.transform.position, b.transform.position);
            return distance < (a.radius + b.radius);
        }

        if (a.shapeType == ShapeType.AABB && b.shapeType == ShapeType.AABB)
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
        if (a.shapeType == ShapeType.Sphere && b.shapeType == ShapeType.AABB)
        {
            return SphereAABBCollision(a, b);
        }

        if (a.shapeType == ShapeType.AABB && b.shapeType == ShapeType.Sphere)
        {
            return SphereAABBCollision(b, a); 
=======
        mtv = Vector3.zero;
        bool collided = false;

        if (a.shape == PhysicalObject.ShapeType.Sphere && b.shape == PhysicalObject.ShapeType.Sphere)
            collided = SAT.SphereSphereCollisionWithMTV(a, b, out mtv);
        else if (a.shape == PhysicalObject.ShapeType.AABB && b.shape == PhysicalObject.ShapeType.AABB)
            collided = SAT.AABBAABBCollisionWithMTV(a, b, out mtv);
        else if (a.shape == PhysicalObject.ShapeType.Sphere && b.shape == PhysicalObject.ShapeType.AABB)
            collided = SAT.SphereOBBCollisionWithMTV(a, b, out mtv);
        else if (a.shape == PhysicalObject.ShapeType.AABB && b.shape == PhysicalObject.ShapeType.Sphere)
            collided = SAT.SphereOBBCollisionWithMTV(b, a, out mtv);
        else if (a.shape == PhysicalObject.ShapeType.AABB && b.shape == PhysicalObject.ShapeType.AABB)
            collided = SAT.OBBOBBCollisionWithMTV(a, b, out mtv);

        if (collided)
        {
            if (!a.isStatic) a.transform.position -= mtv * 0.5f;
            if (!b.isStatic) b.transform.position += mtv * 0.5f;
            return true;
>>>>>>> 0e1295122ebe31296514cedd4ab4eea4a889611c
        }

        return false;

    }

    //bool IsColliding(PhysicalObject a, PhysicalObject b)
    //{
    //    if (a.shape == PhysicalObject.ShapeType.Sphere && b.shape == PhysicalObject.ShapeType.Sphere)
    //    {
    //        float distance = Vector3.Distance(a.transform.position, b.transform.position);
    //        return distance < (a.radius + b.radius);
    //    }

    //    if (a.shape == PhysicalObject.ShapeType.AABB && b.shape == PhysicalObject.ShapeType.AABB)
    //    {
    //        Vector3 aHalfSize = a.transform.localScale / 2;
    //        Vector3 bHalfSize = b.transform.localScale / 2;

    //        Vector3 aMin = a.transform.position - aHalfSize;
    //        Vector3 aMax = a.transform.position + aHalfSize;
    //        Vector3 bMin = b.transform.position - bHalfSize;
    //        Vector3 bMax = b.transform.position + bHalfSize;

    //        return (aMin.x <= bMax.x && aMax.x >= bMin.x) &&
    //               (aMin.y <= bMax.y && aMax.y >= bMin.y) &&
    //               (aMin.z <= bMax.z && aMax.z >= bMin.z);
    //    }

    //    // Sphere vs AABB
    //    if (a.shape == PhysicalObject.ShapeType.Sphere && b.shape == PhysicalObject.ShapeType.AABB)
    //    {
    //        return SphereAABBCollision(a, b);
    //    }

    //    if (a.shape == PhysicalObject.ShapeType.AABB && b.shape == PhysicalObject.ShapeType.Sphere)
    //    {
    //        return SphereAABBCollision(b, a);
    //    }

    //    return false;
    //}

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

<<<<<<< HEAD
        
        if (a.shapeType == ShapeType.Sphere && b.shapeType == ShapeType.Sphere)
=======

        if (a.shape == PhysicalObject.ShapeType.Sphere && b.shape == PhysicalObject.ShapeType.Sphere)
>>>>>>> 0e1295122ebe31296514cedd4ab4eea4a889611c
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
