using UnityEngine;
using System.Collections.Generic;

public class FEMController : MonoBehaviour
{
    [Header("Creation Control")]
    public bool isCreated = false;
    private bool previousIsCreated = false;

    [Header("Visualization")]
    public bool ShowNodes = true;
    public float NodeScale = 0.1f;
    public Color LineColor = Color.green;
    public float LineWidth = 0.02f;

    [Header("FEM-Specific Settings")]
    public float SphereResolution = 0.1f;
    public float CylinderResolution = 0.1f;
    public float CapsuleResolution = 0.1f;

    private PhysicalObject physicalObject;
    private Mesh3D mesh;
    private LineRenderer wireframeRenderer;
    private List<GameObject> nodeVisuals = new();
    private List<float[,]> localStiffnessMatrices = new();
    private FEMSolver solver;
    private FEMDynamics dynamics;
    private float[] totalForce;

    private List<(int, int)> wireframeEdges = new();

    void Start()
    {
        // Get the PhysicalObject component
        physicalObject = GetComponent<PhysicalObject>();
        if (physicalObject == null)
        {
            Debug.LogError("FEMController requires a PhysicalObject component on the same GameObject.");
            return;
        }

        // Only initialize if isCreated is true at startup
        if (isCreated && physicalObject.materialPreset != null)
        {
            Initialize();
        }
        else if (physicalObject.materialPreset == null)
        {
            Debug.LogWarning("PhysicalObject material not assigned. Click 'Initialize FEM' in Inspector.");
        }
    }

    void Update()
    {
        // Detect when isCreated changes from false to true
        if (isCreated && !previousIsCreated)
        {
            if (physicalObject != null && physicalObject.materialPreset != null)
            {
                Initialize();
            }
            else
            {
                Debug.LogWarning("Cannot initialize FEM: PhysicalObject or material not assigned.");
                isCreated = false; // Reset if can't initialize
            }
        }

        // Update previous state for next frame
        previousIsCreated = isCreated;

        // Only run simulation if created
        if (!isCreated) return;

        // Run FEM simulation
        if (dynamics != null && solver != null)
        {
            dynamics.Step(solver.GlobalMassMatrix, solver.GlobalStiffnessMatrix, totalForce);
            UpdateNodeVisuals();

            if (wireframeRenderer != null)
                UpdateWireframeLines();
        }
    }

    public void Initialize()
    {
        // Prevent reinitialization if already created
        if (solver != null && dynamics != null) return;

        // Only proceed if isCreated is true
        if (!isCreated) return;

        if (physicalObject == null)
        {
            Debug.LogError("PhysicalObject component not found.");
            return;
        }

        if (physicalObject.materialPreset == null)
        {
            Debug.LogError("PhysicalMaterial not assigned to PhysicalObject.");
            return;
        }

        Debug.Log("Initializing FEM System...");

        InitializeMesh();

        solver = new FEMSolver
        {
            Nodes = mesh.Nodes,
            Tetrahedra = mesh.Elements
        };

        ComputeLocalStiffness();
        solver.AssembleGlobalStiffness(physicalObject.materialPreset);

        solver.GlobalMassMatrix = MassMatrixAssembler.Assemble(
            solver.Nodes,
            solver.Tetrahedra,
            physicalObject.materialPreset
        );

        dynamics = new FEMDynamics();
        dynamics.Initialize(solver.Nodes.Length * 3);

        // Use initial velocity from PhysicalObject
        Vector3 initialVelocity = physicalObject.initialVelocity;
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
            physicalObject.materialPreset.Density,
            gravity
        );

        // Apply initial force from PhysicalObject
        ApplyInitialForce();

        VisualizeMesh();

        PrintMassMatrixSummary(solver.GlobalMassMatrix);
        PrintExpectedMass(solver.Nodes, solver.Tetrahedra, physicalObject.materialPreset);

        // SetupBoundingBox();

        Debug.Log("FEM System initialized successfully!");
    }

    void InitializeMesh()
    {
        mesh = new Mesh3D();

        // Use shape information from PhysicalObject
        switch (physicalObject.massShapeType)
        {
            case MassShapeType.Cube:
                Vector3 cubeSize = new Vector3(physicalObject.width, physicalObject.height, physicalObject.depth);
                mesh.GenerateCubeMesh(cubeSize, transform.position);
                break;

            case MassShapeType.Sphere:
                mesh.GenerateSphereMesh(physicalObject.radius, SphereResolution, transform.position);
                break;

            case MassShapeType.Cylinder:
                // Now using proper cylinder mesh generation
                mesh.GenerateCylinderMesh(physicalObject.radius, physicalObject.height, CylinderResolution, transform.position);
                break;

            case MassShapeType.Capsule:
                // Now using proper capsule mesh generation
                mesh.GenerateCapsuleMesh(physicalObject.radius, physicalObject.height, CapsuleResolution, transform.position);
                break;

            case MassShapeType.Other:
                // For custom meshes, you might need to extract mesh data from meshSourceObject
                if (physicalObject.meshSourceObject != null)
                {
                    // Default to cube for now, but you should implement custom mesh handling
                    Vector3 defaultSize = new Vector3(physicalObject.width, physicalObject.height, physicalObject.depth);
                    mesh.GenerateCubeMesh(defaultSize, transform.position);
                    Debug.LogWarning("Custom mesh shape using cube approximation. Consider implementing custom mesh support.");
                }
                else
                {
                    Debug.LogError("Custom mesh selected but no meshSourceObject assigned.");
                    Vector3 defaultSize = Vector3.one;
                    mesh.GenerateCubeMesh(defaultSize, transform.position);
                }
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
            float[,] ke = ElementStiffnessCalculator.ComputeLocalStiffness(vertices, physicalObject.materialPreset);
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

    void ApplyInitialForce()
    {
        if (physicalObject.initialForce == Vector3.zero) return;

        // Apply initial force to all nodes
        Vector3 forcePerNode = physicalObject.initialForce / solver.Nodes.Length;

        for (int i = 0; i < solver.Nodes.Length; i++)
        {
            int baseIndex = i * 3;
            totalForce[baseIndex] += forcePerNode.x;
            totalForce[baseIndex + 1] += forcePerNode.y;
            totalForce[baseIndex + 2] += forcePerNode.z;
        }
    }

    // void SetupBoundingBox()
    // {
    //     BoundingBoxDrawer bbox = gameObject.GetComponent<BoundingBoxDrawer>();
    //     if (bbox == null)
    //         bbox = gameObject.AddComponent<BoundingBoxDrawer>();

    //     switch (physicalObject.massShapeType)
    //     {
    //         case MassShapeType.Cube:
    //             bbox.isSphere = false;
    //             bbox.size = new Vector3(physicalObject.width, physicalObject.height, physicalObject.depth);
    //             break;
    //         case MassShapeType.Sphere:
    //             bbox.isSphere = true;
    //             bbox.size = Vector3.one * physicalObject.radius * 2f;
    //             break;
    //         case MassShapeType.Cylinder:
    //             bbox.isSphere = false;
    //             bbox.size = new Vector3(physicalObject.radius * 2f, physicalObject.height, physicalObject.radius * 2f);
    //             break;
    //         case MassShapeType.Capsule:
    //             bbox.isSphere = false;
    //             bbox.size = new Vector3(physicalObject.radius * 2f, physicalObject.height, physicalObject.radius * 2f);
    //             break;
    //         case MassShapeType.Other:
    //             bbox.isSphere = false;
    //             bbox.size = new Vector3(physicalObject.width, physicalObject.height, physicalObject.depth);
    //             break;
    //     }
    // }

    void VisualizeMesh()
    {
        if (ShowNodes) CreateNodeVisuals();
        CreateWireframe();
        if (wireframeRenderer != null && wireframeEdges.Count > 0 && dynamics != null)
            UpdateWireframeLines();
    }

    void CreateNodeVisuals()
    {
        // Destroy previous node visuals if they exist
        foreach (var node in nodeVisuals)
            if (node != null) Destroy(node);
        nodeVisuals.Clear();

        // Create new node visuals
        foreach (var node in mesh.Nodes)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = node.Position;
            sphere.transform.localScale = Vector3.one * NodeScale;

            // ✅ Set FEMController's GameObject as parent
            sphere.transform.SetParent(this.transform);

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
        wireframeRenderer.positionCount = 0;
    }

    void AddEdge(List<(int, int)> edges, int a, int b)
    {
        if (!edges.Contains((a, b)) && !edges.Contains((b, a)))
            edges.Add((a, b));
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
        DestroyFEMSystem();
    }

    // Public method to manually create the FEM system
    public void ManuallyCreateSystem()
    {
        isCreated = true;
        // Initialize will be called automatically in the next Update()
    }

    // Public method to destroy the FEM system
    public void DestroyFEMSystem()
    {
        isCreated = false;
        previousIsCreated = false;

        // Clean up all FEM data
        solver = null;
        dynamics = null;
        mesh = null;
        totalForce = null;
        localStiffnessMatrices?.Clear();
        wireframeEdges?.Clear();

        // Clean up visual objects
        foreach (var obj in nodeVisuals)
            if (obj != null) Destroy(obj);
        nodeVisuals.Clear();

        if (wireframeRenderer != null)
        {
            Destroy(wireframeRenderer.gameObject);
            wireframeRenderer = null;
        }

        Debug.Log("FEM System destroyed");
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

    public Node[] GetAllNodes()
    {
        return this.mesh.Nodes;
    }

    public Vector3 Center
    {
        get
        {
            Node[] nodes = this.GetAllNodes();
            if (nodes == null || nodes.Length == 0)
                return transform.position;

            Vector3 sum = Vector3.zero;
            foreach (var node in nodes)
                sum += node.Position;

            return sum / nodes.Length;
        }
    }
}