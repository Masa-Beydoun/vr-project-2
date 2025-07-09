using UnityEngine;

public class BoundingBoxDrawer : MonoBehaviour
{
    public Vector3 size = Vector3.one;
    public bool isSphere = false;

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = isSphere ? 50 : 16;
        lineRenderer.loop = !isSphere;
        lineRenderer.widthMultiplier = 0.02f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.useWorldSpace = false;
    }

    void Start()
    {
        if (isSphere)
            DrawSphere();
        else
            DrawBox();
    }

    void DrawBox()
    {
        Vector3 half = size / 2f;
        Vector3[] corners = new Vector3[8]
        {
            new Vector3(-half.x, -half.y, -half.z),
            new Vector3(half.x, -half.y, -half.z),
            new Vector3(half.x, -half.y, half.z),
            new Vector3(-half.x, -half.y, half.z),

            new Vector3(-half.x, half.y, -half.z),
            new Vector3(half.x, half.y, -half.z),
            new Vector3(half.x, half.y, half.z),
            new Vector3(-half.x, half.y, half.z),
        };

        Vector3[] linePoints = {
            corners[0], corners[1],
            corners[1], corners[2],
            corners[2], corners[3],
            corners[3], corners[0],

            corners[4], corners[5],
            corners[5], corners[6],
            corners[6], corners[7],
            corners[7], corners[4],

            corners[0], corners[4],
            corners[1], corners[5],
            corners[2], corners[6],
            corners[3], corners[7],
        };

        lineRenderer.positionCount = linePoints.Length;
        lineRenderer.SetPositions(linePoints);
    }

    void DrawSphere()
    {
        // Draw approximate circle lines for sphere using lineRenderer
        int segments = 50;
        lineRenderer.positionCount = segments * 3;

        float radius = size.x / 2f;

        for (int i = 0; i < segments; i++)
        {
            float theta = (2f * Mathf.PI * i) / segments;
            float nextTheta = (2f * Mathf.PI * (i + 1)) / segments;

            // XY circle
            lineRenderer.SetPosition(i, new Vector3(radius * Mathf.Cos(theta), radius * Mathf.Sin(theta), 0));
            // XZ circle
            lineRenderer.SetPosition(i + segments, new Vector3(radius * Mathf.Cos(theta), 0, radius * Mathf.Sin(theta)));
            // YZ circle
            lineRenderer.SetPosition(i + segments * 2, new Vector3(0, radius * Mathf.Cos(theta), radius * Mathf.Sin(theta)));
        }
    }
}
