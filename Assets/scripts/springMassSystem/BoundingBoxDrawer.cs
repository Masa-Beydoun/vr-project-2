#if UNITY_EDITOR
using UnityEngine;

public class BoundingBoxDrawer : MonoBehaviour
{
    public Vector3 size = Vector3.one;
    public bool isSphere = false;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        if (isSphere)
        {
            float radius = size.x / 2f; // Assuming uniform scale
            Gizmos.DrawWireSphere(transform.position, radius);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
#endif

