using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    public Button hostButton;
    public Button joinButton;
    public Button quitButton;
    public Button leaveButton;
    public TMP_Text leaveButtonText; // Text component for the Leave button

    public Transform serverListContainer; // A UI container for the server list
    public GameObject serverListItemPrefab; // A prefab for each server list item
    public GameObject serverListPanel; // The panel containing the server list

    public TMP_Text serverInfoText; 

    public CustomNetworkDiscovery networkDiscovery;

    private string serverIPAddress = "Fetching IP...";

    private void Start()
    {
        hostButton.onClick.AddListener(HostGame);
        joinButton.onClick.AddListener(StartDiscovery);
        quitButton.onClick.AddListener(QuitGame);
        leaveButton.onClick.AddListener(LeaveGame);
        serverListPanel.SetActive(false);

        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    private void HostGame()
    {
        Debug.Log("Hosting game...");
        NetworkManager.singleton.StartHost();

        // Start broadcasting the server
        networkDiscovery.AdvertiseServer();

        // Fetch and display the external IP address
        GetIPAddress();    

        UpdateUI();
    }

    private void StartDiscovery()
    {
        Debug.Log("Searching for servers...");

        serverListPanel.SetActive(true);

        // Clear the server list UI
        foreach (Transform child in serverListContainer)
        {
            Destroy(child.gameObject);
        }

        // Clear the discovered servers dictionary
        networkDiscovery.ClearDiscoveredServers();

        // Start discovering servers
        networkDiscovery.StartDiscovery();
    }

    private void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }

    private void LeaveGame()
    {
        if (NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.active)
        {
            NetworkManager.singleton.StopClient();
        }

        Debug.Log("Left the game.");

        // Update UI
        UpdateUI();
    }

    // Called by CustomNetworkDiscovery to add a server to the UI
    public void AddServerToList(string serverAddress)
    {
        Debug.Log($"Adding server to list: {serverAddress}");

        // Instantiate a new server list item
        GameObject serverListItem = Instantiate(serverListItemPrefab, serverListContainer);

        // Set the server address text
        TMP_Text serverText = serverListItem.GetComponentInChildren<TMP_Text>();
        if (serverText != null)
        {
            serverText.text = serverAddress; // Set the button's text to the server address
        }
        else
        {
            Debug.LogError("Server list item prefab is missing a TMP_Text component.");
        }

        // Add a button listener to join the server when clicked
        Button serverButton = serverListItem.GetComponentInChildren<Button>();
        if (serverButton != null)
        {
            serverButton.onClick.AddListener(() =>
            {
                networkDiscovery.JoinSelectedServer(serverAddress);

                // Hide the server list panel after joining
                serverListPanel.SetActive(false);

                UpdateUI();
            });
        }
        else
        {
            Debug.LogError("Server list item prefab is missing a Button component.");
        }
    }

    // Updates the visibility of UI elements based on the current state
    private void UpdateUI()
    {
        bool isConnected = NetworkServer.active || NetworkClient.active;

        // Show/Hide buttons based on connection state
        hostButton.gameObject.SetActive(!isConnected);
        joinButton.gameObject.SetActive(!isConnected);
        leaveButton.gameObject.SetActive(isConnected);

        if (serverIPAddress == "Fetching IP...")
        {
            serverIPAddress = GetIPAddress();
        }

        serverInfoText.text = isConnected ? $"Server IP: {serverIPAddress}" : "";

        serverInfoText.gameObject.SetActive(isConnected);

        // Update the Leave button text
        if (NetworkServer.active)
        {
            leaveButtonText.text = "Stop Hosting";
        }
        else if (NetworkClient.active)
        {
            leaveButtonText.text = "Leave Server";
        }

        // Hide the server list panel if connected
        if (isConnected)
        {
            serverListPanel.SetActive(false);
        }
    }
    private string GetIPAddress()
    {
        if (!NetworkServer.active && NetworkClient.active)
        {
            return NetworkManager.singleton.networkAddress;
        }
        else if (NetworkServer.active)
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString(); // Return the first IPv4 address
                }
            }
            throw new System.Exception("No network adapters with an IPv4 address in the system!");
        }
        else{
            return serverIPAddress;
        }
    }
}
