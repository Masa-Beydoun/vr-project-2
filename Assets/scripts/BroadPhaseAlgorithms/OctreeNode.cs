using System.Collections.Generic;
using UnityEngine;

public class OctreeNode
{
    private Bounds bounds;
    private List<PhysicalObject> objects;
    private OctreeNode[] children;

    private const int maxObjects = 4;
    private const int maxDepth = 5;
    private int depth;

    public OctreeNode(Bounds bounds, int depth = 0)
    {
        this.bounds = bounds;
        this.depth = depth;
        this.objects = new List<PhysicalObject>();
        this.children = null;
    }

    public void Insert(PhysicalObject obj)
    {
        if (!bounds.Intersects(GetBounds(obj)))
            return;

        if (children == null && (objects.Count < maxObjects || depth >= maxDepth))
        {
            objects.Add(obj);
            return;
        }

        if (children == null)
            Subdivide();

        foreach (var child in children)
            child.Insert(obj);
    }

    public void GetPotentialCollisions(List<(PhysicalObject, PhysicalObject)> pairs)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            for (int j = i + 1; j < objects.Count; j++)
            {
                var a = objects[i];
                var b = objects[j];

                var ordered = a.GetInstanceID() < b.GetInstanceID() ? (a, b) : (b, a);
                if (!pairs.Contains(ordered))
                    pairs.Add(ordered);
            }
        }

        if (children != null)
        {
            foreach (var child in children)
                child.GetPotentialCollisions(pairs);
        }
    }

    private void Subdivide()
    {
        children = new OctreeNode[8];
        Vector3 size = bounds.size / 2f;
        Vector3 center = bounds.center;

        Debug.Log($"Subdivision added, depth : {depth}");

        int index = 0;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 offset = new Vector3(
                        x * size.x / 2f,
                        y * size.y / 2f,
                        z * size.z / 2f
                    );

                    Bounds childBounds = new Bounds(center + offset, size);
                    children[index++] = new OctreeNode(childBounds, depth + 1);
                }
            }
        }

        // Reinsert existing objects
        foreach (var obj in objects)
        {
            foreach (var child in children)
                child.Insert(obj);
        }

        objects.Clear();
    }

    private Bounds GetBounds(PhysicalObject obj)
    {
        Vector3 halfSize = obj.shape == PhysicalObject.ShapeType.Sphere
            ? Vector3.one * obj.radius
            : obj.transform.localScale / 2;

        return new Bounds(obj.transform.position, halfSize * 2);
    }
}
