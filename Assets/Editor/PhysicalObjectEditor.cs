using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhysicalObject))]
public class PhysicalObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PhysicalObject po = (PhysicalObject)target;

        po.useCustomSpringProperties = EditorGUILayout.Toggle("Use Custom Properties", po.useCustomSpringProperties);

        if (!po.useCustomProperties)
        {
            po.material = (PhysicalMaterial)EditorGUILayout.ObjectField("Material", po.material, typeof(PhysicalMaterial), false);

            if (po.material != null)
            {
                EditorGUILayout.LabelField("Material Properties", EditorStyles.boldLabel);
                EditorGUILayout.FloatField("Density", po.material.Density);
                EditorGUILayout.FloatField("Bounciness", po.material.bounciness);
                EditorGUILayout.FloatField("Stiffness", po.material.stiffness);
                EditorGUILayout.FloatField("Fracture Threshold", po.material.fractureThreshold);
                EditorGUILayout.FloatField("Damping", po.material.dampingCoefficient);
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a PhysicalMaterial to use default properties.", MessageType.Warning);
            }
        }
        else
        {
            po.customDensity = EditorGUILayout.FloatField("Custom Density", po.customDensity);
            po.customBounciness = EditorGUILayout.FloatField("Custom Bounciness", po.customBounciness);
            po.customStiffness = EditorGUILayout.FloatField("Custom Stiffness", po.customStiffness);
            po.customFractureThreshold = EditorGUILayout.FloatField("Custom Fracture Threshold", po.customFractureThreshold);
            po.customDamping = EditorGUILayout.FloatField("Custom Damping", po.customDamping);
        }
        po.mass = EditorGUILayout.FloatField("Mass", po.mass);

        po.shapeType = (BoundingShapeType)EditorGUILayout.EnumPopup("Bounding Shape Type", po.shapeType);
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
