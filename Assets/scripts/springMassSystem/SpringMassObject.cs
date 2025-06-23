using System.Collections.Generic;
using UnityEngine;

public enum MassShapeType
{
    Cube,
    Sphere,
    Cylinder,
    Capsule,
    Other
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

    [Header("Shape Dimensions")]
    public float radius = 0.5f;     // for Sphere, Cylinder, Capsule
    public float height = 1.0f;     // for Cylinder, Capsule
    public float width = 1.0f;      // for Cube
    public float depth = 1.0f;      // for Cube

    [Header("For Mesh Input")]
    public GameObject meshSourceObject;
    public int k;


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
            case MassShapeType.Cylinder:
                GenerateCylinderPoints();
                ConnectSphereSprings();
                break;
            case MassShapeType.Capsule:
                GenerateCapsulePoints();
                ConnectSphereSprings();
                break;
            case MassShapeType.Other:
                if (meshSourceObject != null)
                {
                    GenerateMeshPoints(meshSourceObject);
                }
                else
                {
                    Debug.LogError("You selected 'Other' but did not assign a meshSourceObject.");
                }
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
        float dx = width / (resolution - 1);
        float dy = height / (resolution - 1);
        float dz = depth / (resolution - 1);
        Vector3 origin = transform.position - new Vector3(width, height, depth) / 2f;

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
                    go.transform.localScale = Vector3.one * 0.05f;

                    var po = go.GetComponent<PhysicalObject>() ?? go.AddComponent<PhysicalObject>();
                    var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                    MassPoint mp = new MassPoint(worldPos, po);
                    controller.Initialize(mp);
                    cubeGrid[x, y, z] = mp;
                    allPoints.Add(mp);
                }
            }
        }

    }

    void ConnectCubeSprings()
    {
        float dx = width / (resolution - 1);
        float dy = height / (resolution - 1);
        float dz = depth / (resolution - 1);
        connectionRadius = Mathf.Sqrt(dx * dx + dy * dy + dz * dz) * 1.1f; // Slightly over nearest-neighbor distance

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    var current = cubeGrid[x, y, z];

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            for (int k = -1; k <= 1; k++)
                            {
                                if (i == 0 && j == 0 && k == 0) continue;

                                int nx = x + i;
                                int ny = y + j;
                                int nz = z + k;

                                if (nx >= 0 && nx < resolution && ny >= 0 && ny < resolution && nz >= 0 && nz < resolution)
                                {
                                    var neighbor = cubeGrid[nx, ny, nz];
                                    float distance = Vector3.Distance(current.position, neighbor.position);

                                    if (distance <= connectionRadius)
                                    {
                                        springs.Add(new Spring(current, neighbor, springStiffness, springDamping, transform, springLineMaterial));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void GenerateSpherePoints()
    {
        float r = radius;  
        float step = (2f * r) / (resolution - 1);
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

    void GenerateCylinderPoints()
    {
        float r = radius;
        float h = height;

        float stepXZ = (2f * r) / (resolution - 1); // step for X and Z
        float stepY = h / (resolution - 1);         // step for Y

        connectionRadius = Mathf.Max(stepXZ, stepY) * Mathf.Sqrt(3);
        Vector3 center = transform.position;

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    Vector3 offset = new Vector3(
                        x * stepXZ - r,
                        y * stepY - h / 2f,
                        z * stepXZ - r
                    );

                    // Only allow points within the vertical cylinder
                    Vector2 horizontal = new Vector2(offset.x, offset.z);
                    if (horizontal.magnitude <= r)
                    {
                        Vector3 worldPos = center + offset;
                        GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);

                        Vector3 parentScale = transform.lossyScale;
                        go.transform.localScale = new Vector3(
                            0.1f / parentScale.x,
                            0.1f / parentScale.y,
                            0.1f / parentScale.z
                        );

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

    void GenerateCapsulePoints()
    {
        float r = radius;
        float h = height;
        float cylinderHeight = h - 2f * r;
        if (cylinderHeight < 0f) cylinderHeight = 0f; // Prevent negative height

        float stepXZ = (2f * r) / (resolution - 1);      // horizontal step
        float stepY = h / (resolution - 1);              // vertical step
        connectionRadius = Mathf.Max(stepXZ, stepY) * Mathf.Sqrt(3);
        Vector3 center = transform.position;

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    float offsetY = y * stepY - h / 2f;
                    Vector3 offset = new Vector3(
                        x * stepXZ - r,
                        offsetY,
                        z * stepXZ - r
                    );

                    Vector2 horizontal = new Vector2(offset.x, offset.z);
                    float vert = offset.y;

                    // Inside middle cylinder section
                    bool insideCylinder = Mathf.Abs(vert) <= cylinderHeight / 2f && horizontal.sqrMagnitude <= r * r;

                    // Inside hemispherical caps
                    float capY = Mathf.Abs(vert) - cylinderHeight / 2f;
                    bool insideCaps = (capY >= 0) && (horizontal.sqrMagnitude + capY * capY <= r * r);

                    if (insideCylinder || insideCaps)
                    {
                        Vector3 worldPos = center + offset;
                        GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);

                        Vector3 parentScale = transform.lossyScale;
                        go.transform.localScale = new Vector3(
                            0.1f / parentScale.x,
                            0.1f / parentScale.y,
                            0.1f / parentScale.z
                        );

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

    public void GenerateMeshPoints(GameObject meshObject)
    {
        
        MeshFilter[] meshFilters = meshObject.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogError("No MeshFilters found!");
            return;
        }

        float minDistance = 0.01f;
        HashSet<Vector3> seen = new HashSet<Vector3>();

        foreach (MeshFilter mf in meshFilters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            foreach (Vector3 localPos in mesh.vertices)
            {
                Vector3 worldPos = mf.transform.TransformPoint(localPos);

                // Avoid duplicates
                bool isDuplicate = false;
                foreach (Vector3 existing in seen)
                {
                    if (Vector3.Distance(existing, worldPos) < minDistance)
                    {
                        isDuplicate = true;
                        break;
                    }
                }
                if (isDuplicate) continue;
                seen.Add(worldPos);

                GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
                go.transform.localScale = Vector3.one * 0.1f;

                var po = go.GetComponent<PhysicalObject>() ?? go.AddComponent<PhysicalObject>();
                var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                go.AddComponent<CollisionBody>();

                MassPoint mp = new MassPoint(worldPos, po);
                controller.Initialize(mp);
                allPoints.Add(mp);
            }
        }

        ConnectMeshSprings_KNN(k); // or 8, 10 depending on density

    }

    void ConnectMeshSprings_KNN(int k)
    {
        int n = allPoints.Count;

        for (int i = 0; i < n; i++)
        {
            MassPoint current = allPoints[i];
            List<(float, int)> distances = new List<(float, int)>();

            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                float dist = Vector3.Distance(current.position, allPoints[j].position);
                distances.Add((dist, j));
            }

            distances.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            for (int d = 0; d < Mathf.Min(k, distances.Count); d++)
            {
                int neighborIndex = distances[d].Item2;
                springs.Add(new Spring(current, allPoints[neighborIndex], springStiffness, springDamping, transform, springLineMaterial));
            }
        }

        Debug.Log($"Connected {springs.Count} springs using KNN (k = {k}) \n " +
            $"Mesh source: { meshSourceObject} \n " +
            $"Created {allPoints.Count} mass point \n " +
            $"next \n");

    }

    /*
    void ConnectMeshSprings()
    {
        connectionRadius = EstimateConnectionRadius(allPoints);
        for (int i = 0; i < allPoints.Count; i++)
        {
            for (int j = i + 1; j < allPoints.Count; j++)
            {
                if (allPoints[i] != allPoints[j] &&
                    Vector3.Distance(allPoints[i].position, allPoints[j].position) > 0.0001f &&
                    Vector3.Distance(allPoints[i].position, allPoints[j].position) <= connectionRadius)
                {
                    springs.Add(new Spring(allPoints[i], allPoints[j], springStiffness, springDamping, transform, springLineMaterial));
                }

            }
        }
        Debug.Log("Connected total springs: " + springs.Count);

    }
    float EstimateConnectionRadius(List<MassPoint> points)
    {
        float totalNearest = 0f;
        int count = 0;

        foreach (var p in points)
        {
            float nearest = float.MaxValue;
            foreach (var q in points)
            {
                if (p == q) continue;
                float dist = Vector3.Distance(p.position, q.position);
                if (dist < nearest) nearest = dist;
            }
            totalNearest += nearest;
            count++;
        }

        return (totalNearest / count) * 1.2f; // add a small buffer
    }
    */

}
