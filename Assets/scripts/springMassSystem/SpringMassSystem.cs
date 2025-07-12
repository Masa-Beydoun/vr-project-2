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
    MeshVerticesAndBounds,

    FillUsingFloodFill,
    MeshVerticesAndFloodFill,

    FillUsingOctreeBasic,
    MeshVerticesAndOctreeBasic,

    FillUsingOctreeAdvanced,
    MeshVerticesAndOctreeAdvanced,

    FillUsingSDFDistanceOnly,
    MeshVerticesAndSDFDistanceOnly
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

    [HideInInspector] public MassPoint[,,] cubeGrid;
    [HideInInspector] public List<MassPoint> allPoints = new List<MassPoint>();
    [HideInInspector] public List<Spring> springs = new List<Spring>();

    [Header("Mesh Connection Settings")]
    public MeshConnectionMode meshConnectionMode = MeshConnectionMode.KNearestNeighbors;
    public int k=4;
    [Header("Voxel Settings")]
    public bool useVoxelFilling = true;

    [Header("Mesh Point Generation")]
    public MeshPointGenerationMode generationMode = MeshPointGenerationMode.UseMeshVertices;

    // Center of mass tracking
    private Vector3 centerOfMass;
    private Vector3 previousCenterOfMass;


    void Awake()
    {
        if (physicalObject == null)
            physicalObject = GetComponent<PhysicalObject>();
    }
    void Start()
    {
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
                    GenerateAndConnectMeshPoints();
                else
                    Debug.LogError("You selected 'Other' but did not assign a meshSourceObject.");
                break;
        }
        DistributeMassEqually();
        UpdateSpringsAndBounds();
    }
    private void GenerateAndConnectMeshPoints()
    {
        HashSet<MassPoint> uniquePoints = new HashSet<MassPoint>();
        bool flag = generationMode == MeshPointGenerationMode.UseMeshVertices ||
           generationMode == MeshPointGenerationMode.MeshVerticesAndBounds ||
           generationMode == MeshPointGenerationMode.MeshVerticesAndFloodFill ||
           generationMode == MeshPointGenerationMode.MeshVerticesAndOctreeBasic ||
           generationMode == MeshPointGenerationMode.MeshVerticesAndOctreeAdvanced ||
           generationMode == MeshPointGenerationMode.MeshVerticesAndSDFDistanceOnly;
        // Generate mesh vertices if required
        if (flag)
        {
            GenerateMeshPoints(physicalObject.meshSourceObject, uniquePoints);
        }

        // Apply fill strategy (remove duplicate calls)
        ApplyFillStrategy(uniquePoints);

        // Transfer to allPoints
        TransferUniquePointsToAllPoints(uniquePoints);

        // Connect springs
        ConnectMeshSprings();
    }
    private void ApplyFillStrategy(HashSet<MassPoint> uniquePoints)
    {
        Debug.Log("filling");
        switch (generationMode)
        {
            case MeshPointGenerationMode.FillUsingBounds:
            case MeshPointGenerationMode.MeshVerticesAndBounds:
                VoxelFiller.FillUsingBounds(physicalObject.meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                break;

            case MeshPointGenerationMode.FillUsingFloodFill:
            case MeshPointGenerationMode.MeshVerticesAndFloodFill:
                VoxelFiller.FillUsingFloodFill(physicalObject.meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                break;

            case MeshPointGenerationMode.FillUsingOctreeBasic:
            case MeshPointGenerationMode.MeshVerticesAndOctreeBasic:
                VoxelFiller.FillUsingOctreeBasic(physicalObject.meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                break;

            case MeshPointGenerationMode.FillUsingOctreeAdvanced:
            case MeshPointGenerationMode.MeshVerticesAndOctreeAdvanced:
                VoxelFiller.FillUsingOctreeAdvanced(physicalObject.meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                break;

            case MeshPointGenerationMode.FillUsingSDFDistanceOnly:
            case MeshPointGenerationMode.MeshVerticesAndSDFDistanceOnly:
                VoxelFiller.FillUsingSDFDistanceOnly(physicalObject.meshSourceObject, pointPrefab, transform, uniquePoints, resolution);
                break;
        }
    }
    // 5. Extract mesh spring connection logic
    private void ConnectMeshSprings()
    {
        switch (meshConnectionMode)
        {
            case MeshConnectionMode.KNearestNeighbors:
                ConnectMeshSprings_KNN(new HashSet<(int, int)>());
                break;

            case MeshConnectionMode.TriangleEdges:
                var connectedPairs = new HashSet<(int, int)>();
                MeshFilter[] meshFilters = physicalObject.meshSourceObject.GetComponentsInChildren<MeshFilter>();
                ConnectMeshSprings_Triangles(meshFilters, connectedPairs);
                break;

            case MeshConnectionMode.Hybrid:
                ConnectMeshSprings_Hybrid();
                break;
        }
    }
    // 6. Extract mass distribution logic
    private void DistributeMassEqually()
    {
        if (allPoints.Count > 0 && physicalObject != null)
        {
            float massPerPoint = physicalObject.mass / allPoints.Count;
            foreach (var mp in allPoints)
            {
                mp.mass = massPerPoint;
            }
        }
    }
    // 7. Extract spring and bounds update logic
    private void UpdateSpringsAndBounds()
    {
        foreach (var s in springs)
            s.UpdateLine();

        CreateBoundingDrawer();
        UpdateBoundingShape();
    }
    // 8. Add method to clear existing system
    private void ClearExistingSystem()
    {
        // Clear existing GameObjects
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name.Contains("Point") || child.name.Contains("SpringMass"))
            {
                if (Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
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

        // Apply gravity to all mass points
        foreach (var p in allPoints)
        {
            p.ApplyForce(gravity * p.mass, dt);
        }

        // Apply spring forces
        foreach (var s in springs)
            s.ApplyForce(dt);

        // Integrate all mass points
        foreach (var p in allPoints)
        {
            p.Integrate(dt);
        }

        // Update center of mass and move the main object
        CalculateCenterOfMass();
        Vector3 centerOfMassMovement = centerOfMass - previousCenterOfMass;


        foreach (var p in allPoints)
        {
            if (p.physicalObject != null && !p.physicalObject.isStatic)
            {
                // Don't update the main object's position here - it's handled above
                var controller = p.physicalObject.GetComponent<MassPointController>();
                if (controller != null)
                {
                    controller.transform.position = p.position;
                }
            }
        }

        previousCenterOfMass = centerOfMass;

        // Update spring line renderers
        foreach (var s in springs)
            s.UpdateLine();
    }

    private void CalculateCenterOfMass()
    {
        if (allPoints.Count == 0) return;

        Vector3 totalMass = Vector3.zero;
        float totalMassValue = 0f;

        foreach (var p in allPoints)
        {
            totalMass += p.position * p.mass;
            totalMassValue += p.mass;
        }

        centerOfMass = totalMass / totalMassValue;
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
                            Spring s = new Spring(current, neighbor, springStiffness, springDamping, transform, springLineMaterial);
                            springs.Add(s);
                            current.connectedSprings.Add(s);
                            neighbor.connectedSprings.Add(s);
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
                    Spring s = new Spring(allPoints[i], allPoints[j], springStiffness, springDamping, transform, springLineMaterial);
                    springs.Add(s);
                    allPoints[i].connectedSprings.Add(s);
                    allPoints[j].connectedSprings.Add(s);
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

    // 9. Improved GenerateMeshPoints with better duplicate detection
    void GenerateMeshPoints(GameObject meshObject, HashSet<MassPoint> uniquePoints)
    {
        MeshFilter[] meshFilters = meshObject.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogError("No MeshFilters found!");
            return;
        }

        float minDistance = 1f / resolution; // Use resolution-based distance

        foreach (MeshFilter mf in meshFilters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            foreach (Vector3 localPos in mesh.vertices)
            {
                Vector3 worldPos = mf.transform.TransformPoint(localPos);

                // Check if point is too close to existing points
                bool tooClose = false;
                foreach (var existingPoint in uniquePoints)
                {
                    if (Vector3.Distance(existingPoint.position, worldPos) < minDistance)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                GameObject go = Instantiate(pointPrefab, worldPos, Quaternion.identity, transform);
                go.transform.localScale = Vector3.one * 0.1f;
                go.name = $"MeshPoint_{uniquePoints.Count}";

                var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                string source = gameObject.name;
                MassPoint mp = new MassPoint(worldPos, physicalObject, source);
                controller.Initialize(mp);

                uniquePoints.Add(mp);
            }
        }

        Debug.Log($"Generated {uniquePoints.Count} mesh vertex points");
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

            Spring s = new Spring(p1, p2, springStiffness, springDamping, transform, springLineMaterial);
            springs.Add(s);
            p1.connectedSprings.Add(s);
            p2.connectedSprings.Add(s);
            connectedPairs.Add(key);
        }


    }

    void ConnectMeshSprings_Hybrid()
    {
        springs.Clear();
        var connectedPairs = new HashSet<(int, int)>();

        MeshFilter[] meshFilters = physicalObject.meshSourceObject.GetComponentsInChildren<MeshFilter>();
        ConnectMeshSprings_Triangles(meshFilters, connectedPairs);

        ConnectMeshSprings_KNN(connectedPairs);
    }

    // 10. Improved ConnectMeshSprings_KNN with better spatial partitioning
    void ConnectMeshSprings_KNN(HashSet<(int, int)> connectedPairs)
    {
        int n = allPoints.Count;
        if (n == 0) return;

        float cellSize = EstimateConnectionRadius(allPoints);
        SpatialGrid grid = new SpatialGrid(cellSize);

        // Insert points into grid
        for (int i = 0; i < n; i++)
        {
            grid.AddPoint(allPoints[i].position, i);
        }

        int totalConnections = 0;

        for (int i = 0; i < n; i++)
        {
            var current = allPoints[i];
            var candidates = new List<(float dist, int idx)>();

            var neighborIndices = grid.GetNeighbors(current.position);

            foreach (int j in neighborIndices)
            {
                if (i == j) continue;
                float dist = Vector3.Distance(current.position, allPoints[j].position);
                candidates.Add((dist, j));
            }

            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

            int connectionsForThisPoint = 0;
            for (int c = 0; c < Mathf.Min(k, candidates.Count); c++)
            {
                int neighborIdx = candidates[c].idx;
                int minIdx = Mathf.Min(i, neighborIdx);
                int maxIdx = Mathf.Max(i, neighborIdx);

                if (!connectedPairs.Contains((minIdx, maxIdx)))
                {
                    Spring s = new Spring(current, allPoints[neighborIdx], springStiffness, springDamping, transform, springLineMaterial);
                    springs.Add(s);
                    current.connectedSprings.Add(s);
                    allPoints[neighborIdx].connectedSprings.Add(s);
                    connectedPairs.Add((minIdx, maxIdx));
                    connectionsForThisPoint++;
                    totalConnections++;
                }
            }
        }

        Debug.Log($"KNN: Connected {totalConnections} springs for {n} points");
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

    private void TransferUniquePointsToAllPoints(HashSet<MassPoint> uniquePoints)
    {
        allPoints.Clear(); // Optional: clear previous data if needed
        allPoints.AddRange(uniquePoints);
    }


}
