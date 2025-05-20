using System.Collections.Generic;
using UnityEngine;

public class SpringMassObject : MonoBehaviour
{
    public int resolution = 4; // Number of points per axis (e.g., 4x4x4 cube)
    public float springStiffness = 500f;
    public float springDamping = 2f;
    public float connectionRadius = 1f;

    public GameObject pointPrefab; // For visualizing mass points
    public Material springLineMaterial; // assign in Inspector


   // private List<MassPoint> massPoints = new List<MassPoint>()
    private MassPoint[,,] massPointGrid;

    private List<Spring> springs = new List<Spring>();

    void Start()
    {
        GenerateCubeMassPoints();
        ConnectSprings();
    }

    void FixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime;

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    MassPoint point = massPointGrid[x, y, z];

                    Vector3 gravity = SimulationEnvironment.Instance.GetGravity();
                    point.ApplyForce(gravity * point.Mass, deltaTime);
                }
            }
        }

        foreach (var spring in springs)
        {
            spring.ApplyForce(deltaTime);
        }

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    MassPoint point = massPointGrid[x, y, z];

                    point.Integrate(deltaTime);

                    if (point.physicalObject != null)
                    {
                        point.physicalObject.transform.position = point.position;
                    }
                }
            }
        }

        foreach (var spring in springs)
        {
            spring.UpdateLine();
        }
    }


    void GenerateCubeMassPoints()
    {
        Vector3 size = transform.localScale;
        float spacingX = size.x / (resolution - 1);
        float spacingY = size.y / (resolution - 1);
        float spacingZ = size.z / (resolution - 1);
        Vector3 origin = transform.position - size / 2;
        massPointGrid = new MassPoint[resolution, resolution, resolution];

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    Vector3 localPos = new Vector3(x * spacingX, y * spacingY, z * spacingZ);
                    Vector3 worldPos = origin + localPos;

                    GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
                    PhysicalObject po = go.GetComponent<PhysicalObject>();
                    if (po == null) po = go.AddComponent<PhysicalObject>();

                    MassPoint mp = new MassPoint(worldPos, po);
                    massPointGrid[x, y, z] = mp;
                }
            }
        }

    }

    void ConnectSprings()
    {

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    MassPoint current = massPointGrid[x, y, z];

                    // +X neighbor
                    if (x + 1 < resolution)
                        springs.Add(new Spring(current, massPointGrid[x + 1, y, z], springStiffness, springDamping, this.transform, springLineMaterial));
                    // +Y neighbor
                    if (y + 1 < resolution)
                        springs.Add(new Spring(current, massPointGrid[x, y + 1, z], springStiffness, springDamping, this.transform, springLineMaterial));

                    // +Z neighbor
                    if (z + 1 < resolution)
                        springs.Add(new Spring(current, massPointGrid[x, y, z + 1], springStiffness, springDamping, this.transform, springLineMaterial));

                }
            }
        }
    }

}
