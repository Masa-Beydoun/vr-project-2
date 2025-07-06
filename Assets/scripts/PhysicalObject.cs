using System.Collections.Generic;
using UnityEngine;
public enum ShapeType { Sphere, AABB }
public enum MassShapeType
{
    Cube,
    Sphere,
    Cylinder,
    Capsule,
    Other
}
public class PhysicalObject : MonoBehaviour
{
    public PhysicalMaterial materialPreset;

    public MassShapeType massShapeType = MassShapeType.Cube;
    public float mass = 1f;

    [Header("Shape Dimensions")]
    public float radius = 0.5f;   // Sphere, Cylinder, Capsule
    public float height = 1.0f;   // Cylinder, Capsule
    public float width = 1.0f;    // Cube width
    public float depth = 1.0f;    // Cube depth

    public GameObject meshSourceObject; // For Other shape

    public Vector3 velocity;
    public Vector3 forceAccumulator;

    public ShapeType shapeType = ShapeType.AABB;
    private float dragCoefficient;
    public bool isStatic
    {
        get => _isStatic;
        set
        {
            //Debug.LogWarning($"{name}: isStatic set to {value} at runtime");
            _isStatic = value;
        }
    }

    [SerializeField]
    private bool _isStatic = true;

    void Start()
    {
        dragCoefficient = materialPreset != null ? materialPreset.dragCoefficient : 1f;
    }


    void FixedUpdate()
    {
        if (isStatic) return;

        // Skip force-based integration if this object is driven by a MassPoint
        if (GetComponent<MassPointController>() != null) return;

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
        float area = width * height; // assume cube front face
        float speed = velocity.magnitude;

        if (speed <= 0.01f) return;

        Vector3 dragDirection = -velocity.normalized;
        float dragMagnitude = 0.5f * airDensity * dragCoefficient * area * speed * speed;
        Vector3 dragForce = dragDirection * dragMagnitude;

        ApplyForce(dragForce);
    }
}
