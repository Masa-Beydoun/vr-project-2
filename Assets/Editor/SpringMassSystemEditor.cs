using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpringMassSystem))]
public class SpringMassSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SpringMassSystem system = (SpringMassSystem)target;

        EditorGUILayout.LabelField("Spring Mass System Settings", EditorStyles.boldLabel);

        system.resolution = EditorGUILayout.IntField("Resolution", system.resolution);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spring Configuration", EditorStyles.boldLabel);

        

        system.isCreated = EditorGUILayout.Toggle("Is Created", system.isCreated);

        system.pointPrefab = (GameObject)EditorGUILayout.ObjectField("Point Prefab", system.pointPrefab, typeof(GameObject), false);
        system.springLineMaterial = (Material)EditorGUILayout.ObjectField("Spring Line Material", system.springLineMaterial, typeof(Material), false);

        EditorGUILayout.Space();


        // Show extra mesh options ONLY if shapeType is Other
        if (system.physicalObject != null && system.physicalObject.massShapeType == MassShapeType.Other)
        {
                system.generationMode = (MeshPointGenerationMode)EditorGUILayout.EnumPopup("Generation Mode", system.generationMode);
            system.useVoxelFilling = EditorGUILayout.Toggle("Use Voxel Filling", system.useVoxelFilling);

            system.meshConnectionMode = (MeshConnectionMode)EditorGUILayout.EnumPopup("Mesh Connection Mode", system.meshConnectionMode);

            if (system.meshConnectionMode != MeshConnectionMode.TriangleEdges)
            {
                system.k = EditorGUILayout.IntField("K Nearest Neighbors", system.k);
            }
            

            
        }


        if (GUI.changed)
        {
            EditorUtility.SetDirty(system);
        }
    }
}
