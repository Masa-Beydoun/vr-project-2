using System.Collections.Generic;
using UnityEngine;

public class Octree : IBroadPhase
{
    private Bounds worldBounds;

    public Octree(Bounds worldBounds)
    {
        this.worldBounds = worldBounds;
    }

    public List<(PhysicalObject, PhysicalObject)> GetCollisionPairs(PhysicalObject[] objects)
    {
        OctreeNode root = new OctreeNode(worldBounds);

        foreach (var obj in objects)
            root.Insert(obj);

        List<(PhysicalObject, PhysicalObject)> pairs = new List<(PhysicalObject, PhysicalObject)>();
        root.GetPotentialCollisions(pairs);
        return pairs;
    }
}
