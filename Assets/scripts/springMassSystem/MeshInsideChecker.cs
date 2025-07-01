using UnityEngine;

public class MeshInsideChecker
{
    private Mesh[] meshes;
    private Transform[] meshTransforms;

    public MeshInsideChecker(GameObject meshObject)
    {
        var meshFilters = meshObject.GetComponentsInChildren<MeshFilter>();
        meshes = new Mesh[meshFilters.Length];
        meshTransforms = new Transform[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
        {
            meshes[i] = meshFilters[i].sharedMesh;
            meshTransforms[i] = meshFilters[i].transform;
        }
    }

    public bool IsPointInside(Vector3 point)
    {
        Vector3 rayDir = Vector3.right;
        int hitCount = 0;

        for (int i = 0; i < meshes.Length; i++)
        {
            var mesh = meshes[i];
            var transform = meshTransforms[i];

            if (mesh == null) continue;

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            // Transform the point into the local space of the mesh
            Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
            Vector3 localPoint = worldToLocal.MultiplyPoint3x4(point);
            Vector3 localDir = worldToLocal.MultiplyVector(rayDir).normalized;

            Ray ray = new Ray(localPoint, localDir);

            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 v0 = vertices[triangles[t]];
                Vector3 v1 = vertices[triangles[t + 1]];
                Vector3 v2 = vertices[triangles[t + 2]];

                if (RayIntersectsTriangle(ray, v0, v1, v2, out float distance))
                {
                    hitCount++;
                }
            }
        }

        return (hitCount % 2) == 1;
    }

    private bool RayIntersectsTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, out float t)
    {
        t = 0;
        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;

        Vector3 h = Vector3.Cross(ray.direction, edge2);
        float a = Vector3.Dot(edge1, h);
        if (Mathf.Abs(a) < Mathf.Epsilon)
            return false;

        float f = 1.0f / a;
        Vector3 s = ray.origin - v0;
        float u = f * Vector3.Dot(s, h);
        if (u < 0.0 || u > 1.0)
            return false;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(ray.direction, q);
        if (v < 0.0 || u + v > 1.0)
            return false;

        t = f * Vector3.Dot(edge2, q);
        return t > Mathf.Epsilon;
    }
}
