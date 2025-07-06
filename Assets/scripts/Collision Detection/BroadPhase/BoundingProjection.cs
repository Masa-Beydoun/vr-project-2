
using UnityEngine;

public class BoundingProjection
{
    public PhysicalObject obj;
    public float minX, maxX;
    public float minY, maxY;
    public float minZ, maxZ;

    public BoundingProjection(PhysicalObject obj)
    {
        this.obj = obj;
        Vector3 center = obj.transform.position;

        Vector3 halfSize = obj.shapeType == ShapeType.Sphere
            ? Vector3.one * obj.radius
            : obj.transform.localScale / 2;

        this.minX = center.x - halfSize.x;
        this.maxX = center.x + halfSize.x;
        this.minY = center.y - halfSize.y;
        this.maxY = center.y + halfSize.y;
        this.minZ = center.z - halfSize.z;
        this.maxZ = center.z + halfSize.z;

    }

    public bool OverlapsWith(BoundingProjection other)
    {
        return (minX <= other.maxX && maxX >= other.minX) &&
               (minY <= other.maxY && maxY >= other.minY) &&
               (minZ <= other.maxZ && maxZ >= other.minZ);
    }
}
