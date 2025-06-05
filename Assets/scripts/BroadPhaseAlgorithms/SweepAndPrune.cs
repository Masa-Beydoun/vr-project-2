using System.Collections.Generic;
using UnityEngine;

public class SweepAndPrune : IBroadPhase
{
    public List<(PhysicalObject, PhysicalObject)> GetCollisionPairs(PhysicalObject[] objects)
    {
        List<BoundingProjection> projections = new List<BoundingProjection>();

        foreach (var obj in objects)
            projections.Add(new BoundingProjection(obj));

        // Sort by minX
        projections.Sort((a, b) => a.minX.CompareTo(b.minX));

        List<(PhysicalObject, PhysicalObject)> potentialPairs = new List<(PhysicalObject, PhysicalObject)>();

        for (int i = 0; i < projections.Count; i++)
        {
            var a = projections[i];
            for (int j = i + 1; j < projections.Count; j++)
            {
                var b = projections[j];

                if (b.minX > a.maxX)
                    break;

                if (a.OverlapsWith(b))
                    potentialPairs.Add((a.obj, b.obj));
            }
        }

        return potentialPairs;
    }
}
