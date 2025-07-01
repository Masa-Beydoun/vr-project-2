using System.Collections.Generic;
using UnityEngine;

public class SpringPairComparer : IEqualityComparer<(MassPoint, MassPoint)>
{
    public bool Equals((MassPoint, MassPoint) a, (MassPoint, MassPoint) b)
    {
        return (a.Item1.Equals(b.Item1) && a.Item2.Equals(b.Item2)) ||
               (a.Item1.Equals(b.Item2) && a.Item2.Equals(b.Item1));
    }

    public int GetHashCode((MassPoint, MassPoint) pair)
    {
        int h1 = pair.Item1.GetHashCode();
        int h2 = pair.Item2.GetHashCode();
        return h1 ^ h2;
    }
}
