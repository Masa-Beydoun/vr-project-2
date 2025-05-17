using UnityEngine;

public class FEMController : MonoBehaviour
{
    private Mesh3D mesh;

    void Start()
    {
        mesh = new Mesh3D();
        mesh.GenerateSimpleCubeMesh();

        Debug.Log("Generated FEM mesh with " + mesh.Nodes.Count + " nodes and " + mesh.Elements.Count + " tetrahedrons.");

        // visualize nodes
        foreach (var node in mesh.Nodes)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = node.Position;
            sphere.transform.localScale = Vector3.one * 0.1f;
            sphere.name = $"Node_{node.Id}";
        }
    }
}
