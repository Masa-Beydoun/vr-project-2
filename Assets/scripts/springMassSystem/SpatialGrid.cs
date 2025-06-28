// Simple 3D grid for spatial partitioning points
using System.Collections.Generic;
using UnityEngine;

class SpatialGrid
{
    private Dictionary<(int, int, int), List<int>> grid = new Dictionary<(int, int, int), List<int>>();
    private float cellSize;

    public SpatialGrid(float cellSize)
    {
        this.cellSize = cellSize;
    }

    public (int, int, int) GetCell(Vector3 pos)
    {
        return (
            Mathf.FloorToInt(pos.x / cellSize),
            Mathf.FloorToInt(pos.y / cellSize),
            Mathf.FloorToInt(pos.z / cellSize)
        );
    }

    public void AddPoint(Vector3 pos, int index)
    {
        var cell = GetCell(pos);
        if (!grid.TryGetValue(cell, out var list))
        {
            list = new List<int>();
            grid[cell] = list;
        }
        list.Add(index);
    }

    public List<int> GetNeighbors(Vector3 pos)
    {
        List<int> neighbors = new List<int>();
        var cell = GetCell(pos);

        for (int x = cell.Item1 - 1; x <= cell.Item1 + 1; x++)
            for (int y = cell.Item2 - 1; y <= cell.Item2 + 1; y++)
                for (int z = cell.Item3 - 1; z <= cell.Item3 + 1; z++)
                {
                    var neighborCell = (x, y, z);
                    if (grid.TryGetValue(neighborCell, out var points))
                    {
                        neighbors.AddRange(points);
                    }
                }
        return neighbors;
    }
}
