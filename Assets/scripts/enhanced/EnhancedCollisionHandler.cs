using UnityEngine;
using System.Collections.Generic;

// Enhanced collision result structures
public struct CollisionResultEnhanced
{
    public MassPoint pointA;
    public MassPoint pointB;
    public Vector3 normal;
    public float penetrationDepth;
    public Vector3 contactPoint;
    public float relativeVelocity;
    public CollisionType collisionType;
    public bool collided;
}

public struct CollisionResultEnhanced_FEM
{
    public Node pointA;
    public Node pointB;
    public Vector3 normal;
    public float penetrationDepth;
    public Vector3 contactPoint;
    public float relativeVelocity;
    public CollisionType collisionType;
    public bool collided;
}

public enum CollisionType
{
    SpringMass_SpringMass,
    SpringMass_FEM,
    SpringMass_Static,
    FEM_FEM,
    FEM_Static
}

public class EnhancedCollisionHandler : MonoBehaviour
{
    [Header("Physics Parameters")]
    [Range(0, 1)]
    public float globalRestitution = 0.5f;

    [Range(0, 1)]
    public float globalFriction = 0.2f;

    [Range(0, 1)]
    public float dampingFactor = 0.1f;

    [Header("Collision Response")]
    public float collisionResponseStrength = 1.0f;
    public float separationBias = 0.1f;
    public bool usePositionCorrection = true;
    public bool useVelocityCorrection = true;

    [Header("Debugging")]
    public bool showCollisionDebug = true;
    public float debugLineDuration = 0.1f;

    // Handle Spring-Mass to Spring-Mass collisions
    public void HandleSpringMassCollision(CollisionResultEnhanced result)
    {
        if (result.pointA == null || result.pointB == null) return;

        var pointA = result.pointA;
        var pointB = result.pointB;
        var normal = result.normal;
        float penetration = result.penetrationDepth;

        // Skip if both points are pinned
        if (pointA.isPinned && pointB.isPinned) return;

        // Get material properties
        float restitution = GetEffectiveRestitution(pointA.physicalObject, pointB.physicalObject);
        float friction = GetEffectiveFriction(pointA.physicalObject, pointB.physicalObject);

        // 1. Resolve penetration
        if (usePositionCorrection)
        {
            ResolvePenetration(pointA, pointB, normal, penetration);
        }

        // 2. Apply collision impulse
        if (useVelocityCorrection)
        {
            ApplyCollisionImpulse(pointA, pointB, normal, restitution, friction);
        }

        // 3. Apply spring deformation forces
        ApplySpringDeformation(pointA, pointB, result);

        // 4. Debug visualization
        if (showCollisionDebug)
        {
            Debug.DrawLine(pointA.position, pointB.position, Color.red, debugLineDuration);
            Debug.DrawRay(result.contactPoint, normal * penetration, Color.yellow, debugLineDuration);
        }
    }

    // Handle FEM to FEM collisions
    public void HandleFEMCollision(CollisionResultEnhanced_FEM result)
    {
        if (result.pointA == null || result.pointB == null) return;

        var nodeA = result.pointA;
        var nodeB = result.pointB;
        var normal = result.normal;
        float penetration = result.penetrationDepth;

        // Skip if both nodes are fixed
        if (nodeA.isPinned && nodeB.isPinned) return;

        // Get material properties from FEM controllers
        float restitution = GetEffectiveRestitution(nodeA, nodeB);
        float friction = GetEffectiveFriction(nodeA, nodeB);

        // 1. Resolve penetration
        if (usePositionCorrection)
        {
            ResolveFEMPenetration(nodeA, nodeB, normal, penetration);
        }

        // 2. Apply collision impulse
        if (useVelocityCorrection)
        {
            ApplyFEMCollisionImpulse(nodeA, nodeB, normal, restitution, friction);
        }

        // 3. Apply deformation forces
        ApplyFEMDeformation(nodeA, nodeB, result);

        // 4. Debug visualization
        if (showCollisionDebug)
        {
            Debug.DrawLine(nodeA.Position, nodeB.Position, Color.blue, debugLineDuration);
            Debug.DrawRay(result.contactPoint, normal * penetration, Color.cyan, debugLineDuration);
        }
    }

    // Handle mixed Spring-Mass to FEM collisions
    public void HandleMixedCollision(MassPoint massPoint, Node femNode, Vector3 normal, float penetration, Vector3 contactPoint)
    {
        if (massPoint == null || femNode == null) return;
        if (massPoint.isPinned && femNode.isPinned) return;

        float restitution = GetEffectiveRestitution(massPoint.physicalObject, femNode);
        float friction = GetEffectiveFriction(massPoint.physicalObject, femNode);

        // 1. Resolve penetration
        if (usePositionCorrection)
        {
            ResolveMixedPenetration(massPoint, femNode, normal, penetration);
        }

        // 2. Apply collision impulse
        if (useVelocityCorrection)
        {
            ApplyMixedCollisionImpulse(massPoint, femNode, normal, restitution, friction);
        }

        // 3. Debug visualization
        if (showCollisionDebug)
        {
            Debug.DrawLine(massPoint.position, femNode.Position, Color.green, debugLineDuration);
            Debug.DrawRay(contactPoint, normal * penetration, Color.magenta, debugLineDuration);
        }
    }

    #region Spring-Mass Collision Methods

    private void ResolvePenetration(MassPoint pointA, MassPoint pointB, Vector3 normal, float penetration)
    {
        float invMassA = pointA.isPinned ? 0f : 1f / pointA.mass;
        float invMassB = pointB.isPinned ? 0f : 1f / pointB.mass;
        float invMassSum = invMassA + invMassB;

        if (invMassSum == 0) return;

        Vector3 correction = normal * (penetration * separationBias / invMassSum);

        if (!pointA.isPinned)
            pointA.position += correction * invMassA;
        if (!pointB.isPinned)
            pointB.position -= correction * invMassB;
    }

    private void ApplyCollisionImpulse(MassPoint pointA, MassPoint pointB, Vector3 normal, float restitution, float friction)
    {
        Vector3 relativeVelocity = pointB.velocity - pointA.velocity;
        float velAlongNormal = Vector3.Dot(relativeVelocity, normal);

        // Objects moving apart, no impulse needed
        if (velAlongNormal > 0) return;

        float invMassA = pointA.isPinned ? 0f : 1f / pointA.mass;
        float invMassB = pointB.isPinned ? 0f : 1f / pointB.mass;

        // Calculate impulse scalar
        float impulseMagnitude = -(1 + restitution) * velAlongNormal;
        impulseMagnitude /= invMassA + invMassB;
        impulseMagnitude *= collisionResponseStrength;

        Vector3 impulse = impulseMagnitude * normal;

        // Apply normal impulse
        if (!pointA.isPinned)
            pointA.velocity -= invMassA * impulse;
        if (!pointB.isPinned)
            pointB.velocity += invMassB * impulse;

        // Apply friction impulse
        ApplyFrictionImpulse(pointA, pointB, relativeVelocity, normal, friction, impulseMagnitude, invMassA, invMassB);
    }

    private void ApplyFrictionImpulse(MassPoint pointA, MassPoint pointB, Vector3 relativeVelocity, Vector3 normal,
                                    float friction, float normalImpulse, float invMassA, float invMassB)
    {
        Vector3 tangent = relativeVelocity - Vector3.Dot(relativeVelocity, normal) * normal;
        if (tangent.sqrMagnitude < 0.001f) return;

        tangent.Normalize();

        float frictionImpulseMag = -Vector3.Dot(relativeVelocity, tangent);
        frictionImpulseMag /= invMassA + invMassB;
        frictionImpulseMag = Mathf.Clamp(frictionImpulseMag, -normalImpulse * friction, normalImpulse * friction);

        Vector3 frictionImpulse = frictionImpulseMag * tangent;

        if (!pointA.isPinned)
            pointA.velocity -= invMassA * frictionImpulse;
        if (!pointB.isPinned)
            pointB.velocity += invMassB * frictionImpulse;
    }

    private void ApplySpringDeformation(MassPoint pointA, MassPoint pointB, CollisionResultEnhanced result)
    {
        // Apply additional spring forces based on collision
        float deformationForce = result.penetrationDepth * collisionResponseStrength;
        Vector3 force = result.normal * deformationForce;

        if (!pointA.isPinned)
            pointA.ApplyForce(force, Time.fixedDeltaTime);
        if (!pointB.isPinned)
            pointB.ApplyForce(-force, Time.fixedDeltaTime);

        // Apply damping to reduce oscillations
        Vector3 dampingForce = (pointB.velocity - pointA.velocity) * dampingFactor;

        if (!pointA.isPinned)
            pointA.ApplyForce(dampingForce, Time.fixedDeltaTime);
        if (!pointB.isPinned)
            pointB.ApplyForce(-dampingForce, Time.fixedDeltaTime);
    }

    #endregion

    #region FEM Collision Methods

    private void ResolveFEMPenetration(Node nodeA, Node nodeB, Vector3 normal, float penetration)
    {
        float invMassA = nodeA.isPinned ? 0f : 1f / nodeA.Mass;
        float invMassB = nodeB.isPinned ? 0f : 1f / nodeB.Mass;
        float invMassSum = invMassA + invMassB;

        if (invMassSum == 0) return;

        Vector3 correction = normal * (penetration * separationBias / invMassSum);

        if (!nodeA.isPinned)
            nodeA.Position += correction * invMassA;
        if (!nodeB.isPinned)
            nodeB.Position -= correction * invMassB;
    }

    private void ApplyFEMCollisionImpulse(Node nodeA, Node nodeB, Vector3 normal, float restitution, float friction)
    {
        Vector3 relativeVelocity = nodeB.Velocity - nodeA.Velocity;
        float velAlongNormal = Vector3.Dot(relativeVelocity, normal);

        if (velAlongNormal > 0) return;

        float invMassA = nodeA.isPinned ? 0f : 1f / nodeA.Mass;
        float invMassB = nodeB.isPinned ? 0f : 1f / nodeB.Mass;

        float impulseMagnitude = -(1 + restitution) * velAlongNormal;
        impulseMagnitude /= invMassA + invMassB;
        impulseMagnitude *= collisionResponseStrength;

        Vector3 impulse = impulseMagnitude * normal;

        if (!nodeA.isPinned)
            nodeA.Velocity -= invMassA * impulse;
        if (!nodeB.isPinned)
            nodeB.Velocity += invMassB * impulse;

        // Apply friction
        ApplyFEMFrictionImpulse(nodeA, nodeB, relativeVelocity, normal, friction, impulseMagnitude, invMassA, invMassB);
    }

    private void ApplyFEMFrictionImpulse(Node nodeA, Node nodeB, Vector3 relativeVelocity, Vector3 normal,
                                       float friction, float normalImpulse, float invMassA, float invMassB)
    {
        Vector3 tangent = relativeVelocity - Vector3.Dot(relativeVelocity, normal) * normal;
        if (tangent.sqrMagnitude < 0.001f) return;

        tangent.Normalize();

        float frictionImpulseMag = -Vector3.Dot(relativeVelocity, tangent);
        frictionImpulseMag /= invMassA + invMassB;
        frictionImpulseMag = Mathf.Clamp(frictionImpulseMag, -normalImpulse * friction, normalImpulse * friction);

        Vector3 frictionImpulse = frictionImpulseMag * tangent;

        if (!nodeA.isPinned)
            nodeA.Velocity -= invMassA * frictionImpulse;
        if (!nodeB.isPinned)
            nodeB.Velocity += invMassB * frictionImpulse;
    }

    private void ApplyFEMDeformation(Node nodeA, Node nodeB, CollisionResultEnhanced_FEM result)
    {
        // Apply deformation forces based on collision
        float deformationForce = result.penetrationDepth * collisionResponseStrength;
        Vector3 force = result.normal * deformationForce;

        if (!nodeA.isPinned)
            nodeA.ApplyForce(force, Time.fixedDeltaTime);
        if (!nodeB.isPinned)
            nodeB.ApplyForce(-force, Time.fixedDeltaTime);

        // Apply damping
        Vector3 dampingForce = (nodeB.Velocity - nodeA.Velocity) * dampingFactor;

        if (!nodeA.isPinned)
            nodeA.ApplyForce(dampingForce, Time.fixedDeltaTime);
        if (!nodeB.isPinned)
            nodeB.ApplyForce(-dampingForce, Time.fixedDeltaTime);
    }

    #endregion

    #region Mixed Collision Methods

    private void ResolveMixedPenetration(MassPoint massPoint, Node femNode, Vector3 normal, float penetration)
    {
        float invMassA = massPoint.isPinned ? 0f : 1f / massPoint.mass;
        float invMassB = femNode.isPinned ? 0f : 1f / femNode.Mass;
        float invMassSum = invMassA + invMassB;

        if (invMassSum == 0) return;

        Vector3 correction = normal * (penetration * separationBias / invMassSum);

        if (!massPoint.isPinned)
            massPoint.position += correction * invMassA;
        if (!femNode.isPinned)
            femNode.Position -= correction * invMassB;
    }

    private void ApplyMixedCollisionImpulse(MassPoint massPoint, Node femNode, Vector3 normal, float restitution, float friction)
    {
        Vector3 relativeVelocity = femNode.Velocity - massPoint.velocity;
        float velAlongNormal = Vector3.Dot(relativeVelocity, normal);

        if (velAlongNormal > 0) return;

        float invMassA = massPoint.isPinned ? 0f : 1f / massPoint.mass;
        float invMassB = femNode.isPinned ? 0f : 1f / femNode.Mass;

        float impulseMagnitude = -(1 + restitution) * velAlongNormal;
        impulseMagnitude /= invMassA + invMassB;
        impulseMagnitude *= collisionResponseStrength;

        Vector3 impulse = impulseMagnitude * normal;

        if (!massPoint.isPinned)
            massPoint.velocity -= invMassA * impulse;
        if (!femNode.isPinned)
            femNode.Velocity += invMassB * impulse;

        // Apply friction
        Vector3 tangent = relativeVelocity - Vector3.Dot(relativeVelocity, normal) * normal;
        if (tangent.sqrMagnitude > 0.001f)
        {
            tangent.Normalize();
            float frictionImpulseMag = -Vector3.Dot(relativeVelocity, tangent);
            frictionImpulseMag /= invMassA + invMassB;
            frictionImpulseMag = Mathf.Clamp(frictionImpulseMag, -impulseMagnitude * friction, impulseMagnitude * friction);

            Vector3 frictionImpulse = frictionImpulseMag * tangent;

            if (!massPoint.isPinned)
                massPoint.velocity -= invMassA * frictionImpulse;
            if (!femNode.isPinned)
                femNode.Velocity += invMassB * frictionImpulse;
        }
    }

    #endregion

    #region Material Properties

    private float GetEffectiveRestitution(PhysicalObject objA, PhysicalObject objB)
    {
        if (objA == null || objB == null) return globalRestitution;
        return (objA.Bounciness + objB.Bounciness) * 0.5f;
    }

    private float GetEffectiveFriction(PhysicalObject objA, PhysicalObject objB)
    {
        if (objA == null || objB == null) return globalFriction;
        // Combine friction coefficients - could use different methods
        return Mathf.Sqrt(objA.Damping * objB.Damping);
    }

    private float GetEffectiveRestitution(PhysicalObject obj, Node node)
    {
        if (obj == null) return globalRestitution;
        return obj.Bounciness;
    }

    private float GetEffectiveFriction(PhysicalObject obj, Node node)
    {
        if (obj == null) return globalFriction;
        return obj.Damping;
    }

    private float GetEffectiveRestitution(Node nodeA, Node nodeB)
    {
        // You might want to add material properties to FEMNode
        return globalRestitution;
    }

    private float GetEffectiveFriction(Node nodeA, Node nodeB)
    {
        // You might want to add material properties to Node
        return globalFriction;
    }

    #endregion

    #region Public Interface

    public void HandleCollision(CollisionResultEnhanced result)
    {
        HandleSpringMassCollision(result);
    }

    public void HandleCollision(CollisionResultEnhanced_FEM result)
    {
        HandleFEMCollision(result);
    }

    public void HandleCollision(MassPoint massPoint, Node femNode, Vector3 normal, float penetration, Vector3 contactPoint)
    {
        HandleMixedCollision(massPoint, femNode, normal, penetration, contactPoint);
    }

    #endregion
}