using System.Collections.Generic;

public interface IBroadPhase
{
    List<(PhysicalObject, PhysicalObject)> GetCollisionPairs(PhysicalObject[] objects);
}
