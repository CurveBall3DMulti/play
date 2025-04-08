using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic; // Required for Dictionary

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
    public float paddleCollisionCooldown = 0.2f; // Cooldown time for paddle collisions

    private float currentSpeed; // Current speed that increases over time
    private Vector3 direction;
    [SyncVar]
    private Vector3 curveForce; // The current curve force applied to the ball
    private Rigidbody rb;

    private ScoreMenu scoreMenu;

    [SyncVar] private Vector3 syncedPosition;
    [SyncVar] private Vector3 syncedVelocity;

    [SyncVar] private bool isPaused;

    [SyncVar] private Vector3 paddle2Velocity = Vector3.zero;

    private Dictionary<GameObject, float> lastPaddleCollisionTime = new Dictionary<GameObject, float>();

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
            // Check if the ball has recently collided with this paddle
            if (lastPaddleCollisionTime.TryGetValue(collision.gameObject, out float lastCollisionTime))
            {
                if (Time.time - lastCollisionTime < paddleCollisionCooldown)
                {
                    // Skip collision logic if within cooldown
                    return;
                }
            }

            // Update the last collision time for this paddle
            lastPaddleCollisionTime[collision.gameObject] = Time.time;

            curveForce = Vector3.zero; // Reset the curve force on paddle hit
            if (collision.gameObject.transform.position.z < 0)
            {
                Vector3 paddleVelocity = paddle2Velocity;
                Debug.Log($"Paddle 2 Velocity: {paddleVelocity}");

                // Check if the paddle is moving fast enough in the x-direction
                if (Mathf.Abs(paddleVelocity.x) > playerSpeedForCurve)
                {
                    // Calculate the target curve force based on the paddle's horizontal velocity
                    float targetX = paddleVelocity.x * curveFactor;
                    curveForce.x = clampCurve ? Mathf.Clamp(targetX, -maxCurveStrength, maxCurveStrength) : targetX;
                }

                // Check if the paddle is moving fast enough in the y-direction
                if (Mathf.Abs(paddleVelocity.y) > playerSpeedForCurve)
                {
                    // Calculate the target curve force based on the paddle's vertical velocity
                    float targetY = paddleVelocity.y * curveFactor;
                    curveForce.y = clampCurve ? Mathf.Clamp(targetY, -maxCurveStrength, maxCurveStrength) : targetY;
                }
            }
            else
            {
                Rigidbody paddleRb = collision.gameObject.GetComponent<Rigidbody>();
                if (paddleRb != null)
                {
                    // Calculate the paddle's velocity
                    Vector3 paddleVelocity = paddleRb.linearVelocity;

                    // Check if the paddle is moving fast enough in the x-direction
                    if (Mathf.Abs(paddleVelocity.x) > playerSpeedForCurve)
                    {
                        float targetX = paddleVelocity.x * curveFactor;
                        curveForce.x = clampCurve ? Mathf.Clamp(targetX, -maxCurveStrength, maxCurveStrength) : targetX;
                    }

                    if (Mathf.Abs(paddleVelocity.y) > playerSpeedForCurve)
                    {
                        float targetY = paddleVelocity.y * curveFactor;
                        curveForce.y = clampCurve ? Mathf.Clamp(targetY, -maxCurveStrength, maxCurveStrength) : targetY;
                    }
                }
            }

            // Calculate the hit factor based on the paddle's position
            float hitFactorX = (transform.position.x - collision.transform.position.x) / collision.transform.localScale.x;
            float hitFactorY = (transform.position.y - collision.transform.position.y) / collision.transform.localScale.y;

            // Adjust the direction to include changes to the y-component
            direction = new Vector3(hitFactorX * xAxisBounceStrength, hitFactorY * yAxisBounceStrength, -direction.z).normalized;

            // Update velocity after direction change
            rb.linearVelocity = direction * currentSpeed;
            Debug.Log($"Server: Ball hit Paddle. New Velocity: {rb.linearVelocity}, Curve Force: {curveForce}");
        }
        else if (collision.gameObject.CompareTag("SideWall"))
        {
            // Reverse the x-component of the direction
            direction.x = -direction.x * xAxisBounceStrength;

            // Reduce or reset the curve force on the x-axis
            curveForce.x *= 0.5f; // Reduce the curve force by 50%
            if (Mathf.Abs(curveForce.x) < .05f) curveForce.x = 0f; // Stop small lingering forces

            // Move the ball slightly away from the wall
            Vector3 newPosition = rb.transform.position;
            newPosition.x += direction.x * 0.1f; // Adjust the offset as needed
            rb.transform.position = newPosition;

            // Update the ball's velocity
            rb.linearVelocity = direction * currentSpeed;
            Debug.Log($"Server: Ball hit SideWall. New Velocity: {rb.linearVelocity}, Curve Force: {curveForce}");
        }
        else if (collision.gameObject.CompareTag("TopWall"))
        {
            // Reverse the y-component of the direction
            direction.y = -direction.y * yAxisBounceStrength;

            // Reduce or reset the curve force on the y-axis
            curveForce.y *= 0.5f; // Reduce the curve force by 50%
            if (Mathf.Abs(curveForce.y) < .05f) curveForce.y = 0f; // Stop small lingering forces

            // Move the ball slightly away from the wall
            Vector3 newPosition = rb.transform.position;
            newPosition.y += direction.y * .1f; // Adjust the offset as needed
            rb.transform.position = newPosition;

            // Update the ball's velocity
            rb.linearVelocity = direction * currentSpeed;
            Debug.Log($"Server: Ball hit TopWall. New linear velocity{rb.linearVelocity}, Curve Force: {curveForce}");
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

        if (curveForce != Vector3.zero)
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
            if (Mathf.Abs(curveForce.x) < 0.05f) curveForce.x = 0f;
            if (Mathf.Abs(curveForce.y) < 0.05f) curveForce.y = 0f;
        }

        // Apply the curve force to the ball's velocity
        rb.linearVelocity = baseVelocity + curveForce * Time.fixedDeltaTime; // Amplify the curve force

        // Sync the ball's position and velocity
        syncedPosition = rb.transform.position;
        syncedVelocity = rb.linearVelocity;
    }

    private void Update()
    {
        if (!isServer && !isPaused)
        {
            // Smoothly interpolate the ball's position and velocity on the client
            rb.transform.position = Vector3.Lerp(rb.transform.position, syncedPosition, Time.deltaTime * interpolationDelay);
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, syncedVelocity, Time.deltaTime * interpolationDelay);
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
            currentSpeed += isPaused ? 0 : speedIncrementPerSec;
        }
    }
}
