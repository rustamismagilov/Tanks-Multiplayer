using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

// HostSingleton ensures a single instance of the host manager and handles its initialization
public class HostSingleton : MonoBehaviour
{
    private static HostSingleton instance; // Static instance of HostSingleton

    public HostGameManager GameManager { get; private set; } // Instance of the host game manager

    // Property to get the single instance of HostSingleton
    public static HostSingleton Instance
    {
        get
        {
            if (instance != null) { return instance; } // Return existing instance if available

            instance = FindObjectOfType<HostSingleton>(); // Find instance in the scene

            if (instance == null)
            {
                return null;
            }

            return instance;
        }
    }

    // This method is called when the script instance is being loaded
    private void Start()
    {
        DontDestroyOnLoad(gameObject); // Prevent this game object from being destroyed on scene load
    }

    // Method to create and initialize the host
    public void CreateHost(NetworkObject playerPrefab)
    {
        GameManager = new HostGameManager(playerPrefab); // Instantiate the host game manager
    }

    // Dispose method to clean up resources
    private void OnDestroy()
    {
        // Dispose of the game manager if it's not null
        GameManager?.Dispose();
    }
}
