using UnityEngine;
using System.Collections.Generic;

public class FEMController : MonoBehaviour
{
    public enum ShapeType { Cube, Sphere }

    [Header("Mesh Settings")]
    public ShapeType Shape = ShapeType.Cube;
    public Vector3 CubeSize = Vector3.one;
    public Vector3 CubeCenter = Vector3.zero;
    public float SphereRadius = 0.5f;
    public float SphereResolution = 0.1f;
    public Vector3 initialVelocity = Vector3.zero;

    [Header("Visualization")]
    public bool ShowNodes = true;
    public float NodeScale = 0.1f;
    public Color LineColor = Color.green;
    public float LineWidth = 0.02f;

    [Header("Material Properties")]
    public PhysicalMaterial material;

    private Mesh3D mesh;
    private LineRenderer wireframeRenderer;
    private List<GameObject> nodeVisuals = new();
    private List<float[,]> localStiffnessMatrices = new();
    private FEMSolver solver;
    private FEMDynamics dynamics;
    private float[] totalForce;

    private List<(int, int)> wireframeEdges = new();

    public void Initialize()
    {
        if (material == null)
        {
            Debug.LogError("PhysicalMaterial not assigned.");
            return;
        }

        InitializeMesh();

        solver = new FEMSolver
        {
            Nodes = mesh.Nodes,
            Tetrahedra = mesh.Elements
        };

        ComputeLocalStiffness();
        solver.AssembleGlobalStiffness(material);

        solver.GlobalMassMatrix = MassMatrixAssembler.Assemble(
            solver.Nodes,
            solver.Tetrahedra,
            material
        );

        // Initialize dynamics BEFORE calling VisualizeMesh
        dynamics = new FEMDynamics();
        dynamics.Initialize(solver.Nodes.Length * 3);

        for (int i = 0; i < solver.Nodes.Length; i++)
        {
            int baseIndex = i * 3;
            dynamics.Velocity[baseIndex] = initialVelocity.x;
            dynamics.Velocity[baseIndex + 1] = initialVelocity.y;
            dynamics.Velocity[baseIndex + 2] = initialVelocity.z;
        }

        Vector3 gravity = Vector3.zero;
        totalForce = GravityForceGenerator.ComputeGravityForce(
            solver.Nodes,
            solver.Tetrahedra,
            material.Density,
            gravity
        );

        // Now it's safe to visualize the mesh
        VisualizeMesh();

        PrintMassMatrixSummary(solver.GlobalMassMatrix);
        PrintExpectedMass(solver.Nodes, solver.Tetrahedra, material);
    }

    void Start()
    {
        if (material != null)
            Initialize();
        else
            Debug.LogWarning("Material not assigned. Click 'Initialize FEM' in Inspector.");
    }

    void InitializeMesh()
    {
        mesh = new Mesh3D();

        switch (Shape)
        {
            case ShapeType.Cube:
                mesh.GenerateCubeMesh(CubeSize, transform.position);
                break;
            case ShapeType.Sphere:
                mesh.GenerateSphereMesh(SphereRadius, SphereResolution, transform.position);
                break;
        }

        Debug.Log($"FEM Mesh: {mesh.Nodes.Length} nodes, {mesh.Elements.Length} tetrahedrons");
    }

    void ComputeLocalStiffness()
    {
        localStiffnessMatrices.Clear();

        for (int i = 0; i < mesh.Elements.Length; i++)
        {
            var tet = mesh.Elements[i];
            Vector3[] vertices = mesh.GetTetCorners(tet);
            float[,] ke = ElementStiffnessCalculator.ComputeLocalStiffness(vertices, material);
            localStiffnessMatrices.Add(ke);
        }

        if (localStiffnessMatrices.Count > 0)
        {
            var ke = localStiffnessMatrices[0];
            float volume = ElementStiffnessCalculator.ComputeTetrahedronVolume(mesh.GetTetCorners(mesh.Elements[0]));
            Debug.Log($"First local stiffness matrix (Volume: {volume:F4}):");

            for (int i = 0; i < ke.GetLength(0); i++)
            {
                string row = $"Row {i}:";
                for (int j = 0; j < ke.GetLength(1); j++)
                    row += ke[i, j].ToString("F4").PadLeft(12);
                Debug.Log(row);
            }

            bool symmetric = true;
            for (int i = 0; i < ke.GetLength(0); i++)
                for (int j = i + 1; j < ke.GetLength(1); j++)
                    if (Mathf.Abs(ke[i, j] - ke[j, i]) > 1e-5f)
                        symmetric = false;

            Debug.Log(symmetric ? "Matrix is symmetric." : "Matrix is not symmetric.");
        }
    }

    void VisualizeMesh()
    {
        if (ShowNodes) CreateNodeVisuals();
        CreateWireframe();
        
        // Only update wireframe lines if dynamics is initialized
        if (wireframeRenderer != null && wireframeEdges.Count > 0 && dynamics != null)
            UpdateWireframeLines();
    }

    void CreateNodeVisuals()
    {
        foreach (var node in nodeVisuals)
            if (node != null) Destroy(node);
        nodeVisuals.Clear();

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
        if (wireframeRenderer != null)
            Destroy(wireframeRenderer.gameObject);

        wireframeEdges.Clear();

        foreach (var tet in mesh.Elements)
        {
            int[] ids = tet.NodeIndices;
            AddEdge(wireframeEdges, ids[0], ids[1]);
            AddEdge(wireframeEdges, ids[0], ids[2]);
            AddEdge(wireframeEdges, ids[0], ids[3]);
            AddEdge(wireframeEdges, ids[1], ids[2]);
            AddEdge(wireframeEdges, ids[1], ids[3]);
            AddEdge(wireframeEdges, ids[2], ids[3]);
        }

        GameObject lineObj = new GameObject("Wireframe");
        lineObj.transform.SetParent(transform);
        wireframeRenderer = lineObj.AddComponent<LineRenderer>();
        wireframeRenderer.material = new Material(Shader.Find("Sprites/Default"));
        wireframeRenderer.startColor = LineColor;
        wireframeRenderer.endColor = LineColor;
        wireframeRenderer.startWidth = LineWidth;
        wireframeRenderer.endWidth = LineWidth;
        wireframeRenderer.useWorldSpace = true;

        // Prevent null access by ensuring a position count
        wireframeRenderer.positionCount = 0;
    }

    void AddEdge(List<(int, int)> edges, int a, int b)
    {
        if (!edges.Contains((a, b)) && !edges.Contains((b, a)))
            edges.Add((a, b));
    }

    void Update()
    {
        if (dynamics == null || solver == null)
            return;

        dynamics.Step(solver.GlobalMassMatrix, solver.GlobalStiffnessMatrix, totalForce);
        UpdateNodeVisuals();

        if (wireframeRenderer != null)
            UpdateWireframeLines();
    }

    void UpdateNodeVisuals()
    {
        for (int i = 0; i < solver.Nodes.Length && i < nodeVisuals.Count; i++)
        {
            if (nodeVisuals[i] == null) continue;

            int baseIndex = i * 3;
            Vector3 displacement = new(
                dynamics.Displacement[baseIndex],
                dynamics.Displacement[baseIndex + 1],
                dynamics.Displacement[baseIndex + 2]);

            if (float.IsNaN(displacement.x) || float.IsNaN(displacement.y) || float.IsNaN(displacement.z))
            {
                Debug.LogError($"NaN detected at node {i}, resetting.");
                for (int d = 0; d < 3; d++)
                {
                    dynamics.Displacement[baseIndex + d] = 0f;
                    dynamics.Velocity[baseIndex + d] = 0f;
                }
                displacement = Vector3.zero;
            }

            nodeVisuals[i].transform.position = solver.Nodes[i].Position + displacement;
        }
    }

    void UpdateWireframeLines()
    {
        if (wireframeRenderer == null || wireframeEdges.Count == 0 || dynamics == null)
            return;

        Vector3[] positions = new Vector3[wireframeEdges.Count * 2];
        for (int i = 0; i < wireframeEdges.Count; i++)
        {
            var (a, b) = wireframeEdges[i];

            Vector3 da = new(
                dynamics.Displacement[a * 3 + 0],
                dynamics.Displacement[a * 3 + 1],
                dynamics.Displacement[a * 3 + 2]);

            Vector3 db = new(
                dynamics.Displacement[b * 3 + 0],
                dynamics.Displacement[b * 3 + 1],
                dynamics.Displacement[b * 3 + 2]);

            positions[i * 2] = solver.Nodes[a].Position + da;
            positions[i * 2 + 1] = solver.Nodes[b].Position + db;
        }

        wireframeRenderer.positionCount = positions.Length;
        wireframeRenderer.SetPositions(positions);
    }

    void OnDestroy()
    {
        foreach (var obj in nodeVisuals) 
            if (obj != null) Destroy(obj);
        if (wireframeRenderer != null) 
            Destroy(wireframeRenderer.gameObject);
    }

    void PrintMassMatrixSummary(float[,] M)
    {
        int rows = M.GetLength(0);
        float sum = 0f;
        bool symmetric = true;

        for (int i = 0; i < rows; i++)
            for (int j = 0; j < rows; j++)
            {
                sum += M[i, j];
                if (Mathf.Abs(M[i, j] - M[j, i]) > 1e-5f)
                    symmetric = false;
            }

        Debug.Log($"[Mass Matrix] {rows}x{rows}, Total Mass = {sum:F4}, Symmetric = {symmetric}");

        for (int i = 0; i < Mathf.Min(6, rows); i++)
        {
            string row = $"Row {i}:";
            for (int j = 0; j < Mathf.Min(6, rows); j++)
                row += M[i, j].ToString("F4").PadLeft(10);
            Debug.Log(row);
        }
    }

    void PrintExpectedMass(Node[] nodes, Tetrahedron[] elements, PhysicalMaterial mat)
    {
        float totalVolume = 0f;
        foreach (var tet in elements)
        {
            Vector3[] verts = new Vector3[4];
            for (int i = 0; i < 4; i++)
                verts[i] = nodes[tet.NodeIndices[i]].Position;

            totalVolume += ElementStiffnessCalculator.ComputeTetrahedronVolume(verts);
        }

        float expectedMass = mat.Density * totalVolume;
        Debug.Log($"Expected mass = {expectedMass:F4} (Total volume = {totalVolume:F4})");
    }
}