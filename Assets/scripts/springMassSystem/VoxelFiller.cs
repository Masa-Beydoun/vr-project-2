using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public struct Triangle
{
    public Vector3 v0, v1, v2;
    public Vector3 normal;
    public Vector3 center;

    public Triangle(Vector3 a, Vector3 b, Vector3 c)
    {
        v0 = a; v1 = b; v2 = c;
        normal = Vector3.Cross(b - a, c - a).normalized;
        center = (a + b + c) / 3f;
    }
}

public static class VoxelFiller
{
    // 1. Improved FillUsingBounds with better error handling and optimization
    public static void FillUsingBounds(GameObject meshObject, GameObject pointPrefab, Transform parent, HashSet<MassPoint> uniquePoints, int resolution)
    {
        Debug.Log("bounds called");
        if (meshObject == null || pointPrefab == null || parent == null)
        {
            Debug.LogError("[Bounds Fill] Invalid parameters provided");
            return;
        }

        Bounds bounds = CalculateWorldBounds(meshObject);
        float voxelSize = 1f / resolution;

        // Add small padding to ensure we don't miss edge cases
        bounds.Expand(voxelSize * 0.1f);

        MeshInsideChecker checker = new MeshInsideChecker(meshObject);
        int count = 0;
        int totalChecked = 0;

        Vector3 start = bounds.min;
        Vector3 end = bounds.max;

        for (float x = start.x; x < end.x; x += voxelSize)
        {
            for (float y = start.y; y < end.y; y += voxelSize)
            {
                for (float z = start.z; z < end.z; z += voxelSize)
                {
                    Vector3 p = new Vector3(x, y, z);
                    totalChecked++;

                    if (checker.IsPointInside(p))
                    {
                        if (TryAddUniquePoint(p, pointPrefab, parent, uniquePoints, voxelSize))
                        {
                            count++;
                        }
                    }
                }
            }
        }

        Debug.Log($"[Bounds Fill] Spawned {count} points out of {totalChecked} checked positions.");
    }

    // 2. Helper method to reduce code duplication
    private static bool TryAddUniquePoint(Vector3 position, GameObject pointPrefab, Transform parent, HashSet<MassPoint> uniquePoints, float scale)
    {
        MassPoint candidate = new MassPoint(position, null, parent.name);
        if (uniquePoints.Contains(candidate))
            return false;

        GameObject go = Object.Instantiate(pointPrefab, position, Quaternion.identity, parent);
        go.transform.localScale = Vector3.one * scale;
        go.name = $"VoxelPoint_{uniquePoints.Count}";

        var po = go.GetComponent<PhysicalObject>() ?? go.AddComponent<PhysicalObject>();
        var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();

        MassPoint mp = new MassPoint(position, po, parent.name);
        controller.Initialize(mp);
        uniquePoints.Add(mp);
        return true;
    }

    public static Bounds CalculateWorldBounds(GameObject obj)
    {
        MeshFilter[] mfs = obj.GetComponentsInChildren<MeshFilter>();
        if (mfs.Length == 0)
        {
            Debug.LogWarning("[CalculateWorldBounds] No MeshFilters found!");
            return new Bounds(obj.transform.position, Vector3.one);
        }

        bool initialized = false;
        Bounds totalBounds = new Bounds();

        foreach (var mf in mfs)
        {
            if (mf.sharedMesh == null) continue;

            Bounds localBounds = mf.sharedMesh.bounds;
            Bounds worldBounds = TransformBounds(mf, localBounds);

            if (!initialized)
            {
                totalBounds = worldBounds;
                initialized = true;
            }
            else
            {
                totalBounds.Encapsulate(worldBounds);
            }
        }

        Debug.Log($"[CalculateWorldBounds] Final bounds center: {totalBounds.center}, size: {totalBounds.size}");
        return totalBounds;
    }


    private static Bounds TransformBounds(MeshFilter mf, Bounds localBounds)
    {
        Vector3 worldMin = mf.transform.TransformPoint(localBounds.min);
        Vector3 worldMax = mf.transform.TransformPoint(localBounds.max);
        Bounds worldBounds = new Bounds();
        worldBounds.SetMinMax(worldMin, worldMax);
        return worldBounds;
    }

    // 3. Improved FillUsingFloodFill with better performance and error handling
    public static void FillUsingFloodFill(GameObject meshObject, GameObject pointPrefab, Transform parent, HashSet<MassPoint> uniquePoints, int resolution)
    {
        if (meshObject == null || pointPrefab == null || parent == null)
        {
            Debug.LogError("[FloodFill] Invalid parameters provided");
            return;
        }

        Bounds bounds = CalculateWorldBounds(meshObject);
        float voxelSize = 1f / resolution;
        bounds.Expand(voxelSize * 0.1f);

        MeshInsideChecker checker = new MeshInsideChecker(meshObject);
        Vector3? maybeStart = FindInternalPoint(meshObject, bounds, resolution);

        if (!maybeStart.HasValue)
        {
            Debug.LogWarning("[FloodFill] No valid internal point found. Trying center point.");
            Vector3 center = bounds.center;
            if (checker.IsPointInside(center))
            {
                maybeStart = center;
            }
            else
            {
                Debug.LogError("[FloodFill] Cannot find any internal point to start flood fill.");
                return;
            }
        }

        Vector3 start = maybeStart.Value;
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Vector3Int startGrid = WorldToGrid(start, bounds.min, voxelSize);

        queue.Enqueue(startGrid);
        visited.Add(startGrid);

        int count = 0;
        int maxIterations = 100000; // Prevent infinite loops
        int iterations = 0;

        Vector3Int[] directions = {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };

        while (queue.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            Vector3Int current = queue.Dequeue();
            Vector3 worldPos = GridToWorld(current, bounds.min, voxelSize);

            if (!checker.IsPointInside(worldPos)) continue;

            if (TryAddUniquePoint(worldPos, pointPrefab, parent, uniquePoints, voxelSize))
            {
                count++;
            }

            foreach (var dir in directions)
            {
                Vector3Int neighbor = current + dir;
                if (!visited.Contains(neighbor))
                {
                    Vector3 neighborWorld = GridToWorld(neighbor, bounds.min, voxelSize);

                    // Check bounds to prevent infinite expansion
                    if (bounds.Contains(neighborWorld) && checker.IsPointInside(neighborWorld))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
            }
        }

        if (iterations >= maxIterations)
        {
            Debug.LogWarning("[FloodFill] Reached maximum iterations limit. Process may be incomplete.");
        }

        Debug.Log($"[FloodFill] Spawned {count} points in {iterations} iterations.");
    }

    private static Vector3Int WorldToGrid(Vector3 worldPos, Vector3 minBounds, float voxelSize)
    {
        Vector3 relative = worldPos - minBounds;
        return new Vector3Int(
            Mathf.FloorToInt(relative.x / voxelSize),
            Mathf.FloorToInt(relative.y / voxelSize),
            Mathf.FloorToInt(relative.z / voxelSize)
        );
    }

    private static Vector3 GridToWorld(Vector3Int gridPos, Vector3 minBounds, float voxelSize)
    {
        return minBounds + new Vector3(
            gridPos.x * voxelSize,
            gridPos.y * voxelSize,
            gridPos.z * voxelSize
        );
    }

    public static void FillUsingOctreeBasic(GameObject meshObject, GameObject pointPrefab, Transform parent, HashSet<MassPoint> uniquePoints, int maxDepth)
    {
        MeshFilter[] meshFilters = meshObject.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogError("[OctreeBasic] No MeshFilters found!");
            return;
        }

        Bounds worldBounds = GetCombinedWorldBounds(meshFilters);
        MeshInsideChecker insideChecker = new MeshInsideChecker(meshObject);
        int filledCount = 0;

        void Subdivide(Bounds bounds, int depth)
        {
            if (depth > maxDepth) return;

            Vector3 center = bounds.center;
            bool isInside = insideChecker.IsPointInside(center);

            if (depth == maxDepth)
            {
                if (isInside)
                {
                    GameObject go = GameObject.Instantiate(pointPrefab, center, Quaternion.identity, parent);
                    go.transform.localScale = Vector3.one * (bounds.size.x * 0.5f);

                    var po = go.GetComponent<PhysicalObject>() ?? go.AddComponent<PhysicalObject>();
                    var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();

                    MassPoint mp = new MassPoint(center, po, parent.name);
                    controller.Initialize(mp);
                    uniquePoints.Add(mp);
                    filledCount++;

                    Debug.DrawRay(center, Vector3.up * 0.05f, Color.cyan, 3f);
                }
                return;
            }

            Vector3 size = bounds.size / 2f;
            Vector3 min = bounds.min;

            for (int xi = 0; xi < 2; xi++)
            {
                for (int yi = 0; yi < 2; yi++)
                {
                    for (int zi = 0; zi < 2; zi++)
                    {
                        Vector3 offset = new Vector3(
                            (xi + 0.5f) * size.x,
                            (yi + 0.5f) * size.y,
                            (zi + 0.5f) * size.z
                        );
                        Vector3 childCenter = min + offset;
                        Bounds childBounds = new Bounds(childCenter, size);
                        Subdivide(childBounds, depth + 1);
                    }
                }
            }
        }

        Debug.Log($"[OctreeBasic] Starting subdivision with max depth = {maxDepth}");
        Subdivide(worldBounds, 0);
        Debug.Log($"[OctreeBasic] Spawned {filledCount} points.");
    }


    private static Bounds GetCombinedWorldBounds(MeshFilter[] meshFilters)
    {
        Bounds combined = new Bounds();
        bool initialized = false;

        foreach (var mf in meshFilters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            var localBounds = mesh.bounds;
            Vector3[] corners = new Vector3[]
            {
                localBounds.min,
                localBounds.max,
                new Vector3(localBounds.min.x, localBounds.min.y, localBounds.max.z),
                new Vector3(localBounds.min.x, localBounds.max.y, localBounds.min.z),
                new Vector3(localBounds.max.x, localBounds.min.y, localBounds.min.z),
                new Vector3(localBounds.max.x, localBounds.max.y, localBounds.min.z),
                new Vector3(localBounds.min.x, localBounds.max.y, localBounds.max.z),
                new Vector3(localBounds.max.x, localBounds.min.y, localBounds.max.z)
            };

            foreach (var c in corners)
            {
                Vector3 worldPoint = mf.transform.TransformPoint(c);
                if (!initialized)
                {
                    combined = new Bounds(worldPoint, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    combined.Encapsulate(worldPoint);
                }
            }
        }

        return combined;
    }

    // 4. Improved FillUsingOctreeAdvanced with better subdivision logic
    public static void FillUsingOctreeAdvanced(GameObject meshObject, GameObject pointPrefab, Transform parent, HashSet<MassPoint> uniquePoints, int maxDepth)
    {
        if (meshObject == null || pointPrefab == null || parent == null)
        {
            Debug.LogError("[Octree Advanced] Invalid parameters provided");
            return;
        }

        MeshFilter[] meshFilters = meshObject.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogError("No MeshFilters found for Octree filling!");
            return;
        }

        Bounds worldBounds = GetCombinedWorldBounds(meshFilters);
        MeshInsideChecker insideChecker = new MeshInsideChecker(meshObject);
        MeshIntersectionTester intersectionTester = new MeshIntersectionTester(meshObject);

        int filledCount = 0;
        int totalChecked = 0;

        void Subdivide(Bounds bounds, int depth)
        {
            totalChecked++;
            Vector3 center = bounds.center;
            float size = bounds.size.x;

            bool isInside = insideChecker.IsPointInside(center);
            bool intersects = intersectionTester.Intersects(bounds);

            // Early termination if completely outside
            if (!intersects && !isInside) return;

            // Create point if at max depth or completely inside
            if (depth >= maxDepth || (isInside && !intersects))
            {
                if (isInside)
                {
                    if (TryAddUniquePoint(center, pointPrefab, parent, uniquePoints, size * 0.5f))
                    {
                        filledCount++;
                    }
                }
                return;
            }

            // Subdivide into 8 children
            Vector3 half = bounds.size / 2f;
            Vector3 min = bounds.min;

            for (int xi = 0; xi < 2; xi++)
            {
                for (int yi = 0; yi < 2; yi++)
                {
                    for (int zi = 0; zi < 2; zi++)
                    {
                        Vector3 offset = new Vector3(
                            (xi + 0.5f) * half.x,
                            (yi + 0.5f) * half.y,
                            (zi + 0.5f) * half.z
                        );
                        Vector3 childCenter = min + offset;
                        Bounds childBounds = new Bounds(childCenter, half);
                        Subdivide(childBounds, depth + 1);
                    }
                }
            }
        }

        Subdivide(worldBounds, 0);
        Debug.Log($"[Octree Advanced] Spawned {filledCount} points from {totalChecked} subdivisions at max depth {maxDepth}");
    }


    // 5. Improved FillUsingSDFDistanceOnly with distance threshold
    public static void FillUsingSDFDistanceOnly(GameObject meshObject, GameObject pointPrefab, Transform parent, HashSet<MassPoint> uniquePoints, int resolution)
    {
        if (meshObject == null || pointPrefab == null || parent == null)
        {
            Debug.LogError("[SDF Distance] Invalid parameters provided");
            return;
        }

        Bounds bounds = CalculateWorldBounds(meshObject);
        float voxelSize = 1f / resolution;
        bounds.Expand(voxelSize * 0.1f);

        MeshFilter[] meshFilters = meshObject.GetComponentsInChildren<MeshFilter>();
        List<Triangle> allTriangles = new List<Triangle>();

        // Collect triangles from all meshes
        foreach (var mf in meshFilters)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            Vector3[] vertices = mesh.vertices;
            int[] tris = mesh.triangles;
            Matrix4x4 localToWorld = mf.transform.localToWorldMatrix;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v0 = localToWorld.MultiplyPoint3x4(vertices[tris[i]]);
                Vector3 v1 = localToWorld.MultiplyPoint3x4(vertices[tris[i + 1]]);
                Vector3 v2 = localToWorld.MultiplyPoint3x4(vertices[tris[i + 2]]);
                allTriangles.Add(new Triangle(v0, v1, v2));
            }
        }

        if (allTriangles.Count == 0)
        {
            Debug.LogWarning("[SDF Distance] No triangles found in mesh!");
            return;
        }

        int count = 0;
        float maxDistance = voxelSize * 2f; // Only create points within this distance

        for (float x = bounds.min.x; x < bounds.max.x; x += voxelSize)
        {
            for (float y = bounds.min.y; y < bounds.max.y; y += voxelSize)
            {
                for (float z = bounds.min.z; z < bounds.max.z; z += voxelSize)
                {
                    Vector3 p = new Vector3(x, y, z);
                    float minDist = float.MaxValue;

                    // Use spatial optimization for large triangle counts
                    if (allTriangles.Count > 1000)
                    {
                        // Only check nearby triangles
                        var nearbyTriangles = allTriangles.Where(tri =>
                            Vector3.Distance(tri.center, p) < maxDistance * 2f);

                        foreach (var tri in nearbyTriangles)
                        {
                            float d = DistanceToTriangle(p, tri);
                            if (d < minDist)
                                minDist = d;
                        }
                    }
                    else
                    {
                        foreach (var tri in allTriangles)
                        {
                            float d = DistanceToTriangle(p, tri);
                            if (d < minDist)
                                minDist = d;
                        }
                    }

                    // Only create points within reasonable distance
                    if (minDist <= maxDistance)
                    {
                        GameObject go = Object.Instantiate(pointPrefab, p, Quaternion.identity, parent);
                        go.transform.localScale = Vector3.one * voxelSize;
                        go.name = $"SDFPoint_{count}";

                        var po = go.GetComponent<PhysicalObject>() ?? go.AddComponent<PhysicalObject>();
                        var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();

                        MassPoint mp = new MassPoint(p, po, parent.name);
                        mp.signedDistance = minDist;
                        controller.Initialize(mp);
                        uniquePoints.Add(mp);
                        count++;
                    }
                }
            }
        }

        Debug.Log($"[SDF Distance] Spawned {count} points within distance threshold {maxDistance}.");
    }

    public static float DistanceToTriangle(Vector3 point, Triangle tri)
    {
        Vector3 edge0 = tri.v1 - tri.v0;
        Vector3 edge1 = tri.v2 - tri.v0;
        Vector3 v0ToPoint = point - tri.v0;

        float a = Vector3.Dot(edge0, edge0);
        float b = Vector3.Dot(edge0, edge1);
        float c = Vector3.Dot(edge1, edge1);
        float d = Vector3.Dot(edge0, v0ToPoint);
        float e = Vector3.Dot(edge1, v0ToPoint);

        float det = a * c - b * b;
        float s = b * e - c * d;
        float t = b * d - a * e;

        if (s + t <= det)
        {
            if (s < 0)
            {
                if (t < 0) return (point - tri.v0).magnitude; // region 4
                else return DistanceToSegment(point, tri.v0, tri.v2); // region 3
            }
            else if (t < 0) return DistanceToSegment(point, tri.v0, tri.v1); // region 5
            else return DistanceToPlane(point, tri); // region 0
        }
        else
        {
            if (s < 0) return DistanceToSegment(point, tri.v1, tri.v2); // region 2
            else if (t < 0) return DistanceToSegment(point, tri.v0, tri.v1); // region 6
            else return DistanceToSegment(point, tri.v1, tri.v2); // region 1
        }
    }

    public static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
        Vector3 closest = a + t * ab;
        return Vector3.Distance(p, closest);
    }

    public static float DistanceToPlane(Vector3 p, Triangle tri)
    {
        Vector3 normal = Vector3.Cross(tri.v1 - tri.v0, tri.v2 - tri.v0).normalized;
        return Mathf.Abs(Vector3.Dot(p - tri.v0, normal));
    }

    public static Vector3? FindInternalPoint(GameObject meshObject, Bounds bounds, int resolution = 20, int maxTries = 500)
    {
        MeshInsideChecker checker = new MeshInsideChecker(meshObject);
        float voxelSize = 1f / resolution;

        for (int i = 0; i < maxTries; i++)
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            float z = Random.Range(bounds.min.z, bounds.max.z);

            Vector3 point = new Vector3(x, y, z);
            if (checker.IsPointInside(point))
            {
                return point;
            }
        }

        Debug.LogWarning("[VoxelFiller] Couldn't find an internal seed point after maxTries.");
        return null;
    }

}
