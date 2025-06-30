using System.Collections.Generic;
using UnityEngine;

public static class VoxelFiller
{
    public static void FillUsingBounds(GameObject meshObject, GameObject pointPrefab, Transform parent, HashSet<MassPoint> uniquePoints, int resolution)
    {
        Bounds bounds = CalculateWorldBounds(meshObject);
        float voxelSize = 1f / resolution;

        MeshInsideChecker checker = new MeshInsideChecker(meshObject);
        int count = 0;

        for (float x = bounds.min.x; x < bounds.max.x; x += voxelSize)
        {
            for (float y = bounds.min.y; y < bounds.max.y; y += voxelSize)
            {
                for (float z = bounds.min.z; z < bounds.max.z; z += voxelSize)
                {
                    Vector3 p = new Vector3(x, y, z);
                    if (checker.IsPointInside(p))
                    {
                        MassPoint candidate = new MassPoint(p, null);
                        if (uniquePoints.Contains(candidate))
                            continue;

                        GameObject go = Object.Instantiate(pointPrefab, p, Quaternion.identity, parent);
                        go.transform.localScale = Vector3.one * voxelSize;

                        var po = go.GetComponent<PhysicalObject>() ?? go.AddComponent<PhysicalObject>();
                        var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                        go.AddComponent<CollisionBody>();

                        MassPoint mp = new MassPoint(p, po);
                        controller.Initialize(mp);
                        uniquePoints.Add(mp);
                        count++;
                    }
                }
            }
        }

        Debug.Log($"[Bounds Fill] Spawned {count} points.");
    }

    public static void FillUsingVolumeSampling(GameObject meshObject, GameObject pointPrefab, Transform parent, HashSet<MassPoint> uniquePoints, int resolution)
    {
        FillUsingBounds(meshObject, pointPrefab, parent, uniquePoints, resolution);
    }

    public static Bounds CalculateWorldBounds(GameObject obj)
    {
        MeshFilter[] mfs = obj.GetComponentsInChildren<MeshFilter>();
        if (mfs.Length == 0) return new Bounds(obj.transform.position, Vector3.one);

        Bounds totalBounds = TransformBounds(mfs[0], mfs[0].sharedMesh.bounds);

        foreach (var mf in mfs)
        {
            if (mf.sharedMesh == null) continue;
            Bounds local = mf.sharedMesh.bounds;
            totalBounds.Encapsulate(TransformBounds(mf, local));
        }
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

    public static void FillUsingFloodFill(GameObject meshObject, GameObject pointPrefab, Transform parent, HashSet<MassPoint> uniquePoints, int resolution)
    {
        Bounds bounds = CalculateWorldBounds(meshObject);
        float voxelSize = 1f / resolution;

        MeshInsideChecker checker = new MeshInsideChecker(meshObject);
        Vector3 start = bounds.center;
        if (!checker.IsPointInside(start))
        {
            Debug.LogWarning("[FloodFill] Start point is not inside the mesh.");
            return;
        }

        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Vector3Int startGrid = WorldToGrid(start, bounds.min, voxelSize);
        queue.Enqueue(startGrid);
        visited.Add(startGrid);

        int count = 0;
        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            Vector3 worldPos = GridToWorld(current, bounds.min, voxelSize);

            if (!checker.IsPointInside(worldPos)) continue;

            MassPoint candidate = new MassPoint(worldPos, null);
            if (!uniquePoints.Contains(candidate))
            {
                GameObject go = Object.Instantiate(pointPrefab, worldPos, Quaternion.identity, parent);
                go.transform.localScale = Vector3.one * voxelSize;

                var po = go.GetComponent<PhysicalObject>() ?? go.AddComponent<PhysicalObject>();
                var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                go.AddComponent<CollisionBody>();

                MassPoint mp = new MassPoint(worldPos, po);
                controller.Initialize(mp);
                uniquePoints.Add(mp);
                count++;
            }

            foreach (var dir in directions)
            {
                Vector3Int neighbor = current + dir;
                if (!visited.Contains(neighbor))
                {
                    Vector3 neighborWorld = GridToWorld(neighbor, bounds.min, voxelSize);
                    if (checker.IsPointInside(neighborWorld))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
            }
        }

        Debug.Log($"[FloodFill] Spawned {count} points.");
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
            Debug.LogError("No MeshFilters found for Octree filling!");
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
                    Vector3 spawnPos = center;

                    GameObject go = GameObject.Instantiate(pointPrefab, spawnPos, Quaternion.identity, parent);
                    go.transform.localScale = Vector3.one * (bounds.size.x * 0.5f);

                    var po = go.GetComponent<PhysicalObject>() ?? go.AddComponent<PhysicalObject>();
                    var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                    go.AddComponent<CollisionBody>();

                    MassPoint mp = new MassPoint(spawnPos, po);
                    controller.Initialize(mp);
                    uniquePoints.Add(mp);

                    Debug.DrawRay(spawnPos, Vector3.up * 0.1f, Color.cyan, 10f);
                    filledCount++;
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

        Subdivide(worldBounds, 0);
        Debug.Log($"[Octree Basic] Spawned {filledCount} points using depth {maxDepth}");
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

    public static void FillUsingOctreeAdvanced(GameObject meshObject, GameObject pointPrefab, Transform parent, HashSet<MassPoint> uniquePoints, int maxDepth)
    {
        bool insideTest = new MeshInsideChecker(meshObject).IsPointInside(meshObject.transform.position);
        Debug.Log("Center point inside mesh? " + insideTest);


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

        void Subdivide(Bounds bounds, int depth)
        {
            Vector3 center = bounds.center;
            float size = bounds.size.x;

            bool isInside = insideChecker.IsPointInside(center);
            bool intersects = intersectionTester.Intersects(bounds);

            if (!intersects && !isInside) return;

            if (depth == maxDepth || (isInside && !intersects))
            {
                if (isInside)
                {
                    GameObject go = Object.Instantiate(pointPrefab, center, Quaternion.identity, parent);
                    go.transform.localScale = Vector3.one * (size * 0.5f);

                    var po = go.GetComponent<PhysicalObject>() ?? go.AddComponent<PhysicalObject>();
                    var controller = go.GetComponent<MassPointController>() ?? go.AddComponent<MassPointController>();
                    go.AddComponent<CollisionBody>();

                    MassPoint mp = new MassPoint(center, po);
                    controller.Initialize(mp);
                    uniquePoints.Add(mp);

                    Debug.DrawRay(center, Vector3.up * 0.1f, Color.yellow, 10f);
                    filledCount++;
                }
                return;
            }

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
        Debug.Log($"[Octree Advanced] Spawned {filledCount} adaptive points at depth {maxDepth}");
    }
}
