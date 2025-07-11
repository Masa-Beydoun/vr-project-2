using UnityEngine;
using UnityEngine.UIElements;

public class ObjectCreatorUI : MonoBehaviour
{
    private Button addObjectButton;

    void OnEnable()
    {
        // Get root from UI Document
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Get reference to button by name
        addObjectButton = root.Q<Button>("add-object-button");

        if (addObjectButton != null)
        {
            addObjectButton.clicked += OnAddObjectClicked;
        }
        else
        {
            Debug.LogWarning("Button not found!");
        }
    }

    private void OnAddObjectClicked()
    {
        Debug.Log("Add Object clicked!");

        // You can call a method to show a property panel or directly create a physical object
        GameObject newObj = new GameObject("NewPhysicalObject");
        var phys = newObj.AddComponent<PhysicalObject>();
        newObj.AddComponent<SpringMassSystem>(); // or your default algorithm
        newObj.transform.position = Vector3.zero;
    }
}
