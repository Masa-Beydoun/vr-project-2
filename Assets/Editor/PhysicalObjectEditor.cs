//using UnityEditor;
//using UnityEngine;

//[CustomEditor(typeof(PhysicalObject))]
//public class PhysicalObjectEditor : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        PhysicalObject obj = (PhysicalObject)target;

//        obj.algorithm = (SimulationAlgorithm)EditorGUILayout.EnumPopup("Simulation Algorithm", obj.algorithm);
//        obj.shapeType = (MassShapeType)EditorGUILayout.EnumPopup("Shape Type", obj.shapeType);

//        switch (obj.shapeType)
//        {
//            case MassShapeType.Cube:
//                obj.width = EditorGUILayout.FloatField("Width", obj.width);
//                obj.height = EditorGUILayout.FloatField("Height", obj.height);
//                obj.depth = EditorGUILayout.FloatField("Depth", obj.depth);
//                break;
//            case MassShapeType.Sphere:
//                obj.radius = EditorGUILayout.FloatField("Radius", obj.radius);
//                break;
//            case MassShapeType.Cylinder:
//            case MassShapeType.Capsule:
//                obj.radius = EditorGUILayout.FloatField("Radius", obj.radius);
//                obj.height = EditorGUILayout.FloatField("Height", obj.height);
//                break;
//        }

//        obj.resolution = EditorGUILayout.IntField("Resolution", obj.resolution);
//        obj.k = EditorGUILayout.IntField("K Nearest Neighbors (Mesh)", obj.k);
//        obj.pointPrefab = (GameObject)EditorGUILayout.ObjectField("Point Prefab", obj.pointPrefab, typeof(GameObject), false);
//        obj.springLineMaterial = (Material)EditorGUILayout.ObjectField("Spring Line Material", obj.springLineMaterial, typeof(Material), false);

//        if (GUI.changed)
//        {
//            EditorUtility.SetDirty(obj);
//        }
//    }
//}
