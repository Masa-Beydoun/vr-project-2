using UnityEngine;

public class KeyboardMover : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal"); 
        float moveZ = Input.GetAxis("Vertical");   

        Vector3 move = new Vector3(moveX, 0f, moveZ) * moveSpeed * Time.deltaTime;

        transform.Translate(move, Space.World);
    }
}
