using System.Collections.Generic;
using UnityEngine;

public enum MassShapeType
{
    Cube,
    Sphere
}

public class SpringMassSystem : MonoBehaviour
{
    public MassShapeType shapeType = MassShapeType.Cube;
    public int resolution = 5;
    public float springStiffness = 500f;
    public float springDamping = 2f;
    public GameObject pointPrefab;
    public Material springLineMaterial;

    private MassPoint[,,] cubeGrid;
    private List<MassPoint> allPoints = new List<MassPoint>();
    private List<Spring> springs = new List<Spring>();

    private float connectionRadius;

    void Start()
    {
        switch (shapeType)
        {
            case MassShapeType.Cube:
                GenerateCubePoints();
                ConnectCubeSprings();
                break;
            case MassShapeType.Sphere:
                GenerateSpherePoints();
                ConnectSphereSprings();
                break;
        }

        //if (allpoints.count > 0 && allpoints[0].physicalobject != null && allpoints[0].physicalobject.materialpreset != null)
        //{
        //    var mat = allpoints[0].physicalobject.materialpreset;
        //    springstiffness = mat.stiffness;
        //    springdamping = 2f; // if you want damping from materialpreset, add it there too.
        //} 
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 gravity = SimulationEnvironment.Instance.GetGravity();

        foreach (var p in allPoints)
            p.ApplyForce(gravity * p.Mass, dt);

        foreach (var s in springs)
            s.ApplyForce(dt);

        foreach (var p in allPoints)
        {
            p.Integrate(dt);
            if (p.physicalObject != null)
                p.physicalObject.transform.position = p.position;
        }

        foreach (var s in springs)
            s.UpdateLine();
    }

    void GenerateCubePoints()
    {
        Vector3 size = transform.localScale;
        float dx = size.x / (resolution - 1);
        float dy = size.y / (resolution - 1);
        float dz = size.z / (resolution - 1);
        Vector3 origin = transform.position - size / 2;

        cubeGrid = new MassPoint[resolution, resolution, resolution];

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    Vector3 localPos = new Vector3(x * dx, y * dy, z * dz);
                    Vector3 worldPos = origin + localPos;


                    GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
                    go.transform.localScale = Vector3.one * 0.05f;  // fixed size, ignore parent scale

                    var po = go.GetComponent<PhysicalObject>() ?? go.AddComponent<PhysicalObject>();
                    var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                    MassPoint mp = new MassPoint(worldPos, po);
                    controller.Initialize(mp);
                    cubeGrid[x, y, z] = mp;
                    allPoints.Add(mp);
                }
            }
        }

        // Pin corners
        cubeGrid[0, 0, 0].isPinned = true;
        cubeGrid[0, 0, resolution - 1].isPinned = true;
    }

    void ConnectCubeSprings()
    {
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    var current = cubeGrid[x, y, z];
                    if (x + 1 < resolution)
                        springs.Add(new Spring(current, cubeGrid[x + 1, y, z], springStiffness, springDamping, transform, springLineMaterial));
                    if (y + 1 < resolution)
                        springs.Add(new Spring(current, cubeGrid[x, y + 1, z], springStiffness, springDamping, transform, springLineMaterial));
                    if (z + 1 < resolution)
                        springs.Add(new Spring(current, cubeGrid[x, y, z + 1], springStiffness, springDamping, transform, springLineMaterial));
                    // Add full 3D diagonals (volume diagonals)
                    if (x + 1 < resolution && y + 1 < resolution && z + 1 < resolution)
                        springs.Add(new Spring(current, cubeGrid[x + 1, y + 1, z + 1], springStiffness, springDamping, transform, springLineMaterial));

                }
            }
        }
    }

    void GenerateSpherePoints()
    {
        float radius = transform.localScale.x / 2f;
        float step = (2f * radius) / (resolution - 1);
        connectionRadius = step * Mathf.Sqrt(3);
        Vector3 center = transform.position;

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    Vector3 offset = new Vector3(x * step - radius, y * step - radius, z * step - radius);
                    if (offset.magnitude <= radius)
                    {
                        Vector3 worldPos = center + offset;
                        GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
                        go.transform.localScale = Vector3.one * 0.05f;  // fixed size, ignore parent scale

                        var po = go.GetComponent<PhysicalObject>() ?? go.AddComponent<PhysicalObject>();
                        var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                        go.AddComponent<CollisionBody>();
                        MassPoint mp = new MassPoint(worldPos, po);
                        controller.Initialize(mp);
                        allPoints.Add(mp);
                    }
                }
            }
        }
    }

    void ConnectSphereSprings()
    {
        for (int i = 0; i < allPoints.Count; i++)
        {
            for (int j = i + 1; j < allPoints.Count; j++)
            {
                if (Vector3.Distance(allPoints[i].position, allPoints[j].position) <= connectionRadius)
                {
                    springs.Add(new Spring(allPoints[i], allPoints[j], springStiffness, springDamping, transform, springLineMaterial));
                }
            }
        }
    }
}
