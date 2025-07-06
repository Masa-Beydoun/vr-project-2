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

        system.useCustomSpringProperties = EditorGUILayout.Toggle("Use Custom Properties", system.useCustomSpringProperties);

        if (system.useCustomSpringProperties)
        {
            system.springStiffness = EditorGUILayout.FloatField("Spring Stiffness", system.springStiffness);
            system.springDamping = EditorGUILayout.FloatField("Spring Damping", system.springDamping);
        }
        else
        {
            system.materialPreset = (PhysicalMaterial)EditorGUILayout.ObjectField("Material Preset", system.materialPreset, typeof(PhysicalMaterial), false);

            if (system.materialPreset != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Preview from Material", EditorStyles.boldLabel);
                EditorGUILayout.FloatField("Stiffness", system.materialPreset.stiffness);
                EditorGUILayout.FloatField("Density", system.materialPreset.Density);
                EditorGUILayout.FloatField("Drag Coefficient", system.materialPreset.dragCoefficient);
                EditorGUILayout.FloatField("Bounciness", system.materialPreset.bounciness);
                EditorGUILayout.FloatField("Fracture Threshold", system.materialPreset.fractureThreshold);
            }
        }

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
