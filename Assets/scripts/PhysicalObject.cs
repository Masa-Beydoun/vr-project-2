using UnityEngine;


public class PhysicalObject : MonoBehaviour
{
    public PhysicalMaterial materialPreset;
    public float mass = 1f; // user-defined

    private float dragCoefficient;
    public Vector3 velocity;

    void Start()
    {
        if (materialPreset != null)
        {
            dragCoefficient = materialPreset.dragCoefficient;
        }
        else
        {
            dragCoefficient = 1f;
        }
    }

    void FixedUpdate()
    {
        Vector3 gravity = SimulationEnvironment.Instance.GetGravity();
        ApplyForce(gravity * mass);

        ApplyAirResistance();

        transform.position += velocity * Time.fixedDeltaTime;
    }

    public void ApplyForce(Vector3 force)
    {
        Vector3 acceleration = force / mass;
        velocity += acceleration * Time.fixedDeltaTime;
    }

    private void ApplyAirResistance()
    {
        float airDensity = SimulationEnvironment.Instance.GetAirDensity();
        float area = transform.localScale.x * transform.localScale.z;
        float speed = velocity.magnitude;

        if (speed <= 0.01f) return;

        Vector3 dragDirection = -velocity.normalized;
        float dragMagnitude = 0.5f * airDensity * dragCoefficient * area * speed * speed;
        Vector3 dragForce = dragDirection * dragMagnitude;

        ApplyForce(dragForce);
    }
}
