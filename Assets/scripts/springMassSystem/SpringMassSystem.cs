using System.Collections.Generic;
using UnityEngine;


public enum SpringParameterMode
{
    Manual,
    UseMaterial
}
public enum MeshPointGenerationMode
{
    UseMeshVertices,
    FillUsingBounds,
    FillUsingVolumeSampling,
    MeshVerticesAndBounds,
    MeshVerticesAndVolume,
    FillUsingFloodFill,
    FillUsingOctreeBasic,
    FillUsingOctreeAdvanced,
    FillUsingSDFDistanceOnly
}

public enum MeshConnectionMode
{
    KNearestNeighbors,
    TriangleEdges,
    Hybrid
}


public class SpringMassSystem : MonoBehaviour
{
<<<<<<< HEAD:Assets/scripts/springMassSystem/SpringMassSystem.cs

    [HideInInspector] public PhysicalObject physicalObject;
    [HideInInspector] public float width, height, depth, radius;
    [HideInInspector] public int resolution;
    [HideInInspector] public GameObject meshSourceObject;
    [HideInInspector] public GameObject pointPrefab;
    [HideInInspector] public Material springLineMaterial;

    [HideInInspector] public float springStiffness = 500f;
    [HideInInspector] public float springDamping = 2f;
    
=======
    public int resolution = 5;
    public float springStiffness = 500f;
    public float springDamping = 2f;
    public GameObject pointPrefab;
    public Material springLineMaterial;
>>>>>>> mass-temp:Assets/scripts/springMassSystem/SpringMassObject.cs

    private MassPoint[,,] cubeGrid;
    private List<MassPoint> allPoints = new List<MassPoint>();
    private List<Spring> springs = new List<Spring>();

    private float connectionRadius;

<<<<<<< HEAD:Assets/scripts/springMassSystem/SpringMassSystem.cs
    [HideInInspector]  public int k;

    //[Header("Spring Settings")]
    [HideInInspector] public bool useCustomSpringProperties = true;
    [HideInInspector] public PhysicalMaterial materialPreset;
=======
    public PhysicalObject prototypePhysicalObject;

    public int k;

    [Header("Spring Settings")]
    public bool useCustomSpringProperties = true;
    public PhysicalMaterial materialPreset;
>>>>>>> mass-temp:Assets/scripts/springMassSystem/SpringMassObject.cs


    //[Header("Mesh Connection Settings")]
    [HideInInspector]  public MeshConnectionMode meshConnectionMode = MeshConnectionMode.KNearestNeighbors;


    //[Header("Voxel Settings")]
    [HideInInspector] public bool useVoxelFilling = true;

    //[Header("Mesh Point Generation")]
    [HideInInspector] public MeshPointGenerationMode generationMode = MeshPointGenerationMode.UseMeshVertices;


<<<<<<< HEAD:Assets/scripts/springMassSystem/SpringMassSystem.cs
    public void InitializeFromPhysicalObject()
=======
    public bool isCreated = false;
    private bool previousIsCreated = false;
    private bool isStatic=false;
    private float mass;
    private float radius;
    private float height;
    private float width;
    private float depth;
    private GameObject meshSourceObject;
    [HideInInspector]
    public MassShapeType massShapeType;


    void Start()
>>>>>>> mass-temp:Assets/scripts/springMassSystem/SpringMassObject.cs
    {
        if (isCreated)
        {
            CreateSystem();
        }
    }

<<<<<<< HEAD:Assets/scripts/springMassSystem/SpringMassSystem.cs
        if (physicalObject == null)
        {
            Debug.LogError("PhysicalObject not assigned!");
            return;
        }

        width = physicalObject.width;
        height = physicalObject.height;
        depth = physicalObject.depth;
        radius = physicalObject.radius;
        resolution = physicalObject.resolution;
        meshSourceObject = physicalObject.meshSource;
        pointPrefab = Resources.Load<GameObject>("PointPrefab");
        springLineMaterial = Resources.Load<Material>("SpringLineMaterial");
        springStiffness = physicalObject.springStiffness;
        springDamping = physicalObject.springDamping;

        k = physicalObject.k;

=======

    public void CreateSystem()
    {
        if (!isCreated) return;
        PhysicalObject po = GetComponent<PhysicalObject>();
        if (po != null)
        {
            radius = po.radius;
            height = po.height;
            width = po.width;
            depth = po.depth;
            meshSourceObject = po.meshSourceObject;
            massShapeType = po.massShapeType;
            isStatic = po.isStatic;
            mass = po.mass;
        }
        else
        {
            Debug.LogWarning("No PhysicalObject found on the same GameObject.");
        }

        allPoints.Clear();
        springs.Clear();
>>>>>>> mass-temp:Assets/scripts/springMassSystem/SpringMassObject.cs
        if (!useCustomSpringProperties && materialPreset != null)
        {
            springStiffness = materialPreset.stiffness;
            springDamping = 2f; // Optional: you can add damping to the material class if needed
        }
<<<<<<< HEAD:Assets/scripts/springMassSystem/SpringMassSystem.cs
        switch (physicalObject.massShapeType)
=======
        switch (massShapeType)
>>>>>>> mass-temp:Assets/scripts/springMassSystem/SpringMassObject.cs
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
                    // Use a HashSet to avoid duplicate points
                    HashSet<MassPoint> uniquePoints = new HashSet<MassPoint>();
                    allPoints.Clear();

                    if (generationMode == MeshPointGenerationMode.UseMeshVertices ||
                        generationMode == MeshPointGenerationMode.MeshVerticesAndBounds ||
                        generationMode == MeshPointGenerationMode.MeshVerticesAndVolume)
                    {
                        GenerateMeshPoints(meshSourceObject, uniquePoints);
                    }

                    if (generationMode == MeshPointGenerationMode.FillUsingBounds ||
                        generationMode == MeshPointGenerationMode.MeshVerticesAndBounds)
                    {
                        VoxelFiller.FillUsingBounds(meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                    }

                    if (generationMode == MeshPointGenerationMode.FillUsingVolumeSampling ||
                        generationMode == MeshPointGenerationMode.MeshVerticesAndVolume)
                    {
                        VoxelFiller.FillUsingVolumeSampling(meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                    }

                    if (generationMode == MeshPointGenerationMode.FillUsingFloodFill)
                    {
                        VoxelFiller.FillUsingFloodFill(meshSourceObject, pointPrefab, transform, uniquePoints, resolution);

                    }
                    if (generationMode == MeshPointGenerationMode.FillUsingOctreeBasic)
                    {
                        VoxelFiller.FillUsingOctreeBasic(meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                    }
                    else if (generationMode == MeshPointGenerationMode.FillUsingOctreeAdvanced)
                    {
                        VoxelFiller.FillUsingOctreeAdvanced(meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                    }
                    else if (generationMode == MeshPointGenerationMode.FillUsingSDFDistanceOnly)
                    {
                        VoxelFiller.FillUsingSDFDistanceOnly(meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                    }

                    // Copy uniquePoints into allPoints list
                    allPoints = new List<MassPoint>(uniquePoints);

                    // Connect springs
                    if (meshConnectionMode == MeshConnectionMode.KNearestNeighbors)
                    {
                        ConnectMeshSprings_KNN(k, new HashSet<(int, int)>());
                        LogConnectionSummary("Accelerated KNN");
                    }
                    else if (meshConnectionMode == MeshConnectionMode.TriangleEdges)
                    {
                        var meshFilters = meshSourceObject.GetComponentsInChildren<MeshFilter>();
                        ConnectMeshSprings_Triangles(meshFilters, new HashSet<(int, int)>());
                        LogConnectionSummary("Triangle Edges");
                    }
                    else if (meshConnectionMode == MeshConnectionMode.Hybrid)
                    {
                        ConnectMeshSprings_Hybrid(k);
                        LogConnectionSummary("Hybrid (Triangles + KNN)");
                    }
                }
                else
                {
                    Debug.LogError("You selected 'Other' but did not assign a meshSourceObject.");
                }
                break;
        }
        // Distribute mass equally across all points
        if (allPoints.Count > 0 && prototypePhysicalObject != null)
        {
            float massPerPoint = mass / allPoints.Count;
            foreach (var mp in allPoints)
            {
                if (mp.physicalObject != null)
                {
                    mp.physicalObject.mass = massPerPoint;
                }
            }
        }

        foreach (var s in springs)
            s.UpdateLine();

    }

    void LogConnectionSummary(string algorithmName)
    {
        Debug.Log($"Object: {gameObject.name} | Algorithm used: {algorithmName} | " +
                  $"Mass points: {allPoints.Count} | Springs: {springs.Count}");
    }
    void Update()
    {
        PhysicalObject po = GetComponent<PhysicalObject>();
        if (po != null){
            isStatic = po.isStatic;
        }
        if (isCreated && !previousIsCreated)
        {
            CreateSystem();
        }
        previousIsCreated = isCreated;
    }

    void FixedUpdate()
    {
        if (isStatic) return;
        float dt = Time.fixedDeltaTime;
        Vector3 gravity = SimulationEnvironment.Instance.GetGravity();

        foreach (var p in allPoints)
        {
            p.ApplyForce(gravity * p.mass, dt);
            Debug.Log(" gravity " + (gravity * p.mass, dt));
        }
        foreach (var s in springs)
            s.ApplyForce(dt);

        foreach (var p in allPoints)
        {
            p.Integrate(dt);
            if (p.physicalObject != null && !p.physicalObject.isStatic)
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

        Debug.Log($"[Cube] Connection Radius: {connectionRadius}, dx={dx}, dy={dy}, dz={dz}");

        cubeGrid = new MassPoint[resolution, resolution, resolution];

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    Debug.Log("points");

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

    void GenerateMeshPoints(GameObject meshObject, HashSet<MassPoint> uniquePoints)
    {
        MeshFilter[] meshFilters = meshObject.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogError("No MeshFilters found!");
            return;
        }

        float minDistance = 0.01f;
        DuplicateDetector detector = new DuplicateDetector(minDistance);

        foreach (MeshFilter mf in meshFilters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            foreach (Vector3 localPos in mesh.vertices)
            {
                Vector3 worldPos = mf.transform.TransformPoint(localPos);

                // Skip duplicates based on HashSet
                MassPoint candidate = new MassPoint(worldPos, null);
                if (uniquePoints.Contains(candidate)) continue;

                GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
                go.transform.localScale = Vector3.one * 0.1f;

                var po = go.GetComponent<PhysicalObject>() ?? go.AddComponent<PhysicalObject>();
                var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                go.AddComponent<CollisionBody>();

                MassPoint mp = new MassPoint(worldPos, po);
                controller.Initialize(mp);

                uniquePoints.Add(mp);
            }
        }
    }

    void ConnectMeshSprings_Triangles(MeshFilter[] meshFilters, HashSet<(int, int)> connectedPairs)

    {
        Dictionary<Vector3, MassPoint> pointLookup = new Dictionary<Vector3, MassPoint>();
        foreach (var mp in allPoints)
        {
            pointLookup[mp.position] = mp;
        }

        //int connections = 0;

        foreach (MeshFilter mf in meshFilters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = mf.transform.TransformPoint(vertices[triangles[i]]);
                Vector3 v1 = mf.transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v2 = mf.transform.TransformPoint(vertices[triangles[i + 2]]);

                TryAddSpring(v0, v1);
                TryAddSpring(v1, v2);
                TryAddSpring(v2, v0);
            }
        }

        void TryAddSpring(Vector3 a, Vector3 b)
        {
            if (!pointLookup.ContainsKey(a) || !pointLookup.ContainsKey(b)) return;

            MassPoint p1 = pointLookup[a];
            MassPoint p2 = pointLookup[b];

            int i1 = allPoints.IndexOf(p1);
            int i2 = allPoints.IndexOf(p2);
            if (i1 == -1 || i2 == -1) return;

            var key = (Mathf.Min(i1, i2), Mathf.Max(i1, i2));
            if (connectedPairs.Contains(key)) return;

            springs.Add(new Spring(p1, p2, springStiffness, springDamping, transform, springLineMaterial));
            connectedPairs.Add(key);
        }


    }

    void ConnectMeshSprings_Hybrid(int k)
    {
        springs.Clear();
        var connectedPairs = new HashSet<(int, int)>();

        // Surface springs
        MeshFilter[] meshFilters = meshSourceObject.GetComponentsInChildren<MeshFilter>();
        ConnectMeshSprings_Triangles(meshFilters, connectedPairs);

        // Volume/internal springs
        ConnectMeshSprings_KNN(k, connectedPairs);
    }

    void ConnectMeshSprings_KNN(int k, HashSet<(int, int)> connectedPairs)
    {
        int n = allPoints.Count;
        if (n == 0) return;

        // Estimate cell size roughly as average nearest distance
        float cellSize = EstimateConnectionRadius(allPoints);

        SpatialGrid grid = new SpatialGrid(cellSize);

        // Insert points into grid
        for (int i = 0; i < n; i++)
        {
            grid.AddPoint(allPoints[i].position, i);
        }

        for (int i = 0; i < n; i++)
        {
            var current = allPoints[i];
            List<(float dist, int idx)> candidates = new List<(float, int)>();

            var neighborIndices = grid.GetNeighbors(current.position);

            foreach (int j in neighborIndices)
            {
                if (i == j) continue;
                float dist = Vector3.Distance(current.position, allPoints[j].position);
                candidates.Add((dist, j));
            }

            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

            for (int c = 0; c < Mathf.Min(k, candidates.Count); c++)
            {
                int neighborIdx = candidates[c].idx;
                int minIdx = Mathf.Min(i, neighborIdx);
                int maxIdx = Mathf.Max(i, neighborIdx);

                if (!connectedPairs.Contains((minIdx, maxIdx)))
                {
                    springs.Add(new Spring(current, allPoints[neighborIdx], springStiffness, springDamping, transform, springLineMaterial));
                    connectedPairs.Add((minIdx, maxIdx));
                }
            }
        }


    }

    // Estimate average nearest neighbor distance (same as in your commented code)
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

        return (totalNearest / count) * 1.2f; // small buffer
    }


}
