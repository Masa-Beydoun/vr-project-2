using UnityEngine;
using System.Collections.Generic;

public class SimpleCollisionHandler : MonoBehaviour
{
    [Header("Force-Based Collision Settings")]
    [Range(100f, 50000f)]
    public float collisionForceStrength = 10000f; // Increased for better response

    [Range(100f, 50000f)]
    public float separationForceStrength = 8000f; // Increased for better separation

    [Range(0f, 1f)]
    public float restitution = 0.6f; // Bounciness

    [Range(0f, 1f)]
    public float friction = 0.3f; // Surface friction

    [Header("Force Scaling")]
    [Range(0.1f, 10f)]
    public float forceMultiplier = 3f; // Increased overall force scaling

    [Range(0.01f, 1f)]
    public float velocityDamping = 0.9f; // Velocity damping on collision

    [Header("Safety Limits")]
    public float minPenetrationDepth = 0.001f; // Reduced for better sensitivity
    public float maxPenetrationDepth = 2.0f;
    public float maxCollisionForce = 15000f; // Increased limit

    [Header("Debug")]
    public bool showDebug = true;
    public float debugDuration = 0.2f;

    public void HandleSpringMassCollision(CollisionResultEnhanced collision)
    {
        // Validate collision
        if (!IsValidCollision(collision))
        {
            Debug.Log("Invalid collision detected");
            return;
        }

        var pointA = collision.pointA;
        var pointB = collision.pointB;

        // Check if either point is static/ground
        bool isAStatic = IsStaticPoint(pointA);
        bool isBStatic = IsStaticPoint(pointB);

        // Skip if both are static
        if (isAStatic && isBStatic)
        {
            Debug.Log("Both points are static, skipping collision");
            return;
        }

        Debug.Log($"Collision detected: penetration={collision.penetrationDepth}, normal={collision.normal}");

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
        if (collision.pointA == null || collision.pointB == null)
        {
            Debug.Log("Null points in collision");
            return false;
        }
        if (!collision.collided)
        {
            Debug.Log("Collision flag is false");
            return false;
        }
        if (collision.penetrationDepth < minPenetrationDepth)
        {
            Debug.Log($"Penetration too small: {collision.penetrationDepth} < {minPenetrationDepth}");
            return false;
        }
        if (collision.penetrationDepth > maxPenetrationDepth)
        {
            Debug.Log($"Penetration too large: {collision.penetrationDepth} > {maxPenetrationDepth}");
            return false;
        }
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

        Debug.Log($"Relative velocity: {relativeVelocity.magnitude}, along normal: {velocityAlongNormal}");

        // 1. Separation force (based on penetration depth) - ALWAYS apply this
        float separationForceMagnitude = separationForceStrength * penetration * forceMultiplier;
        separationForceMagnitude = Mathf.Min(separationForceMagnitude, maxCollisionForce);

        Vector3 separationForce = normal * separationForceMagnitude;

        Debug.Log($"Separation force magnitude: {separationForceMagnitude}");

        // 2. Collision response force (based on relative velocity)
        float collisionForceMagnitude = 0f;
        if (velocityAlongNormal < -0.01f) // Objects moving towards each other (with small threshold)
        {
            collisionForceMagnitude = -velocityAlongNormal * collisionForceStrength * forceMultiplier;
            collisionForceMagnitude = Mathf.Min(collisionForceMagnitude, maxCollisionForce);
        }

        Vector3 collisionForce = normal * collisionForceMagnitude;

        Debug.Log($"Collision force magnitude: {collisionForceMagnitude}");

        // 3. Calculate mass-based force distribution
        float totalInvMass = (isAStatic ? 0f : 1f / massA) + (isBStatic ? 0f : 1f / massB);
        if (totalInvMass == 0) return;

        float forceRatioA = isBStatic ? 1f : (1f / massA) / totalInvMass;
        float forceRatioB = isAStatic ? 1f : (1f / massB) / totalInvMass;

        // 4. Apply forces
        Vector3 totalForce = separationForce + collisionForce * (1f + restitution);

        Debug.Log($"Total force magnitude: {totalForce.magnitude}");

        if (!pointA.isPinned && !isAStatic)
        {
            Vector3 forceA = totalForce * forceRatioA;
            pointA.ApplyForce(forceA);

            Debug.Log($"Applied force to A: {forceA}");

            // Apply friction force (tangential) - FIXED VERSION
            if (friction > 0)
            {
                Vector3 tangentialVelocity = relativeVelocity - Vector3.Project(relativeVelocity, normal);

                // Only apply friction if there's tangential movement
                if (tangentialVelocity.magnitude > 0.01f)
                {
                    Vector3 frictionDirection = -tangentialVelocity.normalized;
                    float frictionMagnitude = friction * separationForceMagnitude * 0.5f;
                    Vector3 frictionForce = frictionDirection * frictionMagnitude;

                    pointA.ApplyForce(frictionForce);
                    Debug.Log($"Applied friction to A: {frictionForce} (magnitude: {frictionMagnitude})");
                }
                else
                {
                    Debug.Log("No tangential velocity for friction on A");
                }
            }
        }

        if (!pointB.isPinned && !isBStatic)
        {
            Vector3 forceB = -totalForce * forceRatioB;
            pointB.ApplyForce(forceB);

            Debug.Log($"Applied force to B: {forceB}");

            // Apply friction force (tangential) - FIXED VERSION
            if (friction > 0)
            {
                Vector3 tangentialVelocity = relativeVelocity - Vector3.Project(relativeVelocity, normal);

                // Only apply friction if there's tangential movement
                if (tangentialVelocity.magnitude > 0.01f)
                {
                    Vector3 frictionDirection = tangentialVelocity.normalized;
                    float frictionMagnitude = friction * separationForceMagnitude * 0.5f;
                    Vector3 frictionForce = frictionDirection * frictionMagnitude;

                    pointB.ApplyForce(frictionForce);
                    Debug.Log($"Applied friction to B: {frictionForce} (magnitude: {frictionMagnitude})");
                }
                else
                {
                    Debug.Log("No tangential velocity for friction on B");
                }
            }
        }
    }



    private void ApplyVelocityDamping(MassPoint pointA, MassPoint pointB, Vector3 normal, bool isAStatic, bool isBStatic)
    {
        // Apply damping to reduce oscillations
        if (!pointA.isPinned && !isAStatic)
        {
            Vector3 oldVelocity = pointA.velocity;
            pointA.velocity *= velocityDamping;
            Debug.Log($"Damped velocity A: {oldVelocity} -> {pointA.velocity}");
        }

        if (!pointB.isPinned && !isBStatic)
        {
            Vector3 oldVelocity = pointB.velocity;
            pointB.velocity *= velocityDamping;
            Debug.Log($"Damped velocity B: {oldVelocity} -> {pointB.velocity}");
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