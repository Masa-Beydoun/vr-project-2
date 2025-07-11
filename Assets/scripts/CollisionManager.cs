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

        foreach (var (a, b) in candidatePairs)
        {
            SpringMassSystem aSystem = a.GetComponentInParent<SpringMassSystem>();
            SpringMassSystem bSystem = b.GetComponentInParent<SpringMassSystem>();

            var result = CollisionDetector.CheckCollision(aSystem, bSystem);
            if (!result.collided)
                continue;

            // Fix normal direction: always point from B to A
            result.normal = (result.pointA.position - result.pointB.position).normalized;

            //// Optional: skip if moving apart
            //Vector3 relativeVelocity = result.pointA.velocity - result.pointB.velocity;
            //float velAlongNormal = Vector3.Dot(relativeVelocity, result.normal);
            //if (velAlongNormal < 0)
            //{
            //    // They're separating — no need to handle
            //    continue;
            //}

            // Logging
            Debug.Log($"Contact Point: {result.contactPoint}");
            Debug.Log($"Normal: {result.normal}, Depth: {result.penetrationDepth}");
            Debug.Log($"Colliding Points: {result.pointA.sourceName}[{result.pointA.id}] <-> {result.pointB.sourceName}[{result.pointB.id}]");
            Debug.DrawLine(result.pointA.position, result.pointB.position, Color.green, 5f);

            // Call handler
            if (collisionHandler != null)
                collisionHandler.HandleCollision(result);
        }
    }

}
