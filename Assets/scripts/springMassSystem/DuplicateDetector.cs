using System.Collections.Generic;
using UnityEngine;

class DuplicateDetector
{
    private float cellSize;
    private Dictionary<Vector3Int, List<Vector3>> grid = new();

    public DuplicateDetector(float minDist)
    {
        cellSize = minDist * 1.1f; // small buffer
    }

    public bool IsDuplicate(Vector3 point)
    {
        Vector3Int cell = ToCell(point);
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                {
                    Vector3Int neighbor = cell + new Vector3Int(x, y, z);
                    if (grid.TryGetValue(neighbor, out var list))
                    {
                        foreach (var p in list)
                        {
                            if (Vector3.Distance(p, point) < cellSize)
                                return true;
                        }
                    }
                }

        if (!grid.ContainsKey(cell))
            grid[cell] = new List<Vector3>();

        grid[cell].Add(point);
        return false;
    }

    private Vector3Int ToCell(Vector3 point)
    {
        return new Vector3Int(
            Mathf.FloorToInt(point.x / cellSize),
            Mathf.FloorToInt(point.y / cellSize),
            Mathf.FloorToInt(point.z / cellSize)
        );
    }
}
