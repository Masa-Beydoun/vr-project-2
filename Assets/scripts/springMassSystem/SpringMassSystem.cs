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
    public bool isCreated = false, previousIsCreated = false;
    public float connectionRadius;
    public int resolution = 5;
    public float springStiffness = 500f;
    public float springDamping = 2f;
    public MassShapeType massShapeType = MassShapeType.Cube;

    public PhysicalObject physicalObject;
    
    public GameObject pointPrefab;
    public Material springLineMaterial;

    private MassPoint[,,] cubeGrid;
    private List<MassPoint> allPoints = new List<MassPoint>();
    private List<Spring> springs = new List<Spring>();

    [Header("Spring Settings")]
    public bool useCustomSpringProperties = true;
    public PhysicalMaterial materialPreset;

    [Header("Mesh Connection Settings")]
    public MeshConnectionMode meshConnectionMode = MeshConnectionMode.KNearestNeighbors;
    public int k=4;
    [Header("Voxel Settings")]
    public bool useVoxelFilling = true;

    [Header("Mesh Point Generation")]
    public MeshPointGenerationMode generationMode = MeshPointGenerationMode.UseMeshVertices;

    void Awake()
    {
        if (physicalObject == null)
            physicalObject = GetComponent<PhysicalObject>();
    }


    void Start()
    {
        //Debug.Log($"Start: isStatic = {physicalObject.isStatic}");
        //dragCoefficient = materialPreset != null ? materialPreset.dragCoefficient : 1f;
        if (isCreated)
        {
            CreateSystem();
        }
    }

    public void CreateSystem()
    {
        
        if (physicalObject == null)
        {
            Debug.LogError("PhysicalObject not assigned!");
            return;
        }
        if (!isCreated) return;
        massShapeType = physicalObject.massShapeType;

        allPoints.Clear();
        springs.Clear();
        if (!useCustomSpringProperties && materialPreset != null)
        {
            springStiffness = materialPreset.stiffness;
            springDamping = 2f; // Optional: you can add damping to the material class if needed
        }
        switch (massShapeType)
        {
            case MassShapeType.Cube:
                GenerateCubePoints();
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
                if (physicalObject.meshSourceObject != null)
                {
                    // Use a HashSet to avoid duplicate points
                    HashSet<MassPoint> uniquePoints = new HashSet<MassPoint>();
                    allPoints.Clear();

                    if (generationMode == MeshPointGenerationMode.UseMeshVertices ||
                        generationMode == MeshPointGenerationMode.MeshVerticesAndBounds ||
                        generationMode == MeshPointGenerationMode.MeshVerticesAndVolume)
                    {
                        GenerateMeshPoints(physicalObject.meshSourceObject, uniquePoints);
                    }

                    if (generationMode == MeshPointGenerationMode.FillUsingBounds ||
                        generationMode == MeshPointGenerationMode.MeshVerticesAndBounds)
                    {
                        VoxelFiller.FillUsingBounds(physicalObject.meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                    }

                    if (generationMode == MeshPointGenerationMode.FillUsingVolumeSampling ||
                        generationMode == MeshPointGenerationMode.MeshVerticesAndVolume)
                    {
                        VoxelFiller.FillUsingVolumeSampling(physicalObject.meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                    }

                    if (generationMode == MeshPointGenerationMode.FillUsingFloodFill)
                    {
                        VoxelFiller.FillUsingFloodFill(physicalObject.meshSourceObject, pointPrefab, transform, uniquePoints, resolution);

                    }
                    if (generationMode == MeshPointGenerationMode.FillUsingOctreeBasic)
                    {
                        VoxelFiller.FillUsingOctreeBasic(physicalObject.meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                    }
                    else if (generationMode == MeshPointGenerationMode.FillUsingOctreeAdvanced)
                    {
                        VoxelFiller.FillUsingOctreeAdvanced(physicalObject.meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                    }
                    else if (generationMode == MeshPointGenerationMode.FillUsingSDFDistanceOnly)
                    {
                        VoxelFiller.FillUsingSDFDistanceOnly(physicalObject.meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                    }

                    // Copy uniquePoints into allPoints list
                    allPoints = new List<MassPoint>(uniquePoints);

                    // Connect springs
                    if (meshConnectionMode == MeshConnectionMode.KNearestNeighbors)
                    {
                        ConnectMeshSprings_KNN(new HashSet<(int, int)>());
                        LogConnectionSummary("Accelerated KNN");
                    }
                    else if (meshConnectionMode == MeshConnectionMode.TriangleEdges)
                    {
                        var meshFilters = physicalObject.meshSourceObject.GetComponentsInChildren<MeshFilter>();
                        ConnectMeshSprings_Triangles(meshFilters, new HashSet<(int, int)>());
                        LogConnectionSummary("Triangle Edges");
                    }
                    else if (meshConnectionMode == MeshConnectionMode.Hybrid)
                    {
                        ConnectMeshSprings_Hybrid();
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
        if (allPoints.Count > 0 && physicalObject != null)
        {
            float massPerPoint = physicalObject.mass / allPoints.Count;
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
        CreateBoundingDrawer(); // One-time drawer object
        UpdateBoundingShape();  // Draw the initial shape


    }

    void LogConnectionSummary(string algorithmName)
    {
        Debug.Log($"Object: {gameObject.name} | Algorithm used: {algorithmName} | " +
                  $"Mass points: {allPoints.Count} | Springs: {springs.Count}");
    }

    void Update()
    {
        if (isCreated && !previousIsCreated)
        {
            CreateSystem();
        }
        previousIsCreated = isCreated;
        #if UNITY_EDITOR
            UpdateBoundingShape(); // Update drawer position + size
        #endif
    }

    void FixedUpdate()
    {
        if (!isCreated) return;
        if (physicalObject.isStatic) return;
        float dt = Time.fixedDeltaTime;
        Vector3 gravity = SimulationEnvironment.Instance.GetGravity();

        foreach (var p in allPoints)
        {
            p.ApplyForce(gravity * p.mass, dt);
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
    static readonly Vector3Int[] StructuralOffsets = {
    new Vector3Int(1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, 0, 1),
    new Vector3Int(-1, 0, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 0, -1),
};

    static readonly Vector3Int[] ShearOffsets = {
    new Vector3Int(1, 1, 0), new Vector3Int(1, -1, 0), new Vector3Int(-1, 1, 0), new Vector3Int(-1, -1, 0),
    new Vector3Int(0, 1, 1), new Vector3Int(0, -1, 1), new Vector3Int(0, 1, -1), new Vector3Int(0, -1, -1),
    new Vector3Int(1, 0, 1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, -1), new Vector3Int(-1, 0, -1)
};

    static readonly Vector3Int[] BendOffsets = {
    new Vector3Int(2, 0, 0), new Vector3Int(0, 2, 0), new Vector3Int(0, 0, 2),
    new Vector3Int(-2, 0, 0), new Vector3Int(0, -2, 0), new Vector3Int(0, 0, -2)
};

    void GenerateCubePoints()
    {
        int pointsX = Mathf.CeilToInt(physicalObject.width * resolution) + 1;
        int pointsY = Mathf.CeilToInt(physicalObject.height * resolution) + 1;
        int pointsZ = Mathf.CeilToInt(physicalObject.depth * resolution) + 1;

        float dx = physicalObject.width / (pointsX - 1);
        float dy = physicalObject.height / (pointsY - 1);
        float dz = physicalObject.depth / (pointsZ - 1);

        Vector3 origin = transform.position - new Vector3(physicalObject.width, physicalObject.height, physicalObject.depth) / 2f;

        Quaternion rotation = Quaternion.Euler(physicalObject.rotationEuler);

        cubeGrid = new MassPoint[pointsX, pointsY, pointsZ];

        for (int x = 0; x < pointsX; x++)
        {
            for (int y = 0; y < pointsY; y++)
            {
                for (int z = 0; z < pointsZ; z++)
                {
                    Vector3 localPos = new Vector3(x * dx, y * dy, z * dz);
                    Vector3 worldPos = origin + rotation * localPos;

                    GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
                    go.transform.localScale = Vector3.one * 0.05f;

                    var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                    string source = gameObject.name;
                    MassPoint mp = new MassPoint(worldPos, physicalObject, source);
                    controller.Initialize(mp);
                    cubeGrid[x, y, z] = mp;
                    allPoints.Add(mp);
                }
            }
        }

        Debug.Log($"Generated {allPoints.Count} cube mass points with resolution {resolution} points/unit");
        ConnectCubeSprings(pointsX, pointsY, pointsZ, dx, dy, dz);

    }

    void ConnectCubeSprings(int pointsX, int pointsY, int pointsZ, float dx, float dy, float dz)
    {
        AddSpringsWithOffsets(pointsX, pointsY, pointsZ, StructuralOffsets);
        AddSpringsWithOffsets(pointsX, pointsY, pointsZ, ShearOffsets);
        AddSpringsWithOffsets(pointsX, pointsY, pointsZ, BendOffsets);
    }

    void AddSpringsWithOffsets(int pointsX, int pointsY, int pointsZ, Vector3Int[] offsets)
    {
        for (int x = 0; x < pointsX; x++)
        {
            for (int y = 0; y < pointsY; y++)
            {
                for (int z = 0; z < pointsZ; z++)
                {
                    var current = cubeGrid[x, y, z];

                    foreach (var offset in offsets)
                    {
                        int nx = x + offset.x;
                        int ny = y + offset.y;
                        int nz = z + offset.z;

                        if (nx >= 0 && nx < pointsX && ny >= 0 && ny < pointsY && nz >= 0 && nz < pointsZ)
                        {
                            var neighbor = cubeGrid[nx, ny, nz];
                            springs.Add(new Spring(current, neighbor, springStiffness, springDamping, transform, springLineMaterial));
                        }
                    }
                }
            }
        }
    }


    void GenerateSpherePoints()
    {
        float r = physicalObject.radius;
        float diameter = 2f * r;
        int steps = Mathf.Max(2, Mathf.RoundToInt(diameter * resolution));
        float step = diameter / (steps - 1);
        connectionRadius = step * Mathf.Sqrt(3);
        Vector3 center = transform.position;

        for (int x = 0; x < steps; x++)
        {
            for (int y = 0; y < steps; y++)
            {
                for (int z = 0; z < steps; z++)
                {
                    Vector3 offset = new Vector3(x * step - r, y * step - r, z * step - r);
                    if (offset.magnitude <= r)
                    {
                        Vector3 worldPos = center + offset;
                        GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
                        go.transform.localScale = Vector3.one * 0.05f;

                        var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                        string source = gameObject.name;
                        MassPoint mp = new MassPoint(worldPos, physicalObject, source);
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
        float r = physicalObject.radius;
        float h = physicalObject.height;

        int stepsXZ = Mathf.Max(2, Mathf.RoundToInt(2f * r * resolution));
        int stepsY = Mathf.Max(2, Mathf.RoundToInt(h * resolution));

        float stepXZ = (2f * r) / (stepsXZ - 1);
        float stepY = h / (stepsY - 1);

        connectionRadius = Mathf.Max(stepXZ, stepY) * Mathf.Sqrt(3);
        Vector3 center = transform.position;

        for (int x = 0; x < stepsXZ; x++)
        {
            for (int y = 0; y < stepsY; y++)
            {
                for (int z = 0; z < stepsXZ; z++)
                {
                    Vector3 offset = new Vector3(
                        x * stepXZ - r,
                        y * stepY - h / 2f,
                        z * stepXZ - r
                    );

                    Vector2 horizontal = new Vector2(offset.x, offset.z);
                    if (horizontal.magnitude <= r)
                    {
                        Vector3 worldPos = center + offset;
                        GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
                        go.transform.localScale = Vector3.one * 0.05f;

                        var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                        string source = gameObject.name;
                        MassPoint mp = new MassPoint(worldPos, physicalObject, source);
                        controller.Initialize(mp);
                        allPoints.Add(mp);
                    }
                }
            }
        }
    }

    void GenerateCapsulePoints()
    {
        float r = physicalObject.radius;
        float h = physicalObject.height;
        float cylinderHeight = Mathf.Max(0f, h - 2f * r);

        int stepsXZ = Mathf.Max(2, Mathf.RoundToInt(2f * r * resolution));
        int stepsY = Mathf.Max(2, Mathf.RoundToInt(h * resolution));

        float stepXZ = (2f * r) / (stepsXZ - 1);
        float stepY = h / (stepsY - 1);

        connectionRadius = Mathf.Max(stepXZ, stepY) * Mathf.Sqrt(3);
        Vector3 center = transform.position;

        for (int x = 0; x < stepsXZ; x++)
        {
            for (int y = 0; y < stepsY; y++)
            {
                for (int z = 0; z < stepsXZ; z++)
                {
                    float offsetY = y * stepY - h / 2f;
                    Vector3 offset = new Vector3(
                        x * stepXZ - r,
                        offsetY,
                        z * stepXZ - r
                    );

                    Vector2 horizontal = new Vector2(offset.x, offset.z);
                    float vert = offset.y;

                    bool insideCylinder = Mathf.Abs(vert) <= cylinderHeight / 2f && horizontal.sqrMagnitude <= r * r;
                    float capY = Mathf.Abs(vert) - cylinderHeight / 2f;
                    bool insideCaps = capY >= 0 && (horizontal.sqrMagnitude + capY * capY <= r * r);

                    if (insideCylinder || insideCaps)
                    {
                        Vector3 worldPos = center + offset;
                        GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
                        go.transform.localScale = Vector3.one * 0.05f;

                        var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                        string source = gameObject.name;
                        MassPoint mp = new MassPoint(worldPos, physicalObject, source);
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
                string source = gameObject.name;
                MassPoint candidate = new MassPoint(worldPos, physicalObject, source);
                if (uniquePoints.Contains(candidate)) continue;


                GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
                go.transform.localScale = Vector3.one * 0.1f;

                var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                //go.AddComponent<CollisionBody>();

                MassPoint mp = new MassPoint(worldPos, physicalObject, source);
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
            MassPoint p1 = FindClosestMassPoint(a);
            MassPoint p2 = FindClosestMassPoint(b);
            if (p1 == null || p2 == null) return;


            int i1 = allPoints.IndexOf(p1);
            int i2 = allPoints.IndexOf(p2);
            if (i1 == -1 || i2 == -1) return;

            var key = (Mathf.Min(i1, i2), Mathf.Max(i1, i2));
            if (connectedPairs.Contains(key)) return;

            springs.Add(new Spring(p1, p2, springStiffness, springDamping, transform, springLineMaterial));
            connectedPairs.Add(key);
        }


    }

    void ConnectMeshSprings_Hybrid()
    {
        springs.Clear();
        var connectedPairs = new HashSet<(int, int)>();

        // Surface springs
        MeshFilter[] meshFilters = physicalObject.meshSourceObject.GetComponentsInChildren<MeshFilter>();
        ConnectMeshSprings_Triangles(meshFilters, connectedPairs);

        // Volume/internal springs
        ConnectMeshSprings_KNN(connectedPairs);
    }

    void ConnectMeshSprings_KNN(HashSet<(int, int)> connectedPairs)
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

    MassPoint FindClosestMassPoint(Vector3 pos, float maxDistance = 0.01f)
    {
        MassPoint closest = null;
        float minDist = float.MaxValue;
        foreach (var p in allPoints)
        {
            float dist = Vector3.Distance(p.position, pos);
            if (dist < minDist && dist <= maxDistance)
            {
                minDist = dist;
                closest = p;
            }
        }
        return closest;
    }



    void CreateBoundingDrawer()
    {
#if UNITY_EDITOR
    if (allPoints.Count == 0) return;

    GameObject box = new GameObject("SpringMass_BoundingShape");
    box.transform.SetParent(transform);

    var boxDrawer = box.AddComponent<BoundingBoxDrawer>();
    physicalObject.boundingBoxDrawer = boxDrawer; // ✅ Save reference
#endif
    }




    void UpdateBoundingShape()
    {
#if UNITY_EDITOR
    if (allPoints.Count == 0 || physicalObject.boundingBoxDrawer == null) return;

    var boxDrawer = physicalObject.boundingBoxDrawer;

    if (physicalObject.shapeType == BoundingShapeType.Sphere)
    {
        Vector3 center = Vector3.zero;
        foreach (var p in allPoints)
            center += p.position;
        center /= allPoints.Count;

        float radius = 0f;
        foreach (var p in allPoints)
            radius = Mathf.Max(radius, Vector3.Distance(center, p.position));

        boxDrawer.transform.position = center;
        boxDrawer.size = Vector3.one * radius * 2f;
        boxDrawer.isSphere = true;

        physicalObject.boundingShapeType = BoundingShapeType.Sphere;
        physicalObject.boundingSphere = new BoundingSphere { center = center, radius = radius };
        physicalObject.boundingBox = null;
    }
    else
    {
        Vector3 min = allPoints[0].position;
        Vector3 max = allPoints[0].position;

        foreach (var p in allPoints)
        {
            min = Vector3.Min(min, p.position);
            max = Vector3.Max(max, p.position);
        }

        Vector3 center = (min + max) / 2f;
        Vector3 size = max - min;

        boxDrawer.transform.position = center;
        boxDrawer.size = size;
        boxDrawer.isSphere = false;

        physicalObject.boundingShapeType = BoundingShapeType.AABB;
        physicalObject.boundingBox = new Bounds(center, size);
        physicalObject.boundingSphere = null;
    }
#endif
    }

    public List<Vector3> GetPositions()
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (var p in allPoints)
            positions.Add(p.position);
        return positions;
    }

    public List<MassPoint> GetMassPoints()
    {
        return allPoints;
    }

}
