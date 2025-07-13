using UnityEngine;
using System.Collections.Generic;

public class SimpleCollisionHandler : MonoBehaviour
{
    [Header("Basic Settings")]
    [Range(0.01f, 1f)]
    public float positionCorrectionStrength = 0.1f; // Much lower

    [Range(0.01f, 1f)]
    public float velocityCorrectionStrength = 0.3f; // Moderate

    [Range(0f, 1f)]
    public float restitution = 0.2f; // Bounciness

    [Range(0f, 1f)]
    public float damping = 0.8f; // Energy loss

    [Header("Safety Limits")]
    public float minPenetrationDepth = 0.01f;
    public float maxPenetrationDepth = 1.0f;
    public float maxForce = 50f;

    [Header("Debug")]
    public bool showDebug = true;
    public float debugDuration = 0.1f;

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

        // 1. Simple position correction
        CorrectPositions(pointA, pointB, collision.normal, collision.penetrationDepth, isAStatic, isBStatic);

        // 2. Simple velocity correction
        CorrectVelocities(pointA, pointB, collision.normal, isAStatic, isBStatic);

        // 3. Debug visualization
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

    private void CorrectPositions(MassPoint pointA, MassPoint pointB, Vector3 normal, float penetration, bool isAStatic, bool isBStatic)
    {
        // Calculate effective masses
        float massA = isAStatic ? float.MaxValue : pointA.mass;
        float massB = isBStatic ? float.MaxValue : pointB.mass;

        float totalMass = massA + massB;
        if (totalMass == 0 || float.IsInfinity(totalMass)) return;

        // Calculate correction amounts based on mass ratio
        float correctionA = isBStatic ? 1f : (massB / totalMass);
        float correctionB = isAStatic ? 1f : (massA / totalMass);

        // Apply gentle position correction
        Vector3 correction = normal * penetration * positionCorrectionStrength;

        if (!pointA.isPinned && !isAStatic)
        {
            pointA.position += correction * correctionA;
        }

        if (!pointB.isPinned && !isBStatic)
        {
            pointB.position -= correction * correctionB;
        }
    }

    private void CorrectVelocities(MassPoint pointA, MassPoint pointB, Vector3 normal, bool isAStatic, bool isBStatic)
    {
        // Calculate relative velocity
        Vector3 relativeVelocity = pointB.velocity - pointA.velocity;
        float velocityAlongNormal = Vector3.Dot(relativeVelocity, normal);

        // Objects moving apart - no need to correct
        if (velocityAlongNormal > 0) return;

        // Calculate effective masses
        float massA = isAStatic ? float.MaxValue : pointA.mass;
        float massB = isBStatic ? float.MaxValue : pointB.mass;

        float invMassA = isAStatic ? 0f : 1f / massA;
        float invMassB = isBStatic ? 0f : 1f / massB;
        float invMassSum = invMassA + invMassB;

        if (invMassSum == 0) return;

        // Calculate impulse magnitude
        float impulseMagnitude = -(1f + restitution) * velocityAlongNormal;
        impulseMagnitude /= invMassSum;
        impulseMagnitude *= velocityCorrectionStrength;

        // Limit impulse to prevent explosions
        impulseMagnitude = Mathf.Min(impulseMagnitude, maxForce);

        Vector3 impulse = normal * impulseMagnitude;

        // Apply impulse
        if (!pointA.isPinned && !isAStatic)
        {
            Vector3 deltaVelocity = -impulse * invMassA;
            pointA.velocity += deltaVelocity;

            // Apply damping
            pointA.velocity *= damping;
        }

        if (!pointB.isPinned && !isBStatic)
        {
            Vector3 deltaVelocity = impulse * invMassB;
            pointB.velocity += deltaVelocity;

            // Apply damping
            pointB.velocity *= damping;
        }
    }

    private void DrawDebugInfo(CollisionResultEnhanced collision)
    {
        // Draw collision line
        Debug.DrawLine(collision.pointA.position, collision.pointB.position, Color.red, debugDuration);

        // Draw penetration vector
        Debug.DrawRay(collision.contactPoint, collision.normal * collision.penetrationDepth, Color.yellow, debugDuration);

        // Draw contact point
        Debug.DrawRay(collision.contactPoint, Vector3.up * 0.1f, Color.green, debugDuration);
    }
}