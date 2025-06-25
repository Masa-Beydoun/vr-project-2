// using UnityEngine;
// using System.Collections.Generic;
// using System.IO;

// [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
// public class ObjLoader : MonoBehaviour
// {
//     void Start()
//     {
//         // string path = @"F:\fourth\VR projects\model\berrel\Barrel.obj";
//         string path = @"F:\fourth\VR projects\model\couch\couch.obj";
//         ObjMesh obj = new ObjMesh();
//         obj.LoadFromOBJ(path);

//         Mesh mesh = new Mesh();
//         mesh.name = "Imported OBJ";

//         // Set vertices
//         mesh.SetVertices(obj.Vertices);

//         // Flatten face indices into triangle list
//         List<int> triangles = new List<int>();
//         foreach (var face in obj.Faces)
//         {
//             triangles.AddRange(face); // each face has 3 indices
//         }
//         mesh.SetTriangles(triangles, 0);

//         // Recalculate normals for lighting
//         mesh.RecalculateNormals();

//         // Assign mesh to MeshFilter
//         MeshFilter meshFilter = GetComponent<MeshFilter>();
//         meshFilter.mesh = mesh;

//         // Assign material with texture if available
//         MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
//         if (obj.MeshMaterial != null)
//         {
//             meshRenderer.material = obj.MeshMaterial;
//         }
//         else
//         {
//             // fallback: default material
//             meshRenderer.material = new Material(Shader.Find("Standard"));
//         }
//     }
// }
