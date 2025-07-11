//using UnityEngine;

//public class CollisionHandler : MonoBehaviour
//{
//    // Coefficient of restitution (elasticity), 0 = perfectly inelastic, 1 = perfectly elastic
//    [Range(0, 1)]
//    public float restitution = 0.5f;

//    // Friction coefficient
//    [Range(0, 1)]
//    public float friction = 0.2f;

//    public void HandleCollision(CollisionResult result)
//    {
//        var a = result.pointA;
//        var b = result.pointB;
//        var normal = result.normal;
//        float penetration = result.penetrationDepth;

//        // 1. Skip if both points are pinned (fixed)
//        if (a.isPinned && b.isPinned)
//            return;

//        // 2. Resolve penetration - move points apart
//        ResolvePenetration(a, b, normal, penetration);

//        // 3. Calculate relative velocity along the normal
//        Vector3 relativeVelocity = b.velocity - a.velocity;
//        float velAlongNormal = Vector3.Dot(relativeVelocity, normal);

//        // If points are moving apart, no impulse needed
//        if (velAlongNormal > 0)
//            return;

//        // 4. Calculate impulse scalar
//        float invMassA = a.isPinned ? 0f : 1f / a.mass;
//        float invMassB = b.isPinned ? 0f : 1f / b.mass;

//        float impulseMagnitude = -(1 + restitution) * velAlongNormal;
//        impulseMagnitude /= invMassA + invMassB;

//        Vector3 impulse = impulseMagnitude * normal;

//        // 5. Apply impulse to velocities
//        if (!a.isPinned)
//            a.velocity -= invMassA * impulse;
//        if (!b.isPinned)
//            b.velocity += invMassB * impulse;

//        // 6. Apply friction impulse (tangent direction)
//        Vector3 tangent = relativeVelocity - Vector3.Dot(relativeVelocity, normal) * normal;
//        if (tangent != Vector3.zero)
//            tangent.Normalize();

//        float frictionImpulseMag = -Vector3.Dot(relativeVelocity, tangent);
//        frictionImpulseMag /= invMassA + invMassB;

//        frictionImpulseMag = Mathf.Clamp(frictionImpulseMag, -impulseMagnitude * friction, impulseMagnitude * friction);

//        Vector3 frictionImpulse = frictionImpulseMag * tangent;

//        if (!a.isPinned)
//            a.velocity -= invMassA * frictionImpulse;
//        if (!b.isPinned)
//            b.velocity += invMassB * frictionImpulse;

//        // 7. Optional: Apply spring deformation or forces if needed
//        ApplySpringForces(a);
//        ApplySpringForces(b);
//    }

//    private void ResolvePenetration(MassPoint a, MassPoint b, Vector3 normal, float penetration)
//    {
//        float invMassA = a.isPinned ? 0f : 1f / a.mass;
//        float invMassB = b.isPinned ? 0f : 1f / b.mass;
//        float invMassSum = invMassA + invMassB;

//        if (invMassSum == 0)
//            return;

//        Vector3 correction = normal * (penetration / invMassSum);

//        if (!a.isPinned)
//            a.position += correction * invMassA;
//        if (!b.isPinned)
//            b.position -= correction * invMassB;
//    }

//    private void ApplySpringForces(MassPoint point)
//    {
//        if (point.isPinned) return;

//        // For each spring connected to the mass point
//        foreach (var spring in point.connectedSprings)
//        {
//            // Apply spring force based on Hooke's law
//            Vector3 dir = spring.pointB.position - spring.pointA.position;
//            float currentLength = dir.magnitude;
//            dir.Normalize();

//            float displacement = currentLength - spring.restLength;

//            // Hooke's law force magnitude
//            float forceMagnitude = spring.springStiffness * displacement;

//            // Damping force (based on relative velocity)
//            Vector3 relativeVelocity = spring.pointB.velocity - spring.pointA.velocity;
//            float dampingForceMag = spring.springDamping * Vector3.Dot(relativeVelocity, dir);

//            float totalForceMag = forceMagnitude + dampingForceMag;

//            Vector3 force = totalForceMag * dir;

//            // Apply force to mass points (equal and opposite)
//            if (!spring.pointA.isPinned)
//                spring.pointA.velocity += force / spring.pointA.mass * Time.fixedDeltaTime;
//            if (!spring.pointB.isPinned)
//                spring.pointB.velocity -= force / spring.pointB.mass * Time.fixedDeltaTime;
//        }
//    }


//    public void HandleSpringMassCollision(CollisionResult result)
//    {
//        MassPoint a = result.pointA;
//        MassPoint b = result.pointB;

//        Vector3 normal = result.normal;
//        float depth = result.penetrationDepth;

//        float totalMass = a.mass + b.mass;

//        // 1. Position Correction (split movement based on mass)
//        Vector3 correction = normal * depth;
//        if (!a.isPinned)
//            a.position -= correction * (b.mass / totalMass);
//        if (!b.isPinned)
//            b.position += correction * (a.mass / totalMass);

//        // 2. Velocity Adjustment (simple elastic collision)
//        Vector3 relativeVelocity = b.velocity - a.velocity;
//        float velAlongNormal = Vector3.Dot(relativeVelocity, normal);

//        // Skip if separating
//        if (velAlongNormal > 0) return;

//        float restitution = 0.5f; // Between 0 (inelastic) and 1 (perfect elastic)
//        float impulseMag = -(1 + restitution) * velAlongNormal / (1f / a.mass + 1f / b.mass);

//        Vector3 impulse = impulseMag * normal;
//        if (!a.isPinned)
//            a.velocity -= impulse / a.mass;
//        if (!b.isPinned)
//            b.velocity += impulse / b.mass;
//    }

//}
