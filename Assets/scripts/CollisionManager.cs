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
        //physicalObjects = FindObjectsOfType<PhysicalObject>();

        var massPointControllers = FindObjectsOfType<MassPointController>();
        List<PhysicalObject> massPointObjects = new List<PhysicalObject>();

        foreach (var controller in massPointControllers)
        {
            var obj = controller.GetComponent<PhysicalObject>();
            if (obj != null)
                massPointObjects.Add(obj); // Only mass points
        }



        var candidatePairs = broadPhase.GetCollisionPairs(massPointObjects.ToArray());

        List<(PhysicalObject, PhysicalObject,Vector3)> realCollidedPairs = new List<(PhysicalObject, PhysicalObject, Vector3)>();
        foreach (var (a, b) in candidatePairs)
        {
            Vector3 mtv = Vector3.zero;
            if (IsColliding(a, b, out mtv))
            {
                MassPoint m1 = a.GetComponent<MassPointController>()?.point;
                MassPoint m2 = b.GetComponent<MassPointController>()?.point;
                //if (mtv.magnitude < 0.01f) // adjust as needed
                //    continue;
                if (m1 != null && m2 != null)
                {
                    //Debug.Log($"Checking {a.name} (Shape={a.shapeType}) vs {b.name} (Shape={b.shapeType})");

                    if (m1.sourceName == m2.sourceName) continue;
                    Debug.Log(
                        $"MassPoint 1  ID: {m1.GetHashCode()}, Source: {m1.sourceName}, Pos: {m1.position}\n" +
                        $"MassPoint 2  ID: {m2.GetHashCode()}, Source: {m2.sourceName}, Pos: {m2.position}\n" +
                        $"MTV: {mtv}"
                    );
                    //Debug.Log($"Checking collision between {a.name} ({a.shapeType}) and {b.name} ({b.shapeType})");

                }
                //if (a.name.Contains("SpringLine") || b.name.Contains("SpringLine"))
                //    continue;

                //Debug.Log($"Collision Detected between {a.name} and {b.name} mtv = {mtv}");
                //ResolveCollision(a, b); // fallback for rigid body 
            }
        }
    }

    static bool IsColliding(PhysicalObject a, PhysicalObject b, out Vector3 mtv)
    {   mtv = Vector3.zero;
        bool collided = false;

        if (a.shapeType == ShapeType.Sphere && b.shapeType == ShapeType.Sphere)
            collided = SAT.SphereSphereCollisionWithMTV(a, b, out mtv);
        else if (a.shapeType == ShapeType.AABB && b.shapeType == ShapeType.AABB)
            collided = SAT.AABBAABBCollisionWithMTV(a, b, out mtv);
        else if (a.shapeType == ShapeType.Sphere && b.shapeType == ShapeType.AABB)
            collided = SAT.SphereOBBCollisionWithMTV(a, b, out mtv);
        else if (a.shapeType == ShapeType.AABB && b.shapeType == ShapeType.Sphere)
            collided = SAT.SphereOBBCollisionWithMTV(b, a, out mtv);
        else if (a.shapeType == ShapeType.AABB && b.shapeType == ShapeType.AABB)
            collided = SAT.OBBOBBCollisionWithMTV(a, b, out mtv);

        if (collided)
        {
            if (!a.isStatic) a.transform.position -= mtv * 0.5f;
            if (!b.isStatic) b.transform.position += mtv * 0.5f;
            return true;
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

        
        if (a.shapeType == ShapeType.Sphere && b.shapeType == ShapeType.Sphere)
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
