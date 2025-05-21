using UnityEngine;
using System.Collections.Generic;

public class FEMController : MonoBehaviour
{
    [Header("Mesh Settings")]
    public Vector3 CubeSize = Vector3.one;
    public Vector3 CubeCenter = Vector3.zero;
    
    [Header("Visualization")]
    public bool ShowNodes = true;
    public float NodeScale = 0.1f;
    public Color LineColor = Color.green;
    public float LineWidth = 0.02f;

    private Mesh3D mesh;
    private LineRenderer wireframeRenderer;
    private List<GameObject> nodeVisuals = new List<GameObject>();

    void Start()
    {
        InitializeMesh();
        VisualizeMesh();
    }

    void InitializeMesh()
    {
        mesh = new Mesh3D();
        mesh.GenerateCubeMesh(CubeSize, CubeCenter);
        Debug.Log($"FEM Mesh: {mesh.Nodes.Length} nodes, {mesh.Elements.Length} tetrahedrons");
    }

    void VisualizeMesh()
    {
        if (ShowNodes) CreateNodeVisuals();
        CreateWireframe();
    }

    void CreateNodeVisuals()
    {
        foreach (var node in mesh.Nodes)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = node.Position;
            sphere.transform.localScale = Vector3.one * NodeScale;
            nodeVisuals.Add(sphere);
        }
    }

    void CreateWireframe()
    {
        GameObject lineObj = new GameObject("Wireframe");
        wireframeRenderer = lineObj.AddComponent<LineRenderer>();
        
        List<Vector3> allLines = new List<Vector3>();
        foreach (var tet in mesh.Elements)
        {
            Vector3[] corners = mesh.GetTetCorners(tet);
            AddTetrahedronEdges(allLines, corners);
        }

        wireframeRenderer.positionCount = allLines.Count;
        wireframeRenderer.SetPositions(allLines.ToArray());
        wireframeRenderer.startWidth = LineWidth;
        wireframeRenderer.endWidth = LineWidth;
        wireframeRenderer.material = new Material(Shader.Find("Sprites/Default")) { color = LineColor };
    }

    void AddTetrahedronEdges(List<Vector3> lines, Vector3[] points)
    {
        // Add all 6 edges of tetrahedron (avoid duplicates in actual implementation)
        lines.Add(points[0]); lines.Add(points[1]);
        lines.Add(points[0]); lines.Add(points[2]);
        lines.Add(points[0]); lines.Add(points[3]);
        lines.Add(points[1]); lines.Add(points[2]);
        lines.Add(points[1]); lines.Add(points[3]);
        lines.Add(points[2]); lines.Add(points[3]);
    }

    void OnDestroy()
    {
        // Clean up visuals
        foreach (var obj in nodeVisuals) Destroy(obj);
        if (wireframeRenderer != null) Destroy(wireframeRenderer.gameObject);
    }
}
