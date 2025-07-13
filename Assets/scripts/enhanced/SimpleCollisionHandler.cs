using UnityEngine;
using System.Collections.Generic;

public class SimpleCollisionHandler : MonoBehaviour
{
    [Header("Force-Based Collision Settings")]
    [Range(100f, 10000f)]
    public float collisionForceStrength = 2000f; // Much higher for forces

    [Range(100f, 5000f)]
    public float separationForceStrength = 1500f; // Force to separate overlapping objects

    [Range(0f, 1f)]
    public float restitution = 0.6f; // Bounciness

    [Range(0f, 1f)]
    public float friction = 0.3f; // Surface friction

    [Header("Force Scaling")]
    [Range(0.1f, 10f)]
    public float forceMultiplier = 2f; // Overall force scaling

    [Range(0.01f, 1f)]
    public float velocityDamping = 0.9f; // Velocity damping on collision

    [Header("Safety Limits")]
    public float minPenetrationDepth = 0.01f;
    public float maxPenetrationDepth = 2.0f;
    public float maxCollisionForce = 5000f;

    [Header("Debug")]
    public bool showDebug = true;
    public float debugDuration = 0.2f;

    public void HandleSpringMassCollision(CollisionResultEnhanced collision)
    {
        // Validate collision
        if (!IsValidCollision(collision)) return;

        var pointA = collision.pointA;
        var pointB = collision.pointB;

        // Check if either point is static/ground
        bool isAStatic = IsStaticPoint(pointA);
        bool isBStatic = IsStaticPoint(pointB);

        // Skip if both are static
        if (isAStatic && isBStatic) return;

        // Calculate collision forces
        ApplyCollisionForces(pointA, pointB, collision.normal, collision.penetrationDepth, isAStatic, isBStatic);

        // Apply velocity damping
        ApplyVelocityDamping(pointA, pointB, collision.normal, isAStatic, isBStatic);

        // Debug visualization
        if (showDebug)
        {
            DrawDebugInfo(collision);
        }
    }

    private bool IsValidCollision(CollisionResultEnhanced collision)
    {
        if (collision.pointA == null || collision.pointB == null) return false;
        if (!collision.collided) return false;
        if (collision.penetrationDepth < minPenetrationDepth) return false;
        if (collision.penetrationDepth > maxPenetrationDepth) return false;
        return true;
    }

    private bool IsStaticPoint(MassPoint point)
    {
        if (point.isPinned) return true;
        if (point.physicalObject != null && point.physicalObject.isStatic) return true;

        // Check name for ground/static objects
        string name = point.sourceName.ToLower();
        return name.Contains("ground") || name.Contains("floor") || name.Contains("static");
    }

    private void ApplyCollisionForces(MassPoint pointA, MassPoint pointB, Vector3 normal, float penetration, bool isAStatic, bool isBStatic)
    {
        // Calculate effective masses
        float massA = isAStatic ? float.MaxValue : pointA.mass;
        float massB = isBStatic ? float.MaxValue : pointB.mass;

        // Calculate relative velocity
        Vector3 relativeVelocity = pointB.velocity - pointA.velocity;
        float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal);

        // 1. Separation force (based on penetration depth)
        float separationForceMagnitude = separationForceStrength * penetration * forceMultiplier;
        separationForceMagnitude = Mathf.Min(separationForceMagnitude, maxCollisionForce);

        Vector3 separationForce = normal * separationForceMagnitude;

        // 2. Collision response force (based on relative velocity)
        float collisionForceMagnitude = 0f;
        if (velocityAlongNormal < 0) // Objects moving towards each other
        {
            collisionForceMagnitude = -velocityAlongNormal * collisionForceStrength * forceMultiplier;
            collisionForceMagnitude = Mathf.Min(collisionForceMagnitude, maxCollisionForce);
        }

        Vector3 collisionForce = normal * collisionForceMagnitude;

        // 3. Calculate mass-based force distribution
        float totalInvMass = (isAStatic ? 0f : 1f / massA) + (isBStatic ? 0f : 1f / massB);
        if (totalInvMass == 0) return;

        float forceRatioA = isBStatic ? 1f : (1f / massA) / totalInvMass;
        float forceRatioB = isAStatic ? 1f : (1f / massB) / totalInvMass;

        // 4. Apply forces
        Vector3 totalForce = separationForce + collisionForce * (1f + restitution);

        if (!pointA.isPinned && !isAStatic)
        {
            Vector3 forceA = totalForce * forceRatioA;
            pointA.ApplyForce(forceA);

            // Apply friction force (tangential)
            if (friction > 0)
            {
                Vector3 tangentialVelocity = relativeVelocity - Vector3.Project(relativeVelocity, normal);
                Vector3 frictionForce = -tangentialVelocity.normalized * friction * separationForceMagnitude * 0.5f;
                pointA.ApplyForce(frictionForce);
                Debug.Log("frictionForce" + frictionForce);
            }
        }

        if (!pointB.isPinned && !isBStatic)
        {
            Vector3 forceB = -totalForce * forceRatioB;
            pointB.ApplyForce(forceB);

            // Apply friction force (tangential)
            if (friction > 0)
            {
                Vector3 tangentialVelocity = relativeVelocity - Vector3.Project(relativeVelocity, normal);
                Vector3 frictionForce = tangentialVelocity.normalized * friction * separationForceMagnitude * 0.5f;
                pointB.ApplyForce(frictionForce);
                Debug.Log("frictionForce" + frictionForce);

            }
        }
    }

    private void ApplyVelocityDamping(MassPoint pointA, MassPoint pointB, Vector3 normal, bool isAStatic, bool isBStatic)
    {
        // Apply damping to reduce oscillations
        if (!pointA.isPinned && !isAStatic)
        {
            pointA.velocity *= velocityDamping;
        }

        if (!pointB.isPinned && !isBStatic)
        {
            pointB.velocity *= velocityDamping;
        }
    }

    private void DrawDebugInfo(CollisionResultEnhanced collision)
    {
        // Draw collision line
        Debug.DrawLine(collision.pointA.position, collision.pointB.position, Color.red, debugDuration);

        // Draw penetration vector
        Debug.DrawRay(collision.contactPoint, collision.normal * collision.penetrationDepth, Color.yellow, debugDuration);

        // Draw contact point
        Debug.DrawRay(collision.contactPoint, Vector3.up * 0.2f, Color.green, debugDuration);

        // Draw force magnitude indicator
        float forceIndicator = Mathf.Min(collision.penetrationDepth * separationForceStrength / 1000f, 1f);
        Debug.DrawRay(collision.contactPoint, Vector3.right * forceIndicator, Color.cyan, debugDuration);
    }
}