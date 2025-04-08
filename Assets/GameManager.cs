using UnityEngine;
using Mirror;

public class GameManager : NetworkManager
{
    public GameObject ballPrefab;
    private GameObject ballInstance;

    public GameObject scoreUiPrefab;

    private GameObject scoreUiInstance;
    
    public GameObject debugWallPrefab;

    private GameObject debugWallInstance;
    private int playerCount = 0;

    public Camera camera1;
    public Camera camera2;
    public Light blueDirLight;
    public Light redDirLight;
    public bool debugMode = false;
    public override void Start()
    {
        base.Start();

        setCamerasAndLights();
    }

    public override void Update()
    {
        setCamerasAndLights();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        Debug.Log("Server started, players: " + playerCount);
        setCamerasAndLights();
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Vector3 playerPosition = playerCount == 0 ? new Vector3(0, 1, 4.92f) : new Vector3(0, 1, -4.92f);
        Quaternion playerRotation = playerCount == 0 ? Quaternion.identity : Quaternion.Euler(0, 180, 0);

        GameObject player = Instantiate(playerPrefab, playerPosition, playerRotation);

        // Assign the player index based on the player count
        PaddleMovement paddleMovement = player.GetComponent<PaddleMovement>();
        if (paddleMovement != null)
        {
            paddleMovement.playerIndex = playerCount + 1; // Player 1 gets index 1, Player 2 gets index 2
        }

        NetworkServer.AddPlayerForConnection(conn, player);

        playerCount++;

        setCamerasAndLights();
        if (debugMode){
            if (playerCount == 1){
                if (debugWallInstance != null){
                    NetworkServer.Destroy(debugWallInstance);
                    debugWallInstance = null;
                }
                debugWallInstance = Instantiate(debugWallPrefab, new Vector3(0, 1, -4.95f), Quaternion.identity);
                StartGame();
            }
        }
        else{
            if (playerCount == 2) // Check if two players are connected
            {
                StartGame();
            }
        }
        
        Debug.Log("players: " + playerCount);
    }

    public void setCamerasAndLights()
    {
        // If the player is not in a server, enable camera1 by default
        if (!NetworkClient.active && !NetworkServer.active)
        {
            camera1.enabled = true;
            camera1.GetComponent<AudioListener>().enabled = true;
            camera2.enabled = false;
            camera2.GetComponent<AudioListener>().enabled = false;

            blueDirLight.enabled = true;
            redDirLight.enabled = false;
            return;
        }

        // Find all player objects in the scene
        GameObject[] players = GameObject.FindGameObjectsWithTag("Paddle");

        if (players.Length > 0)
        {
            foreach (GameObject player in players)
            {
                // Get the PaddleMovement component to check if this is the local player
                PaddleMovement paddleMovement = player.GetComponent<PaddleMovement>();
                if (paddleMovement != null && paddleMovement.isLocalPlayer)
                {
                    // Check the Z position of the local player to determine their role
                    if (player.transform.position.z > 0)
                    {
                        // Assign camera1 to the local player at position z = 5
                        camera1.enabled = true;
                        camera1.GetComponent<AudioListener>().enabled = true;
                        camera2.enabled = false;
                        camera2.GetComponent<AudioListener>().enabled = false;
                        blueDirLight.enabled = true;
                        redDirLight.enabled = false;
                    }
                    else if (player.transform.position.z < 0)
                    {
                        // Assign camera2 to the local player at position z = -5
                        camera2.enabled = true;
                        camera2.GetComponent<AudioListener>().enabled = true;
                        camera1.enabled = false;
                        camera1.GetComponent<AudioListener>().enabled = false;
                        blueDirLight.enabled = false;
                        redDirLight.enabled = true;
                    }
                }
            }
        }
        else
        {
            // Default to camera1 if no players are found
            camera1.enabled = true;
            camera1.GetComponent<AudioListener>().enabled = true;
            camera2.enabled = false;
            camera2.GetComponent<AudioListener>().enabled = false;
            blueDirLight.enabled = true;
            redDirLight.enabled = false;
        }
    }

    [Server]
    private void StartGame()
    {
        if (scoreUiInstance != null){
            NetworkServer.Destroy(scoreUiInstance);
            scoreUiInstance = null;
        }
        
        scoreUiInstance = Instantiate(scoreUiPrefab);
        NetworkServer.Spawn(scoreUiInstance);

        // Ensure that the ball is destroyed before respawning
        if (ballInstance != null)
        {
            Debug.Log("Destroying existing ball.");
            NetworkServer.Destroy(ballInstance);
            ballInstance = null; // Clear the reference to avoid issues
        }

        // Spawn a new ball
        ballInstance = Instantiate(ballPrefab, new Vector3(0, 0.95f, 0), Quaternion.identity);
        NetworkServer.Spawn(ballInstance);

        // Ensure the ball's Rigidbody is not null
        Rigidbody rb = ballInstance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody is missing on the ball prefab.");
        }

        Debug.Log("Game started, ball spawned.");
    }

    public void ResetGame(){
        if (scoreUiInstance == null){
            scoreUiInstance = Instantiate(scoreUiPrefab);
            NetworkServer.Spawn(scoreUiInstance);
        }

        if (ballInstance != null){
            ballInstance.GetComponent<Ball>().ResetBall();
        }
        else{
            ballInstance = Instantiate(ballPrefab, new Vector3(0, 0.95f, 0), Quaternion.identity);
            NetworkServer.Spawn(ballInstance);
        }

        scoreUiInstance.GetComponent<ScoreMenu>().ResetPoints();
    }

    public void PauseBall(){
        if (ballInstance != null){
            ballInstance.GetComponent<Ball>().PauseBall();
        }
    }

    public void PlayBall(){
        if (ballInstance != null){
            ballInstance.GetComponent<Ball>().PlayBall();
        }
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        if (ballInstance == null && playerCount == 2)
        {
            StartGame();
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);

        playerCount = Mathf.Max(0, playerCount - 1); // Decrease player count when someone disconnects
        setCamerasAndLights();
        if (ballInstance != null && playerCount < 2) // If there are less than 2 players, destroy the ball
        {
            Debug.Log("Player disconnected, destroying ball.");
            if (ballInstance != null){
                NetworkServer.Destroy(ballInstance);
                ballInstance = null; 
            }
            if (scoreUiInstance != null){
                NetworkServer.Destroy(scoreUiInstance);
                scoreUiInstance = null;
            }
            if (debugWallInstance != null){
                NetworkServer.Destroy(debugWallInstance);
                debugWallInstance = null;
            }
        }

        Debug.Log("Player disconnected. Remaining players: " + playerCount);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        // Reset player count to 0
        playerCount = 0;

        setCamerasAndLights();

        if (ballInstance != null)
        {
            NetworkServer.Destroy(ballInstance);
            ballInstance = null;
        }

        if (scoreUiInstance != null){
            NetworkServer.Destroy(scoreUiInstance);
            scoreUiInstance = null;
        }

        if (debugWallInstance != null){
            NetworkServer.Destroy(debugWallInstance);
            debugWallInstance = null;
        }

        Debug.Log("Server stopped. Player count reset to 0.");
    }
}


// curve ball
// improve looks