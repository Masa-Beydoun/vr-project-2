using System.Collections.Generic;
using UnityEngine;

public class UniformGrid : IBroadPhase
{
    private float cellSize;

    public UniformGrid(float cellSize)
    {
        this.cellSize = cellSize;
    }

    public List<(PhysicalObject, PhysicalObject)> GetCollisionPairs(PhysicalObject[] objects)
    {
        HashSet<(PhysicalObject, PhysicalObject)> uniquePairs = new HashSet<(PhysicalObject, PhysicalObject)>();

        Dictionary<Vector3Int, List<PhysicalObject>> grid = BuildSpatialHashGrid(objects);

        foreach (var cellObjects in grid.Values)
        {
            for (int i = 0; i < cellObjects.Count; i++)
            {
                for (int j = i + 1; j < cellObjects.Count; j++)
                {
                    var a = cellObjects[i];
                    var b = cellObjects[j];

                    if (a != b)
                    {
                        var orderedPair = a.GetInstanceID() < b.GetInstanceID() ? (a, b) : (b, a);
                        uniquePairs.Add(orderedPair);
                    }
                }
            }
        }

        return new List<(PhysicalObject, PhysicalObject)>(uniquePairs);
    }

    Dictionary<Vector3Int, List<PhysicalObject>> BuildSpatialHashGrid(PhysicalObject[] objects)
    {
        Dictionary<Vector3Int, List<PhysicalObject>> grid = new Dictionary<Vector3Int, List<PhysicalObject>>();

        foreach (var obj in objects)
        {
            Vector3 min, max;

            if (obj.shapeType == BoundingShapeType.Sphere)
            {
                Vector3 rVec = Vector3.one * obj.radius;
                min = obj.transform.position - rVec;
                max = obj.transform.position + rVec;
            }
            else
            {
                Vector3 half = obj.transform.localScale / 2;
                min = obj.transform.position - half;
                max = obj.transform.position + half;
            }

            Vector3Int minCell = WorldToCell(min, cellSize);
            Vector3Int maxCell = WorldToCell(max, cellSize);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    for (int z = minCell.z; z <= maxCell.z; z++)
                    {
                        Vector3Int cell = new Vector3Int(x, y, z);
                        if (!grid.ContainsKey(cell))
                            grid[cell] = new List<PhysicalObject>();

                        grid[cell].Add(obj);
                    }
                }
            }
        }

        return grid;
    }

    Vector3Int WorldToCell(Vector3 position, float cellSize)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.y / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

}
