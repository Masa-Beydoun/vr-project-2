using UnityEngine;

public enum CollisionResponseType
{
    Bounce,
    Deform
}

public class CollisionHandler : MonoBehaviour
{
    public CollisionResponseType responseType = CollisionResponseType.Bounce;
    public float restitution = 0.5f; // elasticity, 0 = inelastic, 1 = fully elastic
    public float deformationThreshold = 10f; // force threshold for deformation

    public void HandleCollision(MassPoint a, MassPoint b, Vector3 normal)
    {
        switch (responseType)
        {
            case CollisionResponseType.Bounce:
                HandleBounce(a, b, normal);
                break;
            case CollisionResponseType.Deform:
                HandleDeformation(a, b, normal);
                break;
        }
    }

    private void HandleBounce(MassPoint a, MassPoint b, Vector3 normal)
    {
        Vector3 relativeVelocity = b.velocity - a.velocity;
        float velAlongNormal = Vector3.Dot(relativeVelocity, normal);

        if (velAlongNormal > 0) return; // already separating

        float impulse = -(1 + restitution) * velAlongNormal / (1f / a.Mass + 1f / b.Mass);
        Vector3 impulseVec = impulse * normal;

        a.velocity -= impulseVec / a.Mass;
        b.velocity += impulseVec / b.Mass;
    }

    private void HandleDeformation(MassPoint a, MassPoint b, Vector3 normal)
    {
        float impactForce = (b.velocity - a.velocity).magnitude * 0.5f * (a.Mass + b.Mass);
        if (impactForce >= deformationThreshold)
        {
            Debug.Log("Deformation occurred between points");
            // example: break spring if exists
            Spring s = FindSpring(a, b);
            if (s != null)
            {
                s.broken = true;
            }
        }
        else
        {
            HandleBounce(a, b, normal); // default to bounce
        }
    }

    private Spring FindSpring(MassPoint a, MassPoint b)
    {
        // Implement your way of finding a spring from point a to b
        return null;
    }
}
