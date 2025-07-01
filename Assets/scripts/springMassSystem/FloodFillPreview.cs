using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class FloodFillPreview : MonoBehaviour
{
    public GameObject meshObject;
    public float voxelSize = 0.1f;
    public int resolution = 10;
    public Color gizmoColor = Color.cyan;

    private HashSet<Vector3> previewPoints;

    private void OnDrawGizmosSelected()
    {
        if (meshObject == null) return;

        Gizmos.color = gizmoColor;

        if (previewPoints == null || previewPoints.Count == 0)
        {
            previewPoints = new HashSet<Vector3>();
            GeneratePreviewPoints();
        }

        foreach (var pos in previewPoints)
        {
            Gizmos.DrawWireCube(pos, Vector3.one * voxelSize);
        }
    }

    private void GeneratePreviewPoints()
    {
        Bounds bounds = VoxelFiller.CalculateWorldBounds(meshObject);
        float size = 1f / resolution;

        MeshInsideChecker checker = new MeshInsideChecker(meshObject);
        Vector3 start = bounds.center;

        if (!checker.IsPointInside(start)) return;

        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        Vector3Int startGrid = WorldToGrid(start, bounds.min, size);
        queue.Enqueue(startGrid);
        visited.Add(startGrid);

        Vector3Int[] directions = new Vector3Int[]
        {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1)
        };

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            Vector3 worldPos = GridToWorld(current, bounds.min, size);

            if (!checker.IsPointInside(worldPos)) continue;
            previewPoints.Add(worldPos);

            foreach (var dir in directions)
            {
                Vector3Int neighbor = current + dir;
                if (!visited.Contains(neighbor))
                {
                    Vector3 neighborWorld = GridToWorld(neighbor, bounds.min, size);
                    if (checker.IsPointInside(neighborWorld))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
            }
        }
    }

    private Vector3Int WorldToGrid(Vector3 worldPos, Vector3 minBounds, float voxelSize)
    {
        Vector3 relative = worldPos - minBounds;
        return new Vector3Int(
            Mathf.FloorToInt(relative.x / voxelSize),
            Mathf.FloorToInt(relative.y / voxelSize),
            Mathf.FloorToInt(relative.z / voxelSize)
        );
    }

    private Vector3 GridToWorld(Vector3Int gridPos, Vector3 minBounds, float voxelSize)
    {
        return minBounds + new Vector3(
            gridPos.x * voxelSize,
            gridPos.y * voxelSize,
            gridPos.z * voxelSize
        );
    }

    
}
