using UnityEngine;

public class CameraAndObjectController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float boostMultiplier = 2f;
    public LayerMask selectableLayer;

    private Transform selectedObject = null;
    private bool moveCamera = true;

    void Update()
    {
        HandleSelection();
        HandleMovement();
        HandleToggle();
    }

    void HandleSelection()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, selectableLayer))
            {
                selectedObject = hit.transform;
                Debug.Log("Selected: " + selectedObject.name);
            }
        }
    }

    void HandleToggle()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            moveCamera = !moveCamera;
            Debug.Log(moveCamera ? "Camera Mode" : "Object Mode");
        }
    }

    void HandleMovement()
    {
        Vector3 direction = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) direction += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) direction += Vector3.back;
        if (Input.GetKey(KeyCode.A)) direction += Vector3.left;
        if (Input.GetKey(KeyCode.D)) direction += Vector3.right;
        if (Input.GetKey(KeyCode.E)) direction += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) direction += Vector3.down;

        if (direction != Vector3.zero)
        {
            float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? boostMultiplier : 1f);
            Vector3 move = direction.normalized * speed * Time.deltaTime;

            if (moveCamera)
            {
                transform.Translate(move, Space.Self);
            }
            else if (selectedObject != null)
            {
                selectedObject.Translate(move, Space.World);
            }
        }
    }
}
