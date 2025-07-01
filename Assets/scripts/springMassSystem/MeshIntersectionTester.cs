using UnityEngine;
using System.Linq;


public class MeshIntersectionTester
{
    private MeshCollider meshCollider;

    public MeshIntersectionTester(GameObject meshSource)
    {
        GameObject temp = new GameObject("TempCollider");
        temp.hideFlags = HideFlags.HideAndDontSave;

        meshCollider = temp.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = GetCombinedMesh(meshSource);
        meshCollider.convex = false;
    }

    public bool Intersects(Bounds bounds)
    {
        return Physics.CheckBox(bounds.center, bounds.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
    }

    private Mesh GetCombinedMesh(GameObject root)
    {
        CombineInstance[] combine = root.GetComponentsInChildren<MeshFilter>()
            .Where(mf => mf.sharedMesh != null)
            .Select(mf =>
            {
                return new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    transform = mf.transform.localToWorldMatrix
                };
            }).ToArray();

        Mesh combined = new Mesh();
        combined.CombineMeshes(combine);
        return combined;
    }
}
