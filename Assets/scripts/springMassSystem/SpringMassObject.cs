using System.Collections.Generic;
using UnityEngine;

public class SpringMassObject : MonoBehaviour
{
    public enum MassDistributionShape
    {
        Cube,
        Sphere,
        Capsule
    }

    public MassDistributionShape shape = MassDistributionShape.Cube;
    public int resolution = 4; // number of points per axis or radial/longitudinal for non-cube

    public float springStiffness = 500f;
    public float springDamping = 2f;
    public float connectionRadius = 0.5f;


    public GameObject pointPrefab; // Optional for visualization

    private List<MassPoint> massPoints = new List<MassPoint>();
    private List<Spring> springs = new List<Spring>();

    void Start()
    {
        GenerateMassPoints();
        ConnectSprings();
    }

    void FixedUpdate()
    {
        foreach (var point in massPoints)
        {
            point.ApplyForce(SimulationEnvironment.Instance.GetGravity() * point.mass, Time.fixedDeltaTime); // external force
        }

        foreach (var spring in springs)
        {
            spring.ApplyForce(Time.fixedDeltaTime); // internal spring force
        }

        foreach (var point in massPoints)
        {
            point.Integrate(Time.fixedDeltaTime);
        }
    }


    void GenerateMassPoints()
    {
        switch (shape)
        {
            case MassDistributionShape.Cube:
                GenerateCubeMassPoints();
                break;
            case MassDistributionShape.Sphere:
                GenerateSphereMassPoints();
                break;
            case MassDistributionShape.Capsule:
                GenerateCapsuleMassPoints();
                break;
                // Add others
        }
    }
    

    public void ConnectSprings()
    {
        springs = new List<Spring>();

        for (int i = 0; i < massPoints.Count; i++)
        {
            for (int j = i + 1; j < massPoints.Count; j++)
            {
                float dist = Vector3.Distance(massPoints[i].position, massPoints[j].position);
                if (dist <= connectionRadius)
                {
                    Spring spring = new Spring(massPoints[i], massPoints[j], springStiffness, springDamping);
                    springs.Add(spring);
                }
            }
        }

        Debug.Log($"Connected {springs.Count} springs between {massPoints.Count} points.");
    }


    void GenerateCubeMassPoints()
    {
        Vector3 size = transform.localScale;
        float spacingX = size.x / (resolution - 1);
        float spacingY = size.y / (resolution - 1);
        float spacingZ = size.z / (resolution - 1);
        Vector3 origin = transform.position - size / 2;

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    Vector3 position = origin + new Vector3(x * spacingX, y * spacingY, z * spacingZ);
                    GameObject go = Instantiate(pointPrefab, pos, Quaternion.identity, transform);
                    PhysicalObject po = go.GetComponent<PhysicalObject>();
                    if (po == null) po = go.AddComponent<PhysicalObject>();
                    MassPoint point = new MassPoint(pos, po);
                    massPoints.Add(point);

                    if (pointPrefab)
                        Instantiate(pointPrefab, position, Quaternion.identity, transform);
                }
            }
        }
    }

    void GenerateSphereMassPoints()
    {
        float radius = transform.localScale.x / 2f; // Assuming uniform scale (perfect sphere)
        Vector3 center = transform.position;
        int radialDiv = resolution;
        int polarDiv = resolution;
        int azimuthalDiv = resolution;

        for (int r = 0; r < radialDiv; r++)
        {
            float normalizedRadius = (float)(r + 1) / radialDiv; // skip center
            float actualRadius = normalizedRadius * radius;

            for (int thetaStep = 0; thetaStep < polarDiv; thetaStep++)
            {
                float theta = Mathf.PI * thetaStep / (polarDiv - 1); // polar angle [0, π]

                for (int phiStep = 0; phiStep < azimuthalDiv; phiStep++)
                {
                    float phi = 2 * Mathf.PI * phiStep / azimuthalDiv; // azimuthal angle [0, 2π]

                    float x = actualRadius * Mathf.Sin(theta) * Mathf.Cos(phi);
                    float y = actualRadius * Mathf.Cos(theta);
                    float z = actualRadius * Mathf.Sin(theta) * Mathf.Sin(phi);

                    Vector3 pos = center + new Vector3(x, y, z);
                    GameObject go = Instantiate(pointPrefab, pos, Quaternion.identity, transform);
                    PhysicalObject po = go.GetComponent<PhysicalObject>();
                    if (po == null) po = go.AddComponent<PhysicalObject>();
                    MassPoint point = new MassPoint(pos, po);
                    massPoints.Add(point);

                    if (pointPrefab)
                        Instantiate(pointPrefab, pos, Quaternion.identity, transform);
                }
            }
        }

        // Optionally add a single center point
        MassPoint centerPoint = new MassPoint(center, totalMass / (radialDiv * polarDiv * azimuthalDiv));
        massPoints.Add(centerPoint);
        if (pointPrefab)
            Instantiate(pointPrefab, center, Quaternion.identity, transform);
    }


    void GenerateCapsuleMassPoints()
    {
        float height = transform.localScale.y;
        float radius = transform.localScale.x / 2f; // assuming uniform X/Z
        Vector3 center = transform.position;

        int radialDiv = resolution;
        int heightDiv = resolution;

        float cylinderHeight = height - 2 * radius;
        if (cylinderHeight < 0) cylinderHeight = 0f;

        // === 1. CYLINDER PART ===
        for (int yStep = 0; yStep <= heightDiv; yStep++)
        {
            float y = -cylinderHeight / 2f + yStep * (cylinderHeight / heightDiv);

            for (int i = 0; i < radialDiv; i++)
            {
                float theta = 2 * Mathf.PI * i / radialDiv;

                for (int j = 1; j <= radialDiv; j++)
                {
                    float r = radius * j / radialDiv;

                    float x = r * Mathf.Cos(theta);
                    float z = r * Mathf.Sin(theta);

                    Vector3 pos = center + new Vector3(x, y, z);
                    GameObject go = Instantiate(pointPrefab, pos, Quaternion.identity, transform);
                    PhysicalObject po = go.GetComponent<PhysicalObject>();
                    if (po == null) po = go.AddComponent<PhysicalObject>();
                    MassPoint point = new MassPoint(pos, po);
                    massPoints.Add(point);
                    if (pointPrefab)
                        Instantiate(pointPrefab, pos, Quaternion.identity, transform);
                }
            }
        }

        // === 2. TOP HEMISPHERE ===
        GenerateHemisphere(center + Vector3.up * (cylinderHeight / 2f), radius, true);

        // === 3. BOTTOM HEMISPHERE ===
        GenerateHemisphere(center - Vector3.up * (cylinderHeight / 2f), radius, false);
    }

    void GenerateHemisphere(Vector3 origin, float radius, bool top)
    {
        int latDiv = resolution / 2;
        int lonDiv = resolution;

        for (int i = 1; i <= latDiv; i++)
        {
            float theta = Mathf.PI * i / (2 * latDiv);
            if (!top) theta = Mathf.PI - theta;

            for (int j = 0; j < lonDiv; j++)
            {
                float phi = 2 * Mathf.PI * j / lonDiv;

                float x = radius * Mathf.Sin(theta) * Mathf.Cos(phi);
                float y = radius * Mathf.Cos(theta);
                float z = radius * Mathf.Sin(theta) * Mathf.Sin(phi);

                Vector3 pos = origin + new Vector3(x, y, z);
                GameObject go = Instantiate(pointPrefab, pos, Quaternion.identity, transform);
                PhysicalObject po = go.GetComponent<PhysicalObject>();
                if (po == null) po = go.AddComponent<PhysicalObject>();

                MassPoint point = new MassPoint(pos, po);
                massPoints.Add(point);
            }
        }
    }


}
