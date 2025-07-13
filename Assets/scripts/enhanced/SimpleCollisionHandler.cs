using UnityEngine;
using System.Collections.Generic;

public class SimpleCollisionHandler : MonoBehaviour
{
    [Header("Force-Based Collision Settings")]
    [Range(100f, 10000f)]
    public float collisionForceStrength = 2000f; // Reduced from 10000f

    [Range(100f, 10000f)]
    public float separationForceStrength = 3000f; // Reduced from 8000f

    [Range(0f, 1f)]
    public float restitution = 0.4f; // Reduced from 0.6f

    [Range(0f, 1f)]
    public float friction = 0.2f; // Reduced from 0.3f

    [Header("Force Scaling")]
    [Range(0.1f, 5f)]
    public float forceMultiplier = 1f; // Reduced from 3f

    [Range(0.01f, 1f)]
    public float velocityDamping = 0.8f; // Reduced from 0.9f

    [Header("Safety Limits")]
    public float minPenetrationDepth = 0.001f;
    public float maxPenetrationDepth = 0.5f; // Reduced from 2.0f
    public float maxCollisionForce = 5000f; // Reduced from 15000f

    [Header("Position Correction")]
    [Range(0f, 1f)]
    public float positionCorrectionFactor = 0.8f; // NEW: Direct position correction
    public float positionCorrectionThreshold = 0.01f; // NEW: Minimum penetration for correction

    [Header("Debug")]
    public bool showDebug = false; // Turn off by default
    public float debugDuration = 0.2f;

    public void HandleSpringMassCollision(CollisionResultEnhanced collision)
    {
        // Validate collision
        if (!IsValidCollision(collision))
        {
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
            return;
        }

        if (showDebug)
        {
            Debug.Log($"Collision detected: penetration={collision.penetrationDepth}, normal={collision.normal}");
        }

        // Apply position correction first (immediate separation)
        ApplyPositionCorrection(pointA, pointB, collision.normal, collision.penetrationDepth, isAStatic, isBStatic);

        // Calculate collision forces (reduced intensity)
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
            return false;
        }
        if (!collision.collided)
        {
            return false;
        }
        if (collision.penetrationDepth < minPenetrationDepth)
        {
            return false;
        }
        if (collision.penetrationDepth > maxPenetrationDepth)
        {
            if (showDebug)
            {
                Debug.LogWarning($"Penetration too large: {collision.penetrationDepth} > {maxPenetrationDepth}");
            }
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

    // NEW: Direct position correction for immediate separation
    private void ApplyPositionCorrection(MassPoint pointA, MassPoint pointB, Vector3 normal, float penetration, bool isAStatic, bool isBStatic)
    {
        if (penetration < positionCorrectionThreshold) return;

        // Calculate correction based on mass ratio
        float totalMass = (isAStatic ? 0f : pointA.mass) + (isBStatic ? 0f : pointB.mass);
        if (totalMass == 0) return;

        float correctionAmount = penetration * positionCorrectionFactor;
        Vector3 correctionVector = normal * correctionAmount;

        if (!pointA.isPinned && !isAStatic)
        {
            float massRatioA = isBStatic ? 1f : pointB.mass / totalMass;
            Vector3 correctionA = correctionVector * massRatioA;
            pointA.position += correctionA;

            if (showDebug)
            {
                Debug.Log($"Position correction A: {correctionA}");
            }
        }

        if (!pointB.isPinned && !isBStatic)
        {
            float massRatioB = isAStatic ? 1f : pointA.mass / totalMass;
            Vector3 correctionB = -correctionVector * massRatioB;
            pointB.position += correctionB;

            if (showDebug)
            {
                Debug.Log($"Position correction B: {correctionB}");
            }
        }
    }

    private void ApplyCollisionForces(MassPoint pointA, MassPoint pointB, Vector3 normal, float penetration, bool isAStatic, bool isBStatic)
    {
        // Calculate effective masses
        float massA = isAStatic ? float.MaxValue : pointA.mass;
        float massB = isBStatic ? float.MaxValue : pointB.mass;

        // Calculate relative velocity
        Vector3 relativeVelocity = pointB.velocity - pointA.velocity;
        float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal);

        if (showDebug)
        {
            Debug.Log($"Relative velocity: {relativeVelocity.magnitude}, along normal: {velocityAlongNormal}");
        }

        // 1. Separation force (gentler, based on penetration depth)
        float separationForceMagnitude = separationForceStrength * penetration * forceMultiplier;
        separationForceMagnitude = Mathf.Min(separationForceMagnitude, maxCollisionForce * 0.5f); // Limit to half max force

        Vector3 separationForce = normal * separationForceMagnitude;

        if (showDebug)
        {
            Debug.Log($"Separation force magnitude: {separationForceMagnitude}");
        }

        // 2. Collision response force (only if objects are moving toward each other)
        float collisionForceMagnitude = 0f;
        if (velocityAlongNormal < -0.1f) // Increased threshold to prevent micro-collisions
        {
            collisionForceMagnitude = -velocityAlongNormal * collisionForceStrength * forceMultiplier;
            collisionForceMagnitude = Mathf.Min(collisionForceMagnitude, maxCollisionForce * 0.5f);
        }

        Vector3 collisionForce = normal * collisionForceMagnitude;

        if (showDebug)
        {
            Debug.Log($"Collision force magnitude: {collisionForceMagnitude}");
        }

        // 3. Calculate mass-based force distribution
        float totalInvMass = (isAStatic ? 0f : 1f / massA) + (isBStatic ? 0f : 1f / massB);
        if (totalInvMass == 0) return;

        float forceRatioA = isBStatic ? 1f : (1f / massA) / totalInvMass;
        float forceRatioB = isAStatic ? 1f : (1f / massB) / totalInvMass;

        // 4. Apply forces with improved scaling
        Vector3 totalForce = separationForce + collisionForce * (1f + restitution);

        // Scale by time step to make forces frame-rate independent
        float timeScale = Time.fixedDeltaTime / 0.02f; // Normalize to 50Hz
        totalForce *= timeScale;

        if (showDebug)
        {
            Debug.Log($"Total force magnitude: {totalForce.magnitude}");
        }

        // Apply forces with safety check
        if (!pointA.isPinned && !isAStatic)
        {
            Vector3 forceA = totalForce * forceRatioA;

            // Safety check for excessive force
            if (forceA.magnitude > maxCollisionForce)
            {
                forceA = forceA.normalized * maxCollisionForce;
                if (showDebug)
                {
                    Debug.LogWarning($"Clamped force A to {maxCollisionForce}");
                }
            }

            pointA.ApplyForce(forceA);

            if (showDebug)
            {
                Debug.Log($"Applied force to A: {forceA}");
            }

            // Apply friction force (tangential) - improved version
            if (friction > 0)
            {
                ApplyFrictionForce(pointA, relativeVelocity, normal, separationForceMagnitude, false);
            }
        }

        if (!pointB.isPinned && !isBStatic)
        {
            Vector3 forceB = -totalForce * forceRatioB;

            // Safety check for excessive force
            if (forceB.magnitude > maxCollisionForce)
            {
                forceB = forceB.normalized * maxCollisionForce;
                if (showDebug)
                {
                    Debug.LogWarning($"Clamped force B to {maxCollisionForce}");
                }
            }

            pointB.ApplyForce(forceB);

            if (showDebug)
            {
                Debug.Log($"Applied force to B: {forceB}");
            }

            // Apply friction force (tangential) - improved version
            if (friction > 0)
            {
                ApplyFrictionForce(pointB, -relativeVelocity, normal, separationForceMagnitude, true);
            }
        }
    }

    private void ApplyFrictionForce(MassPoint point, Vector3 relativeVelocity, Vector3 normal, float normalForce, bool isPointB)
    {
        Vector3 tangentialVelocity = relativeVelocity - Vector3.Project(relativeVelocity, normal);

        if (tangentialVelocity.magnitude > 0.01f)
        {
            Vector3 frictionDirection = -tangentialVelocity.normalized;
            float frictionMagnitude = friction * normalForce * 0.3f; // Reduced friction multiplier
            Vector3 frictionForce = frictionDirection * frictionMagnitude;

            point.ApplyForce(frictionForce);

            if (showDebug)
            {
                Debug.Log($"Applied friction to {(isPointB ? "B" : "A")}: {frictionForce} (magnitude: {frictionMagnitude})");
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

            if (showDebug)
            {
                Debug.Log($"Damped velocity A: {oldVelocity} -> {pointA.velocity}");
            }
        }

        if (!pointB.isPinned && !isBStatic)
        {
            Vector3 oldVelocity = pointB.velocity;
            pointB.velocity *= velocityDamping;

            if (showDebug)
            {
                Debug.Log($"Damped velocity B: {oldVelocity} -> {pointB.velocity}");
            }
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