using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpringMassSystem))]
public class SpringMassSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SpringMassSystem system = (SpringMassSystem)target;

        EditorGUILayout.LabelField("Spring Mass System Settings", EditorStyles.boldLabel);

        system.shapeType = (MassShapeType)EditorGUILayout.EnumPopup("Shape Type", system.shapeType);
        system.resolution = EditorGUILayout.IntField("Resolution", system.resolution);
        system.springStiffness = EditorGUILayout.FloatField("Spring Stiffness", system.springStiffness);
        system.springDamping = EditorGUILayout.FloatField("Spring Damping", system.springDamping);
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
                system.k = EditorGUILayout.IntField("K nearest neighbors", system.k);
                break;
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(system);
        }
    }
}
