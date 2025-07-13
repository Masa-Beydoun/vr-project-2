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
    public float globalRestitution = 0.1f;

    [Range(0, 1)]
    public float globalFriction = 0.5f;

    [Range(0, 1)]
    public float dampingFactor = 0.3f;

    [Header("Collision Response")]
    public float collisionResponseStrength = 0.4f; // Much more aggressive
    public float separationBias = 0.4f; // Very aggressive separation
    public bool usePositionCorrection = true;
    public bool useVelocityCorrection = true;

    [Header("Debugging")]
    public bool showCollisionDebug = true;
    public float debugLineDuration = 0.1f;

    [Header("Advanced Settings")]
    public float maxCollisionForce = 30f; // Much higher limit
    public float minPenetrationForResponse = 0.01f; // Very low threshold
    public float maxPenetrationDepth = 2.0f; // NEW: Cap maximum penetration depth
    public float staticObjectMass = 1000f; // NEW: Mass for static objects
    public float emergencyPenetrationThreshold = 2.0f; // NEW: For emergency corrections
    public float emergencyForceMultiplier = 2f; // NEW: Extra force for deep penetrations

    // Handle Spring-Mass to Spring-Mass collisions
    public void HandleSpringMassCollision(CollisionResultEnhanced result)
    {
        if (result.pointA == null || result.pointB == null) return;

        var pointA = result.pointA;
        var pointB = result.pointB;
        var normal = result.normal;
        float penetration = Mathf.Min(result.penetrationDepth, maxPenetrationDepth); // CAP PENETRATION

        // Skip if both points are pinned
        if (pointA.isPinned && pointB.isPinned) return;

        // Skip if penetration is too small
        if (penetration < minPenetrationForResponse) return;

        // Get material properties
        float restitution = GetEffectiveRestitution(pointA.physicalObject, pointB.physicalObject);
        float friction = GetEffectiveFriction(pointA.physicalObject, pointB.physicalObject);

        // Check if either object is static/ground
        bool isAStatic = IsStaticObject(pointA);
        bool isBStatic = IsStaticObject(pointB);

        // 1. Resolve penetration FIRST and more aggressively
        if (usePositionCorrection)
        {
            ResolvePenetrationWithStatic(pointA, pointB, normal, penetration, isAStatic, isBStatic);
        }

        // 2. Apply collision impulse
        if (useVelocityCorrection)
        {
            ApplyCollisionImpulseWithStatic(pointA, pointB, normal, restitution, friction, isAStatic, isBStatic);
        }

        // 3. Apply spring deformation forces
        ApplyGentleSpringDeformation(pointA, pointB, result, isAStatic, isBStatic);
        
        // 4. Debug visualization
        if (showCollisionDebug)
        {
            Debug.DrawLine(pointA.position, pointB.position, Color.red, debugLineDuration);
            Debug.DrawRay(result.contactPoint, normal * penetration, Color.yellow, debugLineDuration);
        }
    }
    private bool IsStaticObject(MassPoint point)
    {
        // Check if the object is marked as static
        if (point.physicalObject != null && point.physicalObject.isStatic)
            return true;

        // Check if the point is pinned
        if (point.isPinned)
            return true;

        // Check if the object name suggests it's ground/static
        string objectName = point.sourceName.ToLower();
        return objectName.Contains("ground") || objectName.Contains("floor") || objectName.Contains("static");
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

    private void ResolvePenetrationWithStatic(MassPoint pointA, MassPoint pointB, Vector3 normal, float penetration, bool isAStatic, bool isBStatic)
    {
        if (isAStatic && isBStatic) return;

        float invMassA = (pointA.isPinned || isAStatic) ? 0f : 1f / pointA.mass;
        float invMassB = (pointB.isPinned || isBStatic) ? 0f : 1f / pointB.mass;
        float invMassSum = invMassA + invMassB;

        if (invMassSum == 0) return;

        // MUCH MORE AGGRESSIVE position correction
        Vector3 correction = normal * (penetration * separationBias / invMassSum);

        // Add extra correction for deep penetrations
        if (penetration > emergencyPenetrationThreshold)
        {
            correction *= emergencyForceMultiplier;
        }

        // Apply position correction only to non-static objects
        if (!pointA.isPinned && !isAStatic)
        {
            pointA.position += correction * invMassA;
        }
        if (!pointB.isPinned && !isBStatic)
        {
            pointB.position -= correction * invMassB;
        }

        // STRONGER velocity correction
        Vector3 relativeVelocity = pointB.velocity - pointA.velocity;
        float velAlongNormal = Vector3.Dot(relativeVelocity, normal);

        if (velAlongNormal < 0) // Objects moving towards each other
        {
            Vector3 velocityCorrection = normal * (-velAlongNormal * 1.5f); // Increased from 0.5f

            if (!pointA.isPinned && !isAStatic)
                pointA.velocity += velocityCorrection * invMassA / invMassSum;
            if (!pointB.isPinned && !isBStatic)
                pointB.velocity -= velocityCorrection * invMassB / invMassSum;
        }
    }

    private void ApplyCollisionImpulseWithStatic(MassPoint pointA, MassPoint pointB, Vector3 normal, float restitution, float friction, bool isAStatic, bool isBStatic)
    {
        Vector3 relativeVelocity = pointB.velocity - pointA.velocity;
        float velAlongNormal = Vector3.Dot(relativeVelocity, normal);

        if (velAlongNormal > 0) return;

        float massA = isAStatic ? staticObjectMass : pointA.mass;
        float massB = isBStatic ? staticObjectMass : pointB.mass;

        float invMassA = (pointA.isPinned || isAStatic) ? 0f : 1f / massA;
        float invMassB = (pointB.isPinned || isBStatic) ? 0f : 1f / massB;

        if (invMassA + invMassB == 0) return;

        // MUCH STRONGER impulse calculation
        float impulseMagnitude = -(1 + restitution) * velAlongNormal;
        impulseMagnitude /= invMassA + invMassB;
        impulseMagnitude *= collisionResponseStrength * 2f; // Double the response strength

        Vector3 impulse = impulseMagnitude * normal;

        // Apply impulse only to non-static objects
        if (!pointA.isPinned && !isAStatic)
            pointA.velocity -= invMassA * impulse;
        if (!pointB.isPinned && !isBStatic)
            pointB.velocity += invMassB * impulse;

        // Apply stronger friction
        ApplyFrictionImpulse(pointA, pointB, relativeVelocity, normal, friction * 1.5f, impulseMagnitude, invMassA, invMassB, isAStatic, isBStatic);
    }

    private void ApplyFrictionImpulse(MassPoint pointA, MassPoint pointB, Vector3 relativeVelocity, Vector3 normal,
                                    float friction, float normalImpulse, float invMassA, float invMassB, bool isAStatic, bool isBStatic)
    {
        Vector3 tangent = relativeVelocity - Vector3.Dot(relativeVelocity, normal) * normal;
        if (tangent.sqrMagnitude < 0.001f) return;

        tangent.Normalize();

        float frictionImpulseMag = -Vector3.Dot(relativeVelocity, tangent);
        frictionImpulseMag /= invMassA + invMassB;
        frictionImpulseMag = Mathf.Clamp(frictionImpulseMag, -normalImpulse * friction, normalImpulse * friction);

        Vector3 frictionImpulse = frictionImpulseMag * tangent;

        // Apply friction only to non-static objects
        if (!pointA.isPinned && !isAStatic)
            pointA.velocity -= invMassA * frictionImpulse;
        if (!pointB.isPinned && !isBStatic)
            pointB.velocity += invMassB * frictionImpulse;
    }
    private void ApplySpringDeformation(MassPoint pointA, MassPoint pointB, CollisionResultEnhanced result)
    {
        // Only apply deformation if penetration is significant
        if (result.penetrationDepth < minPenetrationForResponse) return;

        // Much more aggressive deformation force
        float baseForce = result.penetrationDepth * collisionResponseStrength * 200f; // Increased from 50f

        // Emergency boost for deep penetrations
        if (result.penetrationDepth > emergencyPenetrationThreshold)
        {
            baseForce *= emergencyForceMultiplier;
        }

        // Clamp the maximum force to prevent explosion
        float deformationForce = Mathf.Min(baseForce, maxCollisionForce);

        Vector3 force = result.normal * deformationForce;

        if (!pointA.isPinned)
            pointA.ApplyForce(force);
        if (!pointB.isPinned)
            pointB.ApplyForce(-force);

        // Apply damping to reduce oscillation
        Vector3 relativeVelocity = pointB.velocity - pointA.velocity;
        Vector3 dampingForce = relativeVelocity * dampingFactor;

        if (!pointA.isPinned)
            pointA.ApplyForce(dampingForce);
        if (!pointB.isPinned)
            pointB.ApplyForce(-dampingForce);
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
    private void ApplyGentleSpringDeformation(MassPoint pointA, MassPoint pointB, CollisionResultEnhanced result, bool isAStatic, bool isBStatic)
    {
        if (result.penetrationDepth < minPenetrationForResponse) return;

        // MUCH STRONGER deformation force
        float baseForce = Mathf.Min(result.penetrationDepth, maxPenetrationDepth) * collisionResponseStrength * 100f; // INCREASED from 10f

        // Add velocity-based force for moving objects
        Vector3 relativeVelocity = pointB.velocity - pointA.velocity;
        float velocityMagnitude = relativeVelocity.magnitude;
        float velocityForce = velocityMagnitude * collisionResponseStrength * 50f;

        float totalForce = baseForce + velocityForce;

        // Emergency boost for deep penetrations
        if (result.penetrationDepth > emergencyPenetrationThreshold)
        {
            totalForce *= emergencyForceMultiplier;
        }

        float deformationForce = Mathf.Min(totalForce, maxCollisionForce);
        Vector3 force = result.normal * deformationForce;

        // Apply force only to non-static objects
        if (!pointA.isPinned && !isAStatic)
            pointA.ApplyForce(force);
        if (!pointB.isPinned && !isBStatic)
            pointB.ApplyForce(-force);

        // STRONGER damping to prevent oscillation
        Vector3 dampingForce = relativeVelocity * dampingFactor * 2f; // Increased from 0.1f

        if (!pointA.isPinned && !isAStatic)
            pointA.ApplyForce(dampingForce);
        if (!pointB.isPinned && !isBStatic)
            pointB.ApplyForce(-dampingForce);
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