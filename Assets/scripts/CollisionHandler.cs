using UnityEngine;

public enum CollisionResponseType
{
    Bounce,
    Deform
}
public class CollisionHandler : MonoBehaviour
{
    public CollisionResponseType responseType = CollisionResponseType.Bounce;
    public float restitution = 0.5f;
    public float deformationThreshold = 10f;
    public float minPenetrationCorrection = 0.01f;

    public void HandleCollision(MassPoint a, MassPoint b)
    {
        Vector3 normal = (a.position - b.position).normalized;
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

        if (velAlongNormal > 0) return; // they're separating

        float invMassA = 1f / a.Mass;
        float invMassB = 1f / b.Mass;

        float impulseMag = -(1 + restitution) * velAlongNormal / (invMassA + invMassB);
        Vector3 impulse = impulseMag * normal;

        a.velocity -= impulse * invMassA;
        b.velocity += impulse * invMassB;

        // Optional: positional correction to resolve penetration
        Vector3 correction = normal * minPenetrationCorrection;
        a.position += correction * 0.5f;
        b.position -= correction * 0.5f;
    }

    private void HandleDeformation(MassPoint a, MassPoint b, Vector3 normal)
    {
        float impactForce = (b.velocity - a.velocity).magnitude * 0.5f * (a.Mass + b.Mass);

        if (impactForce >= deformationThreshold)
        {
            Debug.Log("Deformation: breaking spring between points.");
            Spring s = FindSpring(a, b);
            if (s != null) s.broken = true;
        }
        else
        {
            HandleBounce(a, b, normal); // fallback
        }
    }

    private Spring FindSpring(MassPoint a, MassPoint b)
    {
        // You must implement this based on how your springs are stored
        return null;
    }
}
