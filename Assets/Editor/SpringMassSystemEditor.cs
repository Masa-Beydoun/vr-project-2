using UnityEditor;
using UnityEngine;
using System.Collections;


[CustomEditor(typeof(SpringMassSystem))]
public class SpringMassSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SpringMassSystem system = (SpringMassSystem)target;

        EditorGUILayout.LabelField("Spring Mass System Settings", EditorStyles.boldLabel);

        system.shapeType = (MassShapeType)EditorGUILayout.EnumPopup("Shape Type", system.shapeType);
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

        system.pointPrefab = (GameObject)EditorGUILayout.ObjectField("Point Prefab", system.pointPrefab, typeof(GameObject), false);
        system.springLineMaterial = (Material)EditorGUILayout.ObjectField("Spring Line Material", system.springLineMaterial, typeof(Material), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shape Dimensions", EditorStyles.boldLabel);

        switch (system.shapeType)
        {
            case MassShapeType.Cube:
                system.width = EditorGUILayout.FloatField("Width", system.width);
                system.height = EditorGUILayout.FloatField("Height", system.height);
                system.depth = EditorGUILayout.FloatField("Depth", system.depth);
                break;

            case MassShapeType.Sphere:
                system.radius = EditorGUILayout.FloatField("Radius", system.radius);
                break;

            case MassShapeType.Cylinder:
            case MassShapeType.Capsule:
                system.radius = EditorGUILayout.FloatField("Radius", system.radius);
                system.height = EditorGUILayout.FloatField("Height", system.height);
                break;

            case MassShapeType.Other:
                system.meshSourceObject = (GameObject)EditorGUILayout.ObjectField("Mesh Source Object", system.meshSourceObject, typeof(GameObject), true);

                system.useVoxelFilling = EditorGUILayout.Toggle("Use Voxel Filling", system.useVoxelFilling);

                system.meshConnectionMode = (MeshConnectionMode)EditorGUILayout.EnumPopup("Mesh Connection Mode", system.meshConnectionMode);

                if (system.meshConnectionMode == MeshConnectionMode.KNearestNeighbors)
                {
                    system.k = EditorGUILayout.IntField("K Nearest Neighbors", system.k);
                }
                system.generationMode = (MeshPointGenerationMode)EditorGUILayout.EnumPopup("Generation Mode", system.generationMode);

                break;


        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(system);
        }
    }
}
