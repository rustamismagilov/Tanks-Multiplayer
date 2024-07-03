using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

// ClientGameManager manages client-side game operations, including initialization and scene management
public class ClientGameManager : IDisposable
{
    private JoinAllocation allocation; // Allocation for joining a relay server

    private NetworkClient networkClient; // Network client instance to manage network-related operations
    private MatchplayMatchmaker matchmaker; // Instance for managing matchmaking
    private UserData userData; // User data for the client

    private const string MenuSceneName = "Main Menu"; // Name of the main menu scene

    // Method to initialize Unity services and authenticate the client asynchronously
    public async Task<bool> InitAsync()
    {
        await UnityServices.InitializeAsync(); // Initialize Unity services

        networkClient = new NetworkClient(NetworkManager.Singleton); // Create a new NetworkClient instance with the singleton NetworkManager
        matchmaker = new MatchplayMatchmaker(); // Initialize the matchmaker

        AuthState authState = await AuthenticationWrapper.DoAuth(); // Perform authentication

        if (authState == AuthState.Authenticated)
        {
            // Create a UserData object with the player's name and authentication ID
            userData = new UserData
            {
                userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Missing Name"),
                userAuthId = AuthenticationService.Instance.PlayerId
            };
            return true; // Return true if authentication is successful
        }

        return false; // Return false if authentication fails
    }

    // Method to navigate to the main menu
    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName); // Load the main menu scene
    }

    // Method to start the client using IP and port
    public void StartClient(string ip, int port)
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>(); // Get the UnityTransport component from the NetworkManager
        transport.SetConnectionData(ip, (ushort)port); // Set the connection data with IP and port
        ConnectClient(); // Connect the client
    }

    // Method to start the client asynchronously using a join code
    public async Task StartClientAsync(string joinCode)
    {
        try
        {
            allocation = await Relay.Instance.JoinAllocationAsync(joinCode); // Join relay allocation using the join code
        }
        catch (Exception e)
        {
            Debug.Log(e); // Log any exceptions
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>(); // Get the UnityTransport component from the NetworkManager

        RelayServerData relayServerData = new RelayServerData(allocation, "dtls"); // Create relay server data using the allocation
        transport.SetRelayServerData(relayServerData); // Set the relay server data for the transport

        ConnectClient(); // Connect the client
    }

    // Method to connect the client
    private void ConnectClient()
    {
        string payload = JsonUtility.ToJson(userData); // Serialize the user data to JSON
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload); // Encode the JSON to bytes

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes; // Set the connection data for the NetworkManager

        NetworkManager.Singleton.StartClient(); // Start the client
    }

    // Method to perform matchmaking asynchronously
    public async void MatchmakeAsync(Action<MatchmakerPollingResult> onMatchmakeResponse)
    {
        if (matchmaker.IsMatchmaking)
        {
            return;
        }

        MatchmakerPollingResult matchResult = await GetMatchAsync(); // Get match result
        onMatchmakeResponse?.Invoke(matchResult); // Invoke the response callback
    }

    // Method to get a match asynchronously
    private async Task<MatchmakerPollingResult> GetMatchAsync()
    {
        MatchmakingResult matchmakingResult = await matchmaker.Matchmake(userData); // Perform matchmaking

        if (matchmakingResult.result == MatchmakerPollingResult.Success)
        {
            StartClient(matchmakingResult.ip, matchmakingResult.port); // Start the client with match result IP and port
        }

        return matchmakingResult.result; // Return the matchmaking result
    }

    // Method to cancel matchmaking asynchronously
    public async Task CancelMatchmaking()
    {
        // Await matchmaking cancellation
        await matchmaker.CancelMatchmaking(); 
    }

    // Method to disconnect the client
    public void Disconnect()
    {
        // Disconnect the network client
        networkClient.Disconnect();
    }

    // Dispose method to clean up resources
    public void Dispose()
    {
        // Dispose of the network client if it's not null
        networkClient?.Dispose();
    }
}
