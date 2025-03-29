using UnityEngine;
using Mirror;
using System.Collections;

public class Ball : NetworkBehaviour
{
    public static Ball Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public float speed = 10f; // Initial speed
    public float speedIncrementPerSec = 0.5f;
    public float curveDecayRate = 0.05f; // Rate at which the curve force decays over time
    public float curveFactor = 2f; // Factor to control the amount of curve applied
    public float playerSpeedForCurve = 0.5f; // Minimum paddle speed required to apply curve
    public float xAxisBounceStrength = 0.5f;
    public float yAxisBounceStrength = 0.5f; // Strength to reduce y-axis bounce
    public float maxCurveStrength = 10f; // Maximum allowable curve strength
    public float interpolationDelay = 5f;
    public bool clampCurve = true;
    public float curveBuildupTime = 0.5f; // Time (in seconds) for the curve to reach full strength
    private float curveBuildupTimer = 0f; // Timer to track the buildup progress
    private bool isCurveBuilding = false; // Whether the curve is currently building up
    private Vector3 targetCurveForce; // The full-strength curve force to build up to
    private float currentSpeed; // Current speed that increases over time
    private Vector3 direction;
    [SyncVar]
    private Vector3 curveForce; // The current curve force applied to the ball
    private Rigidbody rb;

    private ScoreMenu scoreMenu;

    [SyncVar] private Vector3 syncedPosition;
    [SyncVar] private Vector3 syncedVelocity;

    [SyncVar] private bool isPaused;

    [SyncVar] private Vector3 paddle2Velocity;

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

        if (scoreMenus.Length > 0)
        {
            scoreMenu = FindObjectsByType<ScoreMenu>(FindObjectsSortMode.None)[0];
        }
    }

    [Server]
    public void ResetBall()
    {
        isPaused = false;
        curveForce = Vector3.zero; // Reset the curve force
        if (scoreMenu == null)
        {
            scoreMenu = FindObjectsByType<ScoreMenu>(FindObjectsSortMode.None)[0];
            Debug.LogError("ScoreMenu instance not found!");
        }
        else
        {
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
        rb.transform.position = new Vector3(0, 0.95f, 0);
        // direction = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.2f, 0.2f), Random.Range(0, 2) == 0 ? -1 : 1).normalized;
        direction = new Vector3(0, 0, Random.Range(0, 2) == 0 ? -1 : 1).normalized;
        rb.linearVelocity = direction * currentSpeed;
    }

    [Server]
    public void PauseBall()
    {
        isPaused = true;
        rb.linearVelocity = Vector3.zero;
    }

    [Server]
    public void PlayBall()
    {
        isPaused = false;
        rb.linearVelocity = direction * currentSpeed;
    }

    [Server]
    public void UpdatePaddle2Velocity(Vector3 paddleVelocity)
    {
        paddle2Velocity = paddleVelocity;
    }

    [ServerCallback]
    void OnCollisionEnter(Collision collision)
    {
        if (isPaused) return;

        if (collision.gameObject.CompareTag("Paddle"))
        {
            if (collision.gameObject.transform.position.z < 0){
                Vector3 paddleVelocity = paddle2Velocity;
                Debug.Log($"Paddle 2 Velocity: {paddleVelocity}");

                // Check if the paddle is moving fast enough in the x-direction
                if (Mathf.Abs(paddleVelocity.x) > playerSpeedForCurve)
                {
                    // Calculate the target curve force based on the paddle's horizontal velocity
                    float targetX = paddleVelocity.x * -curveFactor;
                    targetCurveForce.x = clampCurve ? Mathf.Clamp(targetX, -maxCurveStrength, maxCurveStrength) : targetX;

                    // Start the curve buildup
                    isCurveBuilding = true;
                    curveBuildupTimer = 0f; // Reset the buildup timer
                    Debug.Log($"Server: Starting Curve Buildup (X): Target Curve Force: {targetCurveForce.x}");
                }

                // Check if the paddle is moving fast enough in the y-direction
                if (Mathf.Abs(paddleVelocity.y) > playerSpeedForCurve)
                {
                    // Calculate the target curve force based on the paddle's vertical velocity
                    float targetY = paddleVelocity.y * -curveFactor;
                    targetCurveForce.y = clampCurve ? Mathf.Clamp(targetY, -maxCurveStrength, maxCurveStrength) : targetY;

                    // Start the curve buildup
                    isCurveBuilding = true;
                    curveBuildupTimer = 0f; // Reset the buildup timer
                    Debug.Log($"Server: Starting Curve Buildup (Y): Target Curve Force: {targetCurveForce.y}");
                }
            }
            else{
                Rigidbody paddleRb = collision.gameObject.GetComponent<Rigidbody>();
                if (paddleRb != null)
                {
                    // Calculate the paddle's velocity
                    Vector3 paddleVelocity = paddleRb.linearVelocity;
                    Debug.Log($"Paddle Velocity: {paddleVelocity}");

                    // Check if the paddle is moving fast enough in the x-direction
                    if (Mathf.Abs(paddleVelocity.x) > playerSpeedForCurve)
                    {
                        // Calculate the target curve force based on the paddle's horizontal velocity
                        float targetX = paddleVelocity.x * -curveFactor;
                        targetCurveForce.x = clampCurve ? Mathf.Clamp(targetX, -maxCurveStrength, maxCurveStrength) : targetX;

                        // Start the curve buildup
                        isCurveBuilding = true;
                        curveBuildupTimer = 0f; // Reset the buildup timer
                        Debug.Log($"Server: Starting Curve Buildup (X): Target Curve Force: {targetCurveForce.x}");
                    }

                    // Check if the paddle is moving fast enough in the y-direction
                    if (Mathf.Abs(paddleVelocity.y) > playerSpeedForCurve)
                    {
                        // Calculate the target curve force based on the paddle's vertical velocity
                        float targetY = paddleVelocity.y * -curveFactor;
                        targetCurveForce.y = clampCurve ? Mathf.Clamp(targetY, -maxCurveStrength, maxCurveStrength) : targetY;

                        // Start the curve buildup
                        isCurveBuilding = true;
                        curveBuildupTimer = 0f; // Reset the buildup timer
                        Debug.Log($"Server: Starting Curve Buildup (Y): Target Curve Force: {targetCurveForce.y}");
                    }
                }
            }
            

            // Calculate the hit factor based on the paddle's position
            float hitFactor = (transform.position.x - collision.transform.position.x) / collision.transform.localScale.x;

            // Adjust the direction to reduce sideways movement
            direction = new Vector3(hitFactor * xAxisBounceStrength, direction.y, -direction.z).normalized;

            // Update velocity after direction change
            rb.linearVelocity = direction * currentSpeed;
        }
        else if (collision.gameObject.CompareTag("SideWall"))
        {
            // Reverse the x-component of the direction
            direction.x = -direction.x * xAxisBounceStrength;
            curveForce.x = -curveForce.x;
            // Reduce or reset the curve force on the x-axis
            curveForce.x *= 0.5f; // Reduce the curve force by 50% (or set to 0 if needed)
            if (Mathf.Abs(curveForce.x) < 0.1f) curveForce.x = 0f; // Stop small lingering forces

            // Update the ball's velocity
            rb.linearVelocity = direction * currentSpeed;
            Debug.Log($"Server: Ball hit SideWall. New Direction: {direction}, Curve Force: {curveForce}");
        }
        else if (collision.gameObject.CompareTag("TopWall"))
        {
            // Reverse the y-component of the direction
            direction.y = -direction.y * yAxisBounceStrength;
            curveForce.y = -curveForce.y;

            // Reduce or reset the curve force on the y-axis
            curveForce.y *= 0.5f; // Reduce the curve force by 50% (or set to 0 if needed)
            if (Mathf.Abs(curveForce.y) < 0.1f) curveForce.y = 0f; // Stop small lingering forces

            // Update the ball's velocity
            rb.linearVelocity = direction * currentSpeed;
            Debug.Log($"Server: Ball hit TopWall. New Direction: {direction}, Curve Force: {curveForce}");
        }
        else if (collision.gameObject.CompareTag("ResetBall"))
        {
            ResetBall();
        }
    }

    [Server]
    private void FixedUpdate()
    {
        if (!isServer) return;

        if (isPaused) return;

        // Update the ball's velocity based on its direction and speed
        Vector3 baseVelocity = direction * currentSpeed;

        // Handle curve buildup
        if (isCurveBuilding)
        {
            curveBuildupTimer += Time.fixedDeltaTime;

            // Gradually increase the curve force toward the target curve force
            curveForce = Vector3.Lerp(Vector3.zero, targetCurveForce, curveBuildupTimer / curveBuildupTime);

            // Clamp the curve force if enabled
            if (clampCurve)
            {
                curveForce.x = Mathf.Clamp(curveForce.x, -maxCurveStrength, maxCurveStrength);
                curveForce.y = Mathf.Clamp(curveForce.y, -maxCurveStrength, maxCurveStrength);
            }

            // If the buildup is complete, stop the buildup phase
            if (curveBuildupTimer >= curveBuildupTime)
            {
                isCurveBuilding = false;
            }
        }
        else if (curveForce != Vector3.zero)
        {
            // Gradually reduce the curve force after the buildup phase
            curveForce = Vector3.Lerp(curveForce, Vector3.zero, curveDecayRate * Time.fixedDeltaTime);

            // Clamp the curve force if enabled
            if (clampCurve)
            {
                curveForce.x = Mathf.Clamp(curveForce.x, -maxCurveStrength, maxCurveStrength);
                curveForce.y = Mathf.Clamp(curveForce.y, -maxCurveStrength, maxCurveStrength);
            }

            // Stop small lingering forces
            if (Mathf.Abs(curveForce.x) < 0.1f) curveForce.x = 0f;
            if (Mathf.Abs(curveForce.y) < 0.1f) curveForce.y = 0f;
        }

        // Apply the curve force to the ball's velocity
        rb.linearVelocity = baseVelocity + curveForce * Time.fixedDeltaTime * 2.0f; // Amplify the curve force
        Debug.Log($"Applying Curve Force: {curveForce}");

        // Sync the ball's position and velocity
        syncedPosition = rb.transform.position;
        syncedVelocity = rb.linearVelocity;

        Debug.Log($"Server: Position: {syncedPosition}, Velocity: {syncedVelocity}, Curve Force: {curveForce}");
    }

    private void Update()
    {
        if (!isServer && !isPaused)
        {
            // Smoothly interpolate the ball's position and velocity on the client
            rb.transform.position = Vector3.Lerp(rb.transform.position, syncedPosition, Time.deltaTime * interpolationDelay);
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, syncedVelocity, Time.deltaTime * interpolationDelay);

            Debug.Log($"Client: Interpolated Position: {rb.transform.position}, Interpolated Velocity: {rb.linearVelocity}");
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
            yield return new WaitForSeconds(1f);
            currentSpeed += isPaused ? 0 : speedIncrementPerSec; // Increment speed by speedIncrementPerSec every second
            Debug.Log($"Server: Ball speed increased to {currentSpeed}");
        }
    }
}
