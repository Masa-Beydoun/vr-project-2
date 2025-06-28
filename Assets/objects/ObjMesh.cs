// using UnityEngine;
// using System;
// using System.IO;
// using System.Collections.Generic;

// public class ObjMesh
// {
//     public List<Vector3> Vertices = new();
//     public List<int[]> Faces = new();

//     public Texture2D DiffuseTexture;
//     public Material MeshMaterial;

//     public void LoadFromOBJ(string objPath)
//     {
//         Vertices.Clear();
//         Faces.Clear();

//         string[] lines = File.ReadAllLines(objPath);
//         string mtlFile = null;
//         string objDirectory = Path.GetDirectoryName(objPath);

//         foreach (string line in lines)
//         {
//             if (line.StartsWith("mtllib "))
//             {
//                 mtlFile = line.Substring(7).Trim();
//             }
//             else if (line.StartsWith("v "))
//             {
//                 string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
//                 float x = float.Parse(parts[1]);
//                 float y = float.Parse(parts[2]);
//                 float z = float.Parse(parts[3]);
//                 Vertices.Add(new Vector3(x, y, z));
//             }
//             else if (line.StartsWith("f "))
//             {
//                 string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
//                 int a = int.Parse(parts[1].Split('/')[0]) - 1;
//                 int b = int.Parse(parts[2].Split('/')[0]) - 1;
//                 int c = int.Parse(parts[3].Split('/')[0]) - 1;
//                 Faces.Add(new int[] { a, b, c });
//             }
//         }

//         if (!string.IsNullOrEmpty(mtlFile))
//         {
//             LoadMTL(Path.Combine(objDirectory, mtlFile));
//         }

//         Debug.Log($"Loaded OBJ: {Vertices.Count} vertices, {Faces.Count} faces");
//     }

//     private void LoadMTL(string mtlPath)
//     {
//         if (!File.Exists(mtlPath)) return;

//         string[] lines = File.ReadAllLines(mtlPath);
//         string textureFile = null;
//         foreach (string line in lines)
//         {
//             if (line.StartsWith("map_Kd "))
//             {
//                 textureFile = line.Substring(7).Trim();
//                 break;
//             }
//         }

//         if (textureFile != null)
//         {
//             string texturePath = Path.Combine(Path.GetDirectoryName(mtlPath), textureFile);
//             if (File.Exists(texturePath))
//             {
//                 byte[] data = File.ReadAllBytes(texturePath);
//                 DiffuseTexture = new Texture2D(2, 2);
//                 DiffuseTexture.LoadImage(data);

//                 MeshMaterial = new Material(Shader.Find("Standard"));
//                 MeshMaterial.mainTexture = DiffuseTexture;
//             }
//         }
//     }
// }
