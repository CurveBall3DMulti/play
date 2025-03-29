using Mirror;
using Mirror.Discovery;
using UnityEngine;
using System.Collections.Generic;

public class CustomNetworkDiscovery : NetworkDiscovery
{
    public MainMenu mainMenu;

    // A dictionary to store discovered servers
    private Dictionary<string, ServerResponse> discoveredServers = new Dictionary<string, ServerResponse>();

    public override void Start()
    {
        // Subscribe to the OnServerFound event
        OnServerFound.AddListener(OnServerFoundHandler);
    }

    // This method is invoked when a server is found
    private void OnServerFoundHandler(ServerResponse info)
    {
        string serverAddress = info.EndPoint.Address.ToString();

        // Add the server to the dictionary if it's not already there
        if (!discoveredServers.ContainsKey(serverAddress))
        {
            discoveredServers[serverAddress] = info;
            Debug.Log($"Discovered server at {serverAddress}");

            // Notify the MainMenu to update the server list UI
            if (mainMenu != null)
            {
                mainMenu.AddServerToList(serverAddress);
            }
        }
    }

    // Called when the user selects a server to join
    public void JoinSelectedServer(string serverAddress)
    {
        if (discoveredServers.ContainsKey(serverAddress))
        {
            Debug.Log($"Joining server at {serverAddress}");
            NetworkManager.singleton.networkAddress = serverAddress;
            NetworkManager.singleton.StartClient();
        }
        else
        {
            Debug.LogError($"Server at {serverAddress} not found in discovered servers.");
        }
    }

    public void ClearDiscoveredServers()
    {
        discoveredServers.Clear();
    }

    public void RemoveDiscoveredServer(string serverAddress)
    {
        if (discoveredServers.ContainsKey(serverAddress))
        {
            discoveredServers.Remove(serverAddress);
        }
    }
}
