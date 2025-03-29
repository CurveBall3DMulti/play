using UnityEngine;
using Mirror;
using System.Collections;

public class Ball : NetworkBehaviour
{
    public float speed = 10f; // Initial speed

    public float speedIncrementPerSec = 0.5f;
    private float currentSpeed; // Current speed that increases over time
    private Vector3 direction;
    private Rigidbody rb;

    private ScoreMenu scoreMenu;

    [SyncVar] private Vector3 syncedPosition;
    [SyncVar] private Vector3 syncedVelocity;

    [SyncVar] private bool isPaused;

    public override void OnStartServer()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) 
        {
            Debug.LogError("Rigidbody is missing from the ball object!");
            return;
        }
        ResetBall();
        StartCoroutine(IncreaseSpeedOverTime());
        ScoreMenu[] scoreMenus = FindObjectsByType<ScoreMenu>(FindObjectsSortMode.None);

        if (scoreMenus.Length > 0){
            scoreMenu = FindObjectsByType<ScoreMenu>(FindObjectsSortMode.None)[0]; 
        }
    }

    [Server]
    public void ResetBall()
    {   
        isPaused = false;
        if (scoreMenu == null)
        {
            scoreMenu = FindObjectsByType<ScoreMenu>(FindObjectsSortMode.None)[0];
            Debug.LogError("ScoreMenu instance not found!");
        }
        else{
            if (rb.transform.position.z < 0)
            {
                // Point for blue
                scoreMenu.AddPointToBlue();
            }
            else if (rb.transform.position.z > 0)
            {
                // Point for red
                scoreMenu.AddPointToRed();
            }
        }

        // Reset the ball's speed to the initial value
        currentSpeed = speed;

        // Reset the ball's position and direction
        rb.transform.position = new Vector3(0, 1, 0);
        direction = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.2f, 0.2f), Random.Range(0, 2) == 0 ? -1 : 1).normalized;
        rb.linearVelocity = direction * currentSpeed;
    }

    [Server]
    public void PauseBall(){
        isPaused = true;
        rb.linearVelocity = Vector3.zero;
    }

    [Server]
    public void PlayBall(){
        isPaused = false;
        rb.linearVelocity = direction * currentSpeed;
    }

    [ServerCallback]
    void OnCollisionEnter(Collision collision)
    {
        if (isPaused) return;
        if (collision.gameObject.CompareTag("Paddle"))
        {
            float hitFactor = (transform.position.x - collision.transform.position.x) / collision.transform.localScale.x;
            direction = new Vector3(hitFactor, direction.y, -direction.z).normalized;

            // Update velocity after direction change
            rb.linearVelocity = direction * currentSpeed;
        }
        else if (collision.gameObject.CompareTag("SideWall"))
        {
            direction.x = -direction.x;
            rb.linearVelocity = direction * currentSpeed;
        }
        else if (collision.gameObject.CompareTag("TopWall"))
        {
            direction.y = -direction.y;
            rb.linearVelocity = direction * currentSpeed;
        }
        else if (collision.gameObject.CompareTag("ResetBall")){
            ResetBall();
        }
    }

    // Override this method to make sure the velocity is never changed on the client side
    public override void OnStartClient()
    {
        if (isLocalPlayer)
        {
            rb.isKinematic = false;
        }
    }

    // For manual synchronization, we can override FixedUpdate to handle the movement
    [Server]
    private void FixedUpdate()
    {
        if (!isServer) return;

        // Update the ball's position and velocity on the server
        syncedPosition = rb.transform.position;
        syncedVelocity = rb.linearVelocity;

        if (isPaused) return;
        rb.linearVelocity = direction * currentSpeed;
    }

    private void Update()
    {
        if (!isServer && !isPaused)
        {
            // Smoothly interpolate the ball's position and velocity on the client
            rb.transform.position = Vector3.Lerp(rb.transform.position, syncedPosition, Time.deltaTime * 10f);
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, syncedVelocity, Time.deltaTime * 10f);
        }
        if (scoreMenu == null)
        {
            scoreMenu = FindObjectsByType<ScoreMenu>(FindObjectsSortMode.None)[0];
            Debug.LogError("ScoreMenu instance not found!");
        }
    }

    [Server]
    private IEnumerator IncreaseSpeedOverTime()
    {
        while (true)
        {
            if (!isPaused){
                yield return new WaitForSeconds(1f);
                currentSpeed += speedIncrementPerSec; // Increment speed by speedIncrementPerSec every second
                Debug.Log($"Server: Ball speed increased to {currentSpeed}");
            }
            
        }
    }
}
