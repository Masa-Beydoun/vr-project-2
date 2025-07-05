using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpringMassSystem))]
public class SpringMassSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SpringMassSystem system = (SpringMassSystem)target;

        EditorGUILayout.LabelField("Spring Mass System Settings", EditorStyles.boldLabel);

        // Show shape type enum selector so user can pick shape
        system.massShapeType = (MassShapeType)EditorGUILayout.EnumPopup("Shape Type", system.massShapeType);

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

        // Debug display current shape type
        EditorGUILayout.LabelField("Current Shape Type: " + system.massShapeType.ToString());

        // Show extra mesh options ONLY if shapeType is Other
        if (system.massShapeType == MassShapeType.Other)
        {
            system.useVoxelFilling = EditorGUILayout.Toggle("Use Voxel Filling", system.useVoxelFilling);

            system.meshConnectionMode = (MeshConnectionMode)EditorGUILayout.EnumPopup("Mesh Connection Mode", system.meshConnectionMode);

            if (system.meshConnectionMode == MeshConnectionMode.KNearestNeighbors)
            {
                system.k = EditorGUILayout.IntField("K Nearest Neighbors", system.k);
            }
            

            system.generationMode = (MeshPointGenerationMode)EditorGUILayout.EnumPopup("Generation Mode", system.generationMode);
        }


        if (GUI.changed)
        {
            EditorUtility.SetDirty(system);
        }
    }
}
