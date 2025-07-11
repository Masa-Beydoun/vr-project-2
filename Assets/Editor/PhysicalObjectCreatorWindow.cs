using UnityEditor;
using UnityEngine;

public class PhysicalObjectCreatorWindow : EditorWindow
{
    private GameObject selectedPrefab;
    private string objectName = "New Physical Object";
    private Vector3 initialPosition = Vector3.zero;

    [MenuItem("Tools/Physical Object Creator")]
    public static void ShowWindow()
    {
        GetWindow<PhysicalObjectCreatorWindow>("Create Physical Object");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create a New Physical Object", EditorStyles.boldLabel);

        objectName = EditorGUILayout.TextField("Name", objectName);
        initialPosition = EditorGUILayout.Vector3Field("Initial Position", initialPosition);

        GUILayout.Space(10);
        selectedPrefab = (GameObject)EditorGUILayout.ObjectField("Point Prefab", selectedPrefab, typeof(GameObject), false);

        GUILayout.Space(10);

        if (GUILayout.Button("Create Physical Object"))
        {
            CreatePhysicalObject();
        }
    }

    private void CreatePhysicalObject()
    {
        // Create an empty GameObject
        GameObject obj = new GameObject(objectName);
        obj.transform.position = initialPosition;

        // Attach required components
        var physicalObject = obj.AddComponent<PhysicalObject>();
        var springMassSystem = obj.AddComponent<SpringMassSystem>();

        // Optional: assign the point prefab if provided
        if (selectedPrefab != null)
        {
            springMassSystem.pointPrefab = selectedPrefab;
        }

        // Focus on the new object
        Selection.activeGameObject = obj;
        EditorGUIUtility.PingObject(obj);

        Debug.Log($"Created new PhysicalObject: {obj.name}");
    }
}
