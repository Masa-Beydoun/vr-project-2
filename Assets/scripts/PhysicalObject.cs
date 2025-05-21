using UnityEngine;

public class PhysicalObject : MonoBehaviour
{
    public PhysicalMaterial materialPreset;

    public float mass = 1f;
    public Vector3 velocity;
    public Vector3 forceAccumulator;

    [HideInInspector]
    public float radius;

    public enum ShapeType { Sphere, AABB }
    public ShapeType shape;

    private float dragCoefficient;

    public bool isStatic = false;

    void Start()
    {
        dragCoefficient = materialPreset != null ? materialPreset.dragCoefficient : 1f;

        if (shape == ShapeType.Sphere)
        {
            radius = transform.localScale.x / 2f;
        }
    }


    void FixedUpdate()
    {
        if (isStatic) return;

        ApplyGravity();
        ApplyAirResistance();

        transform.position += velocity * Time.fixedDeltaTime;
    }

    private void ApplyGravity()
    {
        Vector3 gravity = SimulationEnvironment.Instance.GetGravity();
        ApplyForce(gravity * mass);
    }

    public void ApplyForce(Vector3 force)
    {
        if (isStatic) return;
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
