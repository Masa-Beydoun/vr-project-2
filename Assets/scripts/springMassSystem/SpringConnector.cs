using System.Collections.Generic;
using UnityEngine;

public class SpringConnector
{
    private List<MassPoint> allPoints;
    private List<Spring> springs;
    private float springStiffness;
    private float springDamping;
    private Transform parentTransform;
    private Material springLineMaterial;
    private int k;

    public SpringConnector(List<MassPoint> allPoints, List<Spring> springs,
                           float stiffness, float damping,
                           Transform parentTransform, Material springMaterial,
                           int knnK = 6)
    {
        this.allPoints = allPoints;
        this.springs = springs;
        this.springStiffness = stiffness;
        this.springDamping = damping;
        this.parentTransform = parentTransform;
        this.springLineMaterial = springMaterial;
        this.k = knnK;
    }

    public void ConnectMeshSpringsHybrid(GameObject meshSourceObject)
    {
        springs.Clear();
        var connectedPairs = new HashSet<(int, int)>();

        MeshFilter[] meshFilters = meshSourceObject.GetComponentsInChildren<MeshFilter>();
        ConnectMeshSpringsTriangles(meshFilters, connectedPairs);

        ConnectMeshSpringsKNN(connectedPairs);
    }

    public void ConnectMeshSpringsTriangles(MeshFilter[] meshFilters, HashSet<(int, int)> connectedPairs)
    {
        Dictionary<Vector3, MassPoint> pointLookup = new Dictionary<Vector3, MassPoint>();
        foreach (var mp in allPoints)
            pointLookup[mp.position] = mp;

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

            Spring s = new Spring(p1, p2, springStiffness, springDamping, parentTransform, springLineMaterial);
            springs.Add(s);
            p1.connectedSprings.Add(s);
            p2.connectedSprings.Add(s);
            connectedPairs.Add(key);
        }
    }

    public void ConnectMeshSpringsKNN(HashSet<(int, int)> connectedPairs)
    {
        int n = allPoints.Count;
        if (n == 0) return;

        float cellSize = EstimateConnectionRadius(allPoints);
        SpatialGrid grid = new SpatialGrid(cellSize);

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
                    Spring s = new Spring(current, allPoints[neighborIdx], springStiffness, springDamping, parentTransform, springLineMaterial);
                    springs.Add(s);
                    current.connectedSprings.Add(s);
                    allPoints[neighborIdx].connectedSprings.Add(s);
                    connectedPairs.Add((minIdx, maxIdx));
                }
            }
        }
    }

    public MassPoint FindClosestMassPoint(Vector3 position)
    {
        MassPoint closest = null;
        float minDist = float.MaxValue;
        foreach (var p in allPoints)
        {
            float dist = Vector3.Distance(p.position, position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = p;
            }
        }
        return closest;
    }

    public float EstimateConnectionRadius(List<MassPoint> points)
    {
        float total = 0f;
        int count = 0;

        for (int i = 0; i < points.Count; i++)
        {
            float nearest = float.MaxValue;
            for (int j = 0; j < points.Count; j++)
            {
                if (i == j) continue;
                float dist = Vector3.Distance(points[i].position, points[j].position);
                if (dist < nearest) nearest = dist;
            }

            if (nearest < float.MaxValue)
            {
                total += nearest;
                count++;
            }
        }

        return count > 0 ? total / count : 1f;
    }
}
