using UnityEngine;
using Mirror;

public class PaddleMovement : NetworkBehaviour
{
    public float speed = 10f; // Speed of the paddle movement
    private Camera activeCamera; // The active camera for the local player
    public float xMin = -1f; // Left boundary of the paddle movement
    public float xMax = 1f;  // Right boundary of the paddle movement
    public float yMin = 0f;  // Lower boundary of the paddle movement
    public float yMax = 1f;  // Upper boundary of the paddle movement

    public Material player1Material; // Material for the first player
    public Material player2Material; // Material for the second player

    [SyncVar] public int playerIndex; // Assigned by the server to indicate player order

    void Start()
    {
        if (isLocalPlayer)
        {
            // Find the enabled camera for the local player
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                if (cam.enabled)
                {
                    activeCamera = cam;
                    break;
                }
            }

            if (activeCamera == null)
            {
                Debug.LogError("No enabled camera found for the local player.");
            }
            Debug.Log("player index: " + playerIndex);
        }

        // Assign the appropriate material based on the player's netId
        AssignMaterial();
    }

    void Update()
    {
        if (isLocalPlayer && activeCamera != null)
        {
            MovePaddleWithMouse();
        }

        if (isLocalPlayer)
        {
            // Find the enabled camera for the local player
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                if (cam.enabled)
                {
                    activeCamera = cam;
                    break;
                }
            }

            if (activeCamera == null)
            {
                Debug.LogError("No enabled camera found for the local player.");
            }
        }
    }

    void MovePaddleWithMouse()
    {
        Vector3 mousePos = Input.mousePosition;

        float distanceFromCamera = Mathf.Abs(activeCamera.transform.position.z - transform.position.z);
        mousePos = activeCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, distanceFromCamera));

        mousePos.x = Mathf.Clamp(mousePos.x, xMin, xMax);
        mousePos.y = Mathf.Clamp(mousePos.y, yMin, yMax);
        mousePos.z = transform.position.z;

        // Debug.Log($"{netId}: Calculated mouse position: {mousePos}");

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.MovePosition(Vector3.Lerp(transform.position, mousePos, speed * Time.deltaTime));
            // Debug.Log($"{netId}: Paddle moved to: {mousePos}");
        }
        else
        {
            Debug.LogError("Rigidbody not found on paddle.");
        }

        // transform.position = Vector3.Lerp(transform.position, mousePos, speed * Time.deltaTime);
        // Debug.Log($"{netId}: Paddle moved to: {mousePos}");
    }

    void AssignMaterial()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("Renderer not found on the paddle prefab.");
            return;
        }

        if (playerIndex == 1)
        {
            renderer.material = player1Material;
        }
        else if (playerIndex == 2)
        {
            renderer.material = player2Material;
        }
    }
}
