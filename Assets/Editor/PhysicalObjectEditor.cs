using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhysicalObject))]
public class PhysicalObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PhysicalObject po = (PhysicalObject)target;

        po.materialPreset = (PhysicalMaterial)EditorGUILayout.ObjectField("Material Preset", po.materialPreset, typeof(PhysicalMaterial), false);
        po.mass = EditorGUILayout.FloatField("Mass", po.mass);

        po.shapeType = (ShapeType)EditorGUILayout.EnumPopup("Shape Type", po.shapeType);
        po.massShapeType = (MassShapeType)EditorGUILayout.EnumPopup("Mass Shape Type", po.massShapeType);

        EditorGUILayout.LabelField("Shape Dimensions", EditorStyles.boldLabel);

        switch (po.massShapeType)
        {
            case MassShapeType.Cube:
                po.width = EditorGUILayout.FloatField("Width", po.width);
                po.height = EditorGUILayout.FloatField("Height", po.height);
                po.depth = EditorGUILayout.FloatField("Depth", po.depth);
                break;

            case MassShapeType.Sphere:
                po.radius = EditorGUILayout.FloatField("Radius", po.radius);
                break;

            case MassShapeType.Cylinder:
            case MassShapeType.Capsule:
                po.radius = EditorGUILayout.FloatField("Radius", po.radius);
                po.height = EditorGUILayout.FloatField("Height", po.height);
                break;

            case MassShapeType.Other:
                po.meshSourceObject = (GameObject)EditorGUILayout.ObjectField("Mesh Source Object", po.meshSourceObject, typeof(GameObject), true);
                break;
        }

        po.isStatic = EditorGUILayout.Toggle("Is Static", po.isStatic);
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Physics Properties", EditorStyles.boldLabel);
        po.initialVelocity = EditorGUILayout.Vector3Field("Initial Velocity", po.initialVelocity);
        po.initialForce = EditorGUILayout.Vector3Field("Initial Force", po.initialForce);
        po.dragCoefficient = EditorGUILayout.FloatField("Drag Coefficient", po.dragCoefficient);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Rotation", EditorStyles.boldLabel);
        po.rotationEuler = EditorGUILayout.Vector3Field("Rotation (Euler)", po.rotationEuler);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(po);
        }
    }
}
